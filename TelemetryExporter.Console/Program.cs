// See https://aka.ms/new-console-template for more information
using System.Collections.ObjectModel;
using System.Drawing;
using System.Text;
using SkiaSharp;
using Dynastream.Fit;

using TelemetryExporter.Console.Models;
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

ProcessMethod(fitListener.FitMessages);
return 0;

static void ProcessMethod(FitMessages fitMessages)
{
    List<(System.DateTime start, System.DateTime end)> activePeriods = new();

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

    if (!fitMessages.RecordMesgs.Any())
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

    Point? lastKnownGpsLocation = null;
    int frame = default;

    SessionData sessionData = new()
    {
        MaxSpeed = fitMessages.RecordMesgs.Max(x => x.GetEnhancedSpeed()) ?? 0,
        TotalDistance = fitMessages.SessionMesgs[0].GetTotalDistance() ?? 0,
        CountOfRecords = orderedRecordMessages.Count
    };

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

                speed = GetValueBetweenDates(speedMetrics);
                distance = GetValueBetweenDates(distanceMetrics);
                altitude = GetValueBetweenDates(altitudeMetrics);

                lattitude = (int?)GetValueBetweenDates(lattitudeMetrics);
                longitude = (int?)GetValueBetweenDates(longitudeMetrics);
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
            FileName = $"frame_{++frame}.png",
            Altitude = altitude,
            Distance = distance,
            Pace = pace,
            Speed = speed.Value,
            IndexOfCurrentRecord = orderedRecordMessages.IndexOf(currentRecord) + 1,
            Longitude = lastKnownGpsLocation?.X,
            Latitude = lastKnownGpsLocation?.Y,
        };

        TimeSpan duration = (currentTimeFrame - startDate);

        float? hr = currentRecord?.GetHeartRate(); // beats per minute  
        sbyte? t = currentRecord?.GetTemperature();

        // instead of directly generate images I can made model with ready points for drawing calculating - it could take more memory
        // textResult.AppendLine($"{duration,16} - speed: {(int)pace,3}:{(int)paceSeconds:D2}, {activeTimeDuration,16}, Dist: {distance / 1000,4:F2}km, {altitude,4:F1}↑ ♥{hr,3}, {t,2}℃");

        // this will result 2 fps https://fpstoms.com/
        int millsecondsStep = 1000 / FPS;
        currentTimeFrame = currentTimeFrame.AddMilliseconds(millsecondsStep);

        if (isActiveTime)
        {
            activeTimeDuration = activeTimeDuration.Add(TimeSpan.FromMilliseconds(millsecondsStep));
        }

    } while (currentTimeFrame <= endDate);

    // using FileStream fileStream = new("results.txt", FileMode.OpenOrCreate);
    // using StreamWriter w = new(fileStream);
    // w.WriteLine(textResult.ToString());

    /// Do I need this at all ?
    static decimal ConvertToDegreesFromSemicircles(int value)
    {
        decimal result = (long)int.MaxValue + 1;
        decimal res = 180M / result;

        decimal res2 = result / 180M;

        return value * res;
    }

    static bool IsRecordInActiveTime(System.DateTime date, IReadOnlyCollection<(System.DateTime start, System.DateTime end)> activePeriods)
    {
        bool isRecordInActivePeriod = activePeriods.Any(x => x.start <= date && date <= x.end);
        return isRecordInActivePeriod;
    }
}

static double? GetValueBetweenDates(CalculateModel calculateModel)
{
    System.DateTime previousDate = calculateModel.PreviousDate;
    System.DateTime nextDate = calculateModel.NextDate;
    System.DateTime dateForCalculation = calculateModel.CalculateAt;

    double? previousValue = calculateModel.PreviousValue;
    double? nextValue = calculateModel.NextValue;

    if ((previousDate < dateForCalculation && dateForCalculation < nextDate) == false)
    {
        throw new ArgumentException("dateForCalculation should be between previous and next records!");
    }

    TimeSpan timeDifference = nextDate - previousDate;
    TimeSpan timeForCalculationInRange = dateForCalculation - previousDate;

    double? difference = nextValue - previousValue;

    if (!difference.HasValue || previousValue.HasValue == false)
    {
        return null;
    }

    // no change in speed. Return the same speed
    if (difference.HasValue && difference.Value == 0)
    {
        return previousValue.Value;
    }

    double valueDifference = Math.Abs(difference.Value);

    // should we use seconds here !
    double percentageOfTotalTime = timeForCalculationInRange / timeDifference;

    bool isAscending = nextValue - previousValue > 0;
    bool isDescending = nextValue - previousValue < 0;

    double result = previousValue.Value;

    if (isAscending)
    {
        result += (valueDifference * percentageOfTotalTime);
    }
    else if (isDescending)
    {
        result -= (valueDifference * percentageOfTotalTime);
    }

    return result;
}
