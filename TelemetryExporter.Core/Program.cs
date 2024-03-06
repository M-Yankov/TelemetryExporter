using System.IO.Compression;
using System.Reflection;

using Dynastream.Fit;

using SkiaSharp;

using TelemetryExporter.Core.Attributes;
using TelemetryExporter.Core.Models;
using TelemetryExporter.Core.Utilities;
using TelemetryExporter.Core.Widgets.Interfaces;

namespace TelemetryExporter.Core
{
    // Check this: /activity-service/activity/12092921949/details
    public class Program
    {
        private object lockObj = new object();
        const int FPS = 2;

        /// <summary>
        /// Export images.
        /// </summary>
        /// <param name="fitMessages"></param>
        /// <param name="cancellationToken">Cancellation token if canceled by user.</param>
        /// <param name="widgetsIds">Each widget have an Id.</param>
        /// <param name="saveDirectoryPath">Directory for final result provided by the user.</param>
        /// <param name="tempDirectoryPath">Platform temp directory.</param>
        /// <param name="fps">Generated frames per second, each frame is image.</param>
        /// <param name="rangeStartDate">Provide UTC date.</param>
        /// <param name="rangeEndDate">Provide UTC date.</param>
        /// <param name="calculateStatisticsFromRange">Whether to calculate the long stats (like distance, elevation graph, path etc.) from provided range (<paramref name="rangeStartDate"/> and <paramref name="rangeEndDate"/> ). <para></para>  Example:<para/> The activity has 30km distance, but it's provided a range of 5km. All the stats will be calculated from that period. If "false" stats are calculated from start of the activity, <paramref name="rangeStartDate"/> and <paramref name="rangeEndDate"/> still can be used to export images from that range.</param>
        public async Task ProcessMethod(
            FitMessages fitMessages,
            List<int> widgetsIds,
            string saveDirectoryPath,
            string tempDirectoryPath,
            CancellationTokenSource cancellationToken,
            int fps = FPS,
            System.DateTime? rangeStartDate = null,
            System.DateTime? rangeEndDate = null,
            bool calculateStatisticsFromRange = false)
        {
            List<(System.DateTime start, System.DateTime end)> activePeriods = [];

            List<EventMesg> eventMessages = fitMessages.EventMesgs.OrderBy(e => e.GetTimestamp().GetDateTime()).ToList();

            if (widgetsIds.Count == 0)
            {
                return;
            }

            if (fitMessages.RecordMesgs.Count == 0)
            {
                return;
            }

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

            // if this is always = true (not runtime calculated) the code could generate the transition (fake) frames.
            // my options is that it's not necessary since the activity is in pause mode. The user can be at one place, not moving, different heart beat
            // but on the other hand the fake frames are something average moving (similar to strava flyBy mode when some one has paused an activity, but was resume far later)
            bool isActiveTime = true;

            System.DateTime startDate = fitMessages.RecordMesgs[0].GetTimestamp().GetDateTime();
            System.DateTime endDate = fitMessages.RecordMesgs[^1].GetTimestamp().GetDateTime();

            List<RecordMesg> orderedRecordMessages = fitMessages.RecordMesgs.OrderBy(x => x.GetTimestamp().GetDateTime()).ToList();
            
            // There may not be any records in the selected range.
            if (rangeStartDate.HasValue && rangeEndDate.HasValue)
            {
                startDate = rangeStartDate.Value;
                endDate = rangeEndDate.Value;

                orderedRecordMessages = new(orderedRecordMessages.Where(x =>
                     rangeStartDate.Value < x.GetTimestamp().GetDateTime() && x.GetTimestamp().GetDateTime() < rangeEndDate.Value));
            }
            else if (rangeStartDate.HasValue)
            {
                startDate = rangeStartDate.Value;

                orderedRecordMessages = new(orderedRecordMessages.Where(x => rangeStartDate.Value < x.GetTimestamp().GetDateTime()));
            }
            else if (rangeEndDate.HasValue)
            {
                endDate = rangeEndDate.Value;

                orderedRecordMessages = new(orderedRecordMessages.Where(x => x.GetTimestamp().GetDateTime() < rangeEndDate.Value));
            }

            Queue<RecordMesg> queue = new(orderedRecordMessages);
            System.DateTime currentTimeFrame = startDate;

            TimeSpan activeTimeDuration = TimeSpan.Zero;
            
            SKPoint? lastKnownGpsLocation = null;
            int frame = default;

            SessionData sessionData = new()
            {
                MaxSpeed = orderedRecordMessages.Max(x => x.GetEnhancedSpeed()) * 3.6 ?? 0,
                TotalDistance = calculateStatisticsFromRange 
                ? (orderedRecordMessages[^1].GetDistance() ?? 0 - orderedRecordMessages[0].GetDistance() ?? 0) // Check [^1] has distance
                : fitMessages.SessionMesgs[0].GetTotalDistance() ?? 0,
                CountOfRecords = orderedRecordMessages.Count
            };


            RecordMesg currentRecord = queue.Dequeue();
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

                FrameData frameData = new()
                {
                    FileName = $"frame_{$"{++frame}".PadLeft(6, '0')}.png",
                    Altitude = altitude,
                    Distance = distance,
                    Speed = speed * 3.6 ?? 0,
                    IndexOfCurrentRecord = orderedRecordMessages.IndexOf(currentRecord) + 1, // this can be replaced with some counter
                    Longitude = lastKnownGpsLocation?.X,
                    Latitude = lastKnownGpsLocation?.Y,
                };

                framesList.Add(frameData);

                // this is the duration (feature widget)
                TimeSpan duration = (currentTimeFrame - startDate);

                // data for future widgets
                float? hr = currentRecord?.GetHeartRate(); // beats per minute  
                sbyte? t = currentRecord?.GetTemperature();

                // https://fpstoms.com/ Example: 2 fps (500ms) = 1000 / 2; 
                int millsecondsStep = 1000 / fps;
                currentTimeFrame = currentTimeFrame.AddMilliseconds(millsecondsStep);

                if (isActiveTime)
                {
                    activeTimeDuration = activeTimeDuration.Add(TimeSpan.FromMilliseconds(millsecondsStep));
                }

            } while (currentTimeFrame <= endDate);

