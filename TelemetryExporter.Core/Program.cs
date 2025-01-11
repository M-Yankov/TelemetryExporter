using System.Collections.Concurrent;
using System.IO.Compression;

using Dynastream.Fit;

using SkiaSharp;

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
        public event EventHandler<Dictionary<string, double>>? OnProgress;

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

            WidgetFactory widgetFactory = new();

            FitInitializer initializer = FitInitializer.Initialize(
                fitMessages,
                rangeStartDate,
                rangeEndDate,
                calculateStatisticsFromRange);

            // if this is always = true (not runtime calculated) the code could generate the transition (fake) frames.
            // my options is that it's not necessary since the activity is in pause mode. The user can be at one place, not moving, different heart beat
            // but on the other hand the fake frames are something average moving (similar to strava flyBy mode when some one has paused an activity, but was resume far later)
            bool isActiveTime = true;

            Queue<FitMessageModel> queue = new(initializer.Records);
            System.DateTime currentTimeFrame = initializer.StartDate;

            TimeSpan activeTimeDuration = TimeSpan.Zero;

            SKPoint? lastKnownGpsLocation = null;
            // TimeSpan dateDiff = startDate - originalStartDateTime;

            int frame = 0;// ((int)dateDiff.TotalSeconds) * fps;

            SessionData sessionData = new()
            {
                MaxSpeed = initializer.MaxSpeed,
                TotalDistance = initializer.Distance,
                CountOfRecords = calculateStatisticsFromRange ? initializer.Records.Count : fitMessages.RecordMesgs.Count,
                MaxPower = initializer.MaxPower
            };

            FitMessageModel currentRecord = queue.Dequeue();
            List<FrameData> framesList = [];
            var gradesEnumerator = initializer.Grades.GetEnumerator();
            // advance to the first value
            gradesEnumerator.MoveNext();
            KeyValuePair<System.DateTime, double?> currentGradePair = gradesEnumerator.Current;

            double? initialDistance = initializer.FirstDistance;

            do
            {
                double? speed = null;
                double? distance = null;
                double? altitude = null;
                int? lattitude = null;
                int? longitude = null;
                double? grade = null;
                ushort? power = null;

                isActiveTime = initializer.IsRecordInActiveTime(currentTimeFrame);

                if (queue.TryPeek(out FitMessageModel? nextRcordMesg))
                {
                    System.DateTime recordDate = nextRcordMesg.RecordDateTime;
                    System.DateTime? currentRecordDate = currentRecord.RecordDateTime;

                    if (currentRecordDate < currentTimeFrame && currentTimeFrame < recordDate && isActiveTime)
                    {
                        CalculateModel speedMetrics = new(
                            currentRecordDate.Value,
                            recordDate,
                            currentRecord.Speed,
                            nextRcordMesg.Speed,
                            currentTimeFrame);

                        CalculateModel distanceMetrics = new(
                            currentRecordDate.Value,
                            recordDate,
                            currentRecord.Distance,
                            nextRcordMesg.Distance,
                            currentTimeFrame);

                        CalculateModel altitudeMetrics = new(
                            currentRecordDate.Value,
                            recordDate,
                            currentRecord.Altitude,
                            nextRcordMesg.Altitude,
                            currentTimeFrame);

                        CalculateModel lattitudeMetrics = new(
                            currentRecordDate.Value,
                            recordDate,
                            currentRecord.Lattitude,
                            nextRcordMesg.Lattitude,
                            currentTimeFrame);

                        CalculateModel longitudeMetrics = new(
                            currentRecordDate.Value,
                            recordDate,
                            currentRecord.Longitude,
                            nextRcordMesg.Longitude,
                            currentTimeFrame);

                        CalculateModel powerMetrics = new(
                            currentRecordDate.Value,
                            recordDate,
                            currentRecord.Power,
                            nextRcordMesg.Power,
                            currentTimeFrame);

                        speed = Helper.GetValueBetweenDates(speedMetrics);
                        distance = Helper.GetValueBetweenDates(distanceMetrics);
                        altitude = Helper.GetValueBetweenDates(altitudeMetrics);
                        power = (ushort?)Helper.GetValueBetweenDates(powerMetrics);

                        lattitude = (int?)Helper.GetValueBetweenDates(lattitudeMetrics);
                        longitude = (int?)Helper.GetValueBetweenDates(longitudeMetrics);
                    }

                    // Warning: added the equality symbol in the check: recordDate >= currentRecordDate
                    // the case is when the activity contains to many records with same data.
                    // if current functionality affected, revert the change 
                    if (currentRecordDate.HasValue && recordDate >= currentRecordDate && currentTimeFrame >= recordDate)
                    {
                        if (isActiveTime)
                        {
                            currentRecord = nextRcordMesg;
                        }

                        queue.Dequeue();
                    }

                    #region GradeCalulation
                    if (gradesEnumerator.Current.Key < currentTimeFrame)
                    {
                        currentGradePair = gradesEnumerator.Current;
                        gradesEnumerator.MoveNext();
                    }

                    if (currentGradePair.Key == currentTimeFrame)
                    {
                        grade = currentGradePair.Value;
                    }
                    else if (gradesEnumerator.Current.Key == currentTimeFrame)
                    {
                        grade = gradesEnumerator.Current.Value;
                    }
                    else if (currentGradePair.Key < currentTimeFrame
                        && currentTimeFrame < gradesEnumerator.Current.Key)
                    {
                        CalculateModel gradeMetrics = new(
                            currentGradePair.Key,
                            gradesEnumerator.Current.Key,
                            currentGradePair.Value,
                            gradesEnumerator.Current.Value,
                            currentTimeFrame);

                        grade = Helper.GetValueBetweenDates(gradeMetrics);
                    }
                    #endregion
                }

                bool inStartDate = rangeStartDate.HasValue == false
                    || (rangeStartDate.HasValue && rangeStartDate.Value <= currentTimeFrame);

                bool inEndDate = rangeEndDate.HasValue == false
                    || (rangeEndDate.HasValue && currentTimeFrame <= rangeEndDate.Value);

                if (inStartDate && inEndDate)
                {
                    if (isActiveTime)
                    {
                        altitude ??= currentRecord.Altitude;
                        speed ??= currentRecord.Speed;
                        distance ??= currentRecord.Distance;

                        longitude ??= currentRecord.Longitude;
                        lattitude ??= currentRecord.Lattitude;
                        power ??= currentRecord.Power;

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
                        IndexOfCurrentRecord = currentRecord.IndexOfRecord,
                        Longitude = lastKnownGpsLocation?.X,
                        Latitude = lastKnownGpsLocation?.Y,
                        Grade = grade,
                        Power = power,
                        ElapsedTime = TimeOnly.FromTimeSpan(currentTimeFrame - initializer.StartDate),
                        // It's good to take localTime from garmin settings 
                        CurrentTime = TimeOnly.FromDateTime(currentTimeFrame.ToLocalTime())
                    };

                    framesList.Add(frameData);
                }

                // data for future widgets
                // float? hr = currentRecord?.GetHeartRate(); // beats per minute  
                // sbyte? t = currentRecord?.GetTemperature();

                // https://fpstoms.com/ Example: 2 fps (500ms) = 1000 / 2; 
                int millsecondsStep = 1000 / fps;
                currentTimeFrame = currentTimeFrame.AddMilliseconds(millsecondsStep);

                if (isActiveTime)
                {
                    activeTimeDuration = activeTimeDuration.Add(TimeSpan.FromMilliseconds(millsecondsStep));
                }
            } while (currentTimeFrame <= initializer.EndDate);

            ConcurrentBag<(string, SKData)> zipEntries = [];
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

            IReadOnlyCollection<IWidget> widgets = widgetFactory.GetWidgets(widgetsIds, initializer.ChartDataStats);

            try
            {
                IEnumerable<Task> renderTasks = widgets.Select(widget =>
                 Task.Run(async () =>
                 {
                     await ImagesGenerator.GenerateDataForWidgetAsync(
                         sessionData,
                         framesList,
                         widget,
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
    }
}
