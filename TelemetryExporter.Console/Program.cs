using System.Reflection;

using Dynastream.Fit;

using SkiaSharp;

using TelemetryExporter.Console.Attributes;
using TelemetryExporter.Console.Models;
using TelemetryExporter.Console.Utilities;
using TelemetryExporter.Console.Widgets.Interfaces;

// Check this: /activity-service/activity/12092921949/details

// below that speed it's assumed as walking (For running only)
// https://www.convert-me.com/en/convert/speed/?u=minperkm_1&v=30
const double SpeedCutoff = 0.5556;

const int FPS = 2;

Decode decode = new();
FitListener fitListener = new();
decode.MesgEvent += fitListener.OnMesg;

FileInfo fitFile = new(@"C:\Users\M.Yankov\Desktop\12027002954_Lidl_Run_ACTIVITY.fit");
using FileStream fitStream = fitFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
decode.Read(fitStream);

await ProcessMethod(fitListener.FitMessages);
return 0;

static async Task ProcessMethod(FitMessages fitMessages)
{
    List<(System.DateTime start, System.DateTime end)> activePeriods = [];

    List<EventMesg> eventMessages = fitMessages.EventMesgs.OrderBy(e => e.GetTimestamp().GetDateTime()).ToList();

    int lastIndexStart = -1;
    for (int i = 0; i < eventMessages.Count; i++)
    {
        EventMesg eventMesg = eventMessages[i];
        EventType? eventType = eventMesg.GetEventType();
        if (eventType.HasValue)
        {
            switch (eventType.Value)
            {
                case EventType.Start:
                    lastIndexStart = i;
                    break;
                case EventType.Stop:
                case EventType.StopAll:

                    if (lastIndexStart != -1)
                    {
                        System.DateTime periodStartDate = eventMessages[lastIndexStart].GetTimestamp().GetDateTime();
                        System.DateTime periodStopDate = eventMesg.GetTimestamp().GetDateTime();
                        activePeriods.Add((periodStartDate, periodStopDate));
                    }

                    lastIndexStart = -1;
                    break;
                default:
                    break;
            }
        }
    }

    if (fitMessages.RecordMesgs.Count == 0)
    {
        return;
    }

    // if this is always = true (not runtime calculated) the code could generate the transition (fake) frames.
    // my options is that it's not necessary since the activity is in pause mode. The user can be at one place, not moving, different heart beat
    // but on the other hand the fake frames are something average moving (similar to strava flyBy mode when some one has paused an activity, but was resume far later)
    bool isActiveTime = true; 

    List<RecordMesg> orderedRecordMessages = fitMessages.RecordMesgs.OrderBy(x => x.GetTimestamp().GetDateTime()).ToList();

    Queue<RecordMesg> queue = new(orderedRecordMessages);
    System.DateTime startDate = fitMessages.RecordMesgs[0].GetTimestamp().GetDateTime();
    System.DateTime endDate = fitMessages.RecordMesgs[^1].GetTimestamp().GetDateTime();

    System.DateTime currentTimeFrame = startDate;

    TimeSpan activeTimeDuration = TimeSpan.Zero;

    RecordMesg currentRecord = queue.Dequeue();

    SKPoint? lastKnownGpsLocation = null;
    int frame = default;

    SessionData sessionData = new()
    {
        MaxSpeed = fitMessages.RecordMesgs.Max(x => x.GetEnhancedSpeed()) * 3.6 ?? 0,
        TotalDistance = fitMessages.SessionMesgs[0].GetTotalDistance() ?? 0,
        CountOfRecords = orderedRecordMessages.Count
    };

    List<FrameData> framesList = [];
 
    do
    {
        double? speed = null;
        double? distance = null;
        double? altitude = null;
        int? lattitude = null;
        int? longitude = null;

        isActiveTime = IsRecordInActiveTime(currentTimeFrame, activePeriods);

        if (queue.TryPeek(out RecordMesg? nextRcordMesg))
        {
            System.DateTime recordDate = nextRcordMesg.GetTimestamp().GetDateTime();
            System.DateTime? currentRecordDate = currentRecord.GetTimestamp().GetDateTime();

            if (currentRecordDate < currentTimeFrame && currentTimeFrame < recordDate && isActiveTime)
            {
                CalculateModel speedMetrics = new(
                    currentRecordDate.Value,
                    recordDate,
                    currentRecord.GetEnhancedSpeed(),
                    nextRcordMesg.GetEnhancedSpeed(),
                    currentTimeFrame);

                CalculateModel distanceMetrics = new(
                    currentRecordDate.Value,
                    recordDate,
                    currentRecord.GetDistance(),
                    nextRcordMesg.GetDistance(),
                    currentTimeFrame);

                CalculateModel altitudeMetrics = new(
                    currentRecordDate.Value,
                    recordDate,
                    currentRecord.GetEnhancedAltitude(),
                    nextRcordMesg.GetEnhancedAltitude(),
                    currentTimeFrame);

                CalculateModel lattitudeMetrics = new(
                    currentRecordDate.Value,
                    recordDate,
                    currentRecord.GetPositionLat(),
                    nextRcordMesg.GetPositionLat(),
                    currentTimeFrame);

                CalculateModel longitudeMetrics = new(
                    currentRecordDate.Value,
                    recordDate,
                    currentRecord.GetPositionLong(),
                    nextRcordMesg.GetPositionLong(),
                    currentTimeFrame);

                speed = Helper.GetValueBetweenDates(speedMetrics);
                distance = Helper.GetValueBetweenDates(distanceMetrics);
                altitude = Helper.GetValueBetweenDates(altitudeMetrics);

                lattitude = (int?)Helper.GetValueBetweenDates(lattitudeMetrics);
                longitude = (int?)Helper.GetValueBetweenDates(longitudeMetrics);
            }

            if (currentRecordDate.HasValue && recordDate > currentRecordDate && currentTimeFrame >= recordDate)
            {
                if (isActiveTime)
                {
                    currentRecord = nextRcordMesg;
                }

                queue.Dequeue();
            }
        }

        if (isActiveTime)
        {
            altitude ??= currentRecord?.GetEnhancedAltitude();
            speed ??= currentRecord?.GetEnhancedSpeed();
            distance ??= currentRecord?.GetDistance();

            longitude ??= currentRecord?.GetPositionLong();
            lattitude ??= currentRecord?.GetPositionLat();

            if (lattitude.HasValue && longitude.HasValue)
            {
                lastKnownGpsLocation = new(longitude.Value, lattitude.Value);
            }
        }

        // for run only !
        if (speed < SpeedCutoff)
        {
            speed = 0;
        }

        double pace = speed > 0 ? 60 / (speed.Value * 3.6) : default;

        FrameData frameData = new() 
        {
            FileName = $"frame_{$"{++frame}".PadLeft(6, '0')}.png",
            Altitude = altitude,
            Distance = distance,
            Pace = pace,
            Speed = speed * 3.6 ?? 0,
            IndexOfCurrentRecord = orderedRecordMessages.IndexOf(currentRecord) + 1,
            Longitude = lastKnownGpsLocation?.X,
            Latitude = lastKnownGpsLocation?.Y,
        };

        framesList.Add(frameData);

        TimeSpan duration = (currentTimeFrame - startDate);

        float? hr = currentRecord?.GetHeartRate(); // beats per minute  
        sbyte? t = currentRecord?.GetTemperature();

        // this will result 2 fps https://fpstoms.com/
        int millsecondsStep = 1000 / FPS;
        currentTimeFrame = currentTimeFrame.AddMilliseconds(millsecondsStep);

        if (isActiveTime)
        {
            activeTimeDuration = activeTimeDuration.Add(TimeSpan.FromMilliseconds(millsecondsStep));
        }

    } while (currentTimeFrame <= endDate);


    IEnumerable<IWidget> widgets = GetWidgetList([4], fitMessages.RecordMesgs);

    List<Task> renderTasks = [];
    foreach (IWidget widget in widgets)
    {
        // task.ConfigureAwait(false);
        renderTasks.Add(ImagesGenerator.GenerateDataForWidgetAsync(sessionData, framesList, widget));
    }

    await Task.WhenAll(renderTasks);

    static bool IsRecordInActiveTime(System.DateTime date, IReadOnlyCollection<(System.DateTime start, System.DateTime end)> activePeriods)
    {
        bool isRecordInActivePeriod = activePeriods.Any(x => x.start <= date && date <= x.end);
        return isRecordInActivePeriod;
    }
}

static IEnumerable<IWidget> GetWidgetList(IReadOnlyCollection<int> selectedIds, IReadOnlyCollection<RecordMesg> recordData)
{
    List<int?> searchableList = selectedIds.Cast<int?>().ToList();
    IEnumerable<Type> types = Assembly
        .GetExecutingAssembly()
        .GetTypes()
        .Where(t => t.IsAssignableTo(typeof(IWidget)) && searchableList.Contains(t.GetCustomAttribute<WidgetDataAttribute>()?.Index));

    List<IWidget> result = [];
    foreach (Type type in types)
    {
        List<object?> parameters = [];
        if (type.GetConstructor([recordData.GetType()]) != null)
        {
            parameters.Add(recordData);
        }

        object? createdInstance = Activator.CreateInstance(type, [.. parameters]);
        if (createdInstance is IWidget widget)
        {
            result.Add(widget);
        }
    }

    return result;
}