            List<(ZipArchiveEntry, SKData)> zipEntries = [];
            Guid sesstionGuid = Guid.NewGuid();
            using FileStream tempDirectoryStream = new(Path.Combine(tempDirectoryPath, $"{sesstionGuid}"), FileMode.OpenOrCreate, FileAccess.Write);
            using ZipArchive zipArchive = new(tempDirectoryStream, ZipArchiveMode.Create);

            IEnumerable<IWidget> widgets = GetWidgetList(widgetsIds, orderedRecordMessages);
            IEnumerable<Task> renderTasks = widgets.Select(w => ImagesGenerator.GenerateDataForWidgetAsync(sessionData, framesList, w, cancellationToken, ProcessImage));

            await Task.WhenAll(renderTasks);

            static bool IsRecordInActiveTime(System.DateTime date, IReadOnlyCollection<(System.DateTime start, System.DateTime end)> activePeriods)
            {
                bool isRecordInActivePeriod = activePeriods.Any(x => x.start <= date && date <= x.end);
                return isRecordInActivePeriod;
            }
            
            void ProcessImage(SKData imageData, IWidget widget, string fileNameOfFrame)
            {
                WidgetDataAttribute widgetData = widget?.GetType().GetCustomAttribute<WidgetDataAttribute>()!;
                ZipArchiveEntry entry = zipArchive.CreateEntry(Path.Combine(widgetData.Category, fileNameOfFrame), CompressionLevel.Fastest);

                /// threshold
                if (zipEntries.Count >= 100)
                {
                    lock (lockObj)
                    {
                        foreach (var (zipEntry, skData) in zipEntries)
                        {
                            using Stream streamZipFile = zipEntry.Open();
                            using Stream s = skData.AsStream();
                            s.CopyTo(streamZipFile);
                        }
                    }
                }

                zipEntries.Add((entry, imageData));
            }
        }

        static List<IWidget> GetWidgetList(IReadOnlyCollection<int> selectedIds, IReadOnlyCollection<RecordMesg> recordData)
        {
            List<int?> searchableList = selectedIds.Cast<int?>().ToList();
            IEnumerable<Type> types = Assembly
                .GetAssembly(typeof(Program))!
                .GetTypes()
                .Where(t => t.IsAssignableTo(typeof(IWidget)) && searchableList.Contains(t.GetCustomAttribute<WidgetDataAttribute>()?.Index));

            List<IWidget> result = [];
            foreach (Type type in types)
            {
                List<object?> parameters = [];

                // there are two types of widget, parameterless constructor and constructor with recordMessages
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
    }
}
