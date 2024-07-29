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
        private readonly object lockObj = new();
        const int FPS = 2;

        /// <summary>
        /// Invoked when processing images, after an certain interval.
        /// </summary>
        public event EventHandler<Dictionary<string, double>> OnProgress;

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
        /// <param name="calculateStatisticsFromRange">Whether to calculate the long stats (like distance, elevation graph, path etc.) from provided range 
        /// (<paramref name="rangeStartDate"/> and <paramref name="rangeEndDate"/> ). <para></para> 
        /// Example:<para/> The activity has 30km distance, but it's provided a range of 5km. All the stats will be calculated from that period.
        /// If "false" stats are calculated from start of the activity, <paramref name="rangeStartDate"/> and <paramref name="rangeEndDate"/> 
        /// still can be used to export images from that range.</param>
        public async Task ExportImageFramesAsync(
            FitMessages fitMessages,
            List<int> widgetsIds,
            string saveDirectoryPath,
            string tempDirectoryPath,
            CancellationToken cancellationToken,
            int fps = FPS,
            System.DateTime? rangeStartDate = null,
            System.DateTime? rangeEndDate = null,
            bool calculateStatisticsFromRange = false)
        {
            if (widgetsIds.Count == 0)
            {
                return;
            }

            if (fitMessages.RecordMesgs.Count == 0)
            {
                return;
            }

            List<(System.DateTime start, System.DateTime end)> activePeriods = [];

            List<EventMesg> eventMessages = fitMessages.EventMesgs
                .Where(e => e.GetEvent() == Event.Timer)
                .OrderBy(e => e.GetTimestamp().GetDateTime()).ToList();

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
            //System.DateTime originalStartDateTime = startDate;

            System.DateTime endDate = fitMessages.RecordMesgs[^1].GetTimestamp().GetDateTime();
           // System.DateTime originalEndDateTime = endDate;

            List<RecordMesg> orderedRecordMessages = fitMessages.RecordMesgs.OrderBy(x => x.GetTimestamp().GetDateTime()).ToList();

            // There may not be any records in the selected range.
            if (calculateStatisticsFromRange)
            {
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
            }

            Queue<RecordMesg> queue = new(orderedRecordMessages);
            System.DateTime currentTimeFrame = startDate;

            TimeSpan activeTimeDuration = TimeSpan.Zero;

            SKPoint? lastKnownGpsLocation = null;
            // TimeSpan dateDiff = startDate - originalStartDateTime;
            int indexCurrentRecord = 0;
            int frame = 0;// ((int)dateDiff.TotalSeconds) * fps;

           // int totalFrames = (int)(originalEndDateTime - originalStartDateTime).TotalSeconds * fps;

            SessionData sessionData = new()
            {
                MaxSpeed = orderedRecordMessages.Max(x => x.GetEnhancedSpeed()) * 3.6 ?? 0,
                TotalDistance = calculateStatisticsFromRange
                ? ((orderedRecordMessages[^1].GetDistance() ?? 0) - (orderedRecordMessages[0].GetDistance() ?? 0)) // Check [^1] has distance
                : fitMessages.SessionMesgs[0].GetTotalDistance() ?? 0,
                CountOfRecords = orderedRecordMessages.Count
            };

            RecordMesg currentRecord = queue.Dequeue();
            List<FrameData> framesList = [];

            double? initialDistance = GetInitialDistance(orderedRecordMessages);
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

                        queue.Dequeue(); indexCurrentRecord++;
                    }
                }

                bool inStartDate = rangeStartDate.HasValue == false
                    || (rangeStartDate.HasValue && rangeStartDate.Value <= currentTimeFrame);

                bool inEndDate = rangeEndDate.HasValue == false
                    || (rangeEndDate.HasValue && currentTimeFrame <= rangeEndDate.Value);

                if (inStartDate && inEndDate)
                {
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

                        if (calculateStatisticsFromRange)
                        {
                            distance -= initialDistance;
                        }
                    }

                    FrameData frameData = new()
                    {
                        FileName = $"frame_{$"{++frame}".PadLeft(6, '0')}.png",
                        Altitude = altitude,
                        Distance = distance,
                        Speed = speed * 3.6 ?? 0,
                        IndexOfCurrentRecord = indexCurrentRecord,
                        Longitude = lastKnownGpsLocation?.X,
                        Latitude = lastKnownGpsLocation?.Y,
                    };

                    framesList.Add(frameData);
                }
                // this is the duration (feature widget)
                TimeSpan duration = (currentTimeFrame - startDate); //!! Assume calculateStatisticsFromRange

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

            List<(string, SKData)> zipEntries = [];
            Guid sesstionGuid = Guid.NewGuid();

            string genratedFileName = $"{sesstionGuid}.zip";

            if (!Directory.Exists(tempDirectoryPath))
            {
                Directory.CreateDirectory(tempDirectoryPath);
            }

            string tempZipFileDirectory = Path.Combine(tempDirectoryPath, genratedFileName);
            using FileStream tempDirectoryStream = new(tempZipFileDirectory, FileMode.OpenOrCreate, FileAccess.Write);
            using ZipArchive zipArchive = new(tempDirectoryStream, ZipArchiveMode.Create);

            Dictionary<string, double> widgetDonePercentage = [];

            try
            {
                IEnumerable<IWidget> widgets = GetWidgetList(widgetsIds, orderedRecordMessages);

                IEnumerable<Task> renderTasks = widgets.Select(w =>
                 Task.Run(async () =>
                 {
                     await ImagesGenerator.GenerateDataForWidgetAsync(
                         sessionData,
                         framesList,
                         w,
                         ProcessImage, 
                         cancellationToken);
                 },
                 cancellationToken));

                await Task.WhenAll(renderTasks);

                #region FixingRemainingFramesNotExported
                foreach (var (zipEntryPath, skImageData) in zipEntries)
                {
                    ZipArchiveEntry entry = zipArchive.CreateEntry(zipEntryPath);
                    using Stream streamZipFile = entry.Open();
                    using Stream imageDataStream = skImageData.AsStream();
                    imageDataStream.CopyTo(streamZipFile);
                }

                zipEntries.Clear();

                OnProgress?.Invoke(this, widgetDonePercentage);
                #endregion

            }
            catch (Exception)
            {
                // Don't invoke tempDirectoryStream.Dispose();
                // It's invoked internally form zipArchive.Dispose().
                zipArchive.Dispose();
                System.IO.File.Delete(tempZipFileDirectory);
                throw;
            }

            zipArchive.Dispose();

            if (cancellationToken.IsCancellationRequested)
            {
                System.IO.File.Delete(tempZipFileDirectory);
                return;
            }
            else 
            {
                System.IO.File.Move(tempZipFileDirectory, Path.Combine(saveDirectoryPath, genratedFileName));
            }

            static bool IsRecordInActiveTime(System.DateTime date, IReadOnlyCollection<(System.DateTime start, System.DateTime end)> activePeriods)
            {
                bool isRecordInActivePeriod = activePeriods.Any(x => x.start <= date && date <= x.end);
                return isRecordInActivePeriod;
            }

            void ProcessImage(SKData imageData, IWidget widget, string fileNameOfFrame, double percentage)
            {
                widgetDonePercentage[widget.Name] = percentage;

                const int ThresHold = 100;

                if (zipEntries.Count >= ThresHold)
                {
                    lock (lockObj)
                    {
                        if (zipEntries.Count >= ThresHold)
                        {
                            foreach (var (zipEntryPath, skImageData) in zipEntries)
                            {
                                ZipArchiveEntry entry = zipArchive.CreateEntry(zipEntryPath);
                                using Stream streamZipFile = entry.Open();
                                using Stream imageDataStream = skImageData.AsStream();
                                imageDataStream.CopyTo(streamZipFile);
                            }

                            zipEntries.Clear();

                            OnProgress?.Invoke(this, widgetDonePercentage);
                        }
                    }
                }

                zipEntries.Add((Path.Combine(widget.Category, widget.Name, fileNameOfFrame), imageData));
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
                bool hasConstructorWithOneParameter = type.GetConstructors().Any(c => 
                    c.GetParameters().Length == 1 
                        && c.GetParameters()[0].ParameterType.IsAssignableFrom(recordData.GetType()));

                if (hasConstructorWithOneParameter)
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

        /// <summary>
        /// Need to iterate the collection because the first item could return null 
        /// </summary>
        private static double GetInitialDistance(IReadOnlyCollection<RecordMesg> recordMesgs)
        {
            double? distance = null;

            foreach (RecordMesg recordMessage in recordMesgs)
            {
                distance = recordMessage.GetDistance();
                if (distance.HasValue)
                {
                    return distance.Value;
                }
            }

            return 0;
        }
    }
}
