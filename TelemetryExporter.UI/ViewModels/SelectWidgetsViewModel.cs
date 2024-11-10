using System.ComponentModel;
using System.Windows.Input;

using CommunityToolkit.Maui.Storage;

using Dynastream.Fit;

using SkiaSharp;

using TelemetryExporter.Core.Models;
using TelemetryExporter.Core.Utilities;
using TelemetryExporter.Core.Widgets.Elevation;
using TelemetryExporter.UI.CustomModels;

namespace TelemetryExporter.UI.ViewModels
{
    internal class SelectWidgetsViewModel : INotifyPropertyChanged
    {
        private readonly ICollection<ExpanderDataItem> widgetElements;
        private ImageSource? elevationProfileImage;

        public SelectWidgetsViewModel()
        {
            WidgetFactory widgetFactory = new();

            widgetElements = widgetFactory.GetWidgets
                .Select(x => new WidgetData() 
                { 
                    Category = x.Value.Category,
                    ImagePath = x.Value.ImagePath,
                    Index = x.Key
                })
                .GroupBy(x => x.Category)
                .Select(x => new ExpanderDataItem()
                {
                    Category = x.Key,
                    Widgets = [.. x]
                })
                .ToList();

            Dictionary<string, ExpanderDataItem> widgetCategories = [];
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand ChooseSaveFolder => new Command<Entry>(PickUpFolder);

        public System.DateTime StartActivityDate { get; set; }

        public System.DateTime EndActivityDate { get; set; }

        public double TotalDistance { get; set; }

        public FitMessages FitMessages { get; private set; }

        public List<(System.DateTime start, System.DateTime end)> PausePeriods { get; private set; } = [];

        public ImageSource? MyImage
        {
            get => elevationProfileImage;

            set
            {
                elevationProfileImage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MyImage)));
            }
        }

        public ICollection<ExpanderDataItem> WidgetCategories
        {
            get => widgetElements;
        }

        /// <param name="fitFileStream">Steam of .fit file.</param>
        public void Initialize(Stream fitFileStream)
        {
            FitMessages = new FitDecoder(fitFileStream).FitMessages;
            ElevationWidget elevationProfile = new();
            List<ChartDataModel> charDataStats = FitMessages.RecordMesgs
                .Select(x => new ChartDataModel()
                {
                    Altitude = x.GetEnhancedAltitude(),
                    Latitude = x.GetPositionLat(),
                    Longitude = x.GetPositionLong(),
                    RecordDateTime = x.GetTimestamp().GetDateTime().ToLocalTime(),
                })
                .ToList();

            elevationProfile.Initialize(charDataStats);

            IEnumerable<RecordMesg> orderedMessages = FitMessages.RecordMesgs.OrderBy(x => x.GetTimestamp().GetDateTime());
            StartActivityDate = orderedMessages.First().GetTimestamp().GetDateTime().ToLocalTime();
            EndActivityDate = orderedMessages.Last().GetTimestamp().GetDateTime().ToLocalTime();

            /*
            uint? activityTimestamp = fitMessages.ActivityMesgs[0].GetLocalTimestamp();
            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(activityTimestamp.Value);
            TimeSpan timeSpanOffset = TimeZoneInfo.Local.GetUtcOffset(dateTimeOffset);
            timeSpanOffset.TotalHours
            */

            TotalDistance = FitMessages.SessionMesgs[0].GetTotalDistance() ?? 0;

            List<EventMesg> timerEvents = FitMessages.EventMesgs
                .Where(e => e.GetEvent() == Event.Timer)
                .OrderBy(e => e.GetTimestamp().GetDateTime()).ToList();

            for (int i = 0; i < timerEvents.Count; i++)
            {
                EventMesg eventMessage = timerEvents[i];
                EventType? eventType = eventMessage.GetEventType();

                if (eventType.HasValue
                    && (eventType.Value == EventType.Stop || eventType.Value == EventType.StopAll))
                {
                    for (int y = ++i; y < timerEvents.Count; y++, i++)
                    {
                        EventMesg nextEventMessage = timerEvents[y];
                        EventType? nextEventType = nextEventMessage.GetEventType();

                        if (nextEventType.HasValue && nextEventType == EventType.Start)
                        {
                            System.DateTime stopEventTime = eventMessage.GetTimestamp().GetDateTime().ToLocalTime();
                            System.DateTime startEventTime = nextEventMessage.GetTimestamp().GetDateTime().ToLocalTime();
                            PausePeriods.Add((stopEventTime, startEventTime));

                            // AjdustStartEndTimes(stopEventTime, startEventTime);

                            break;
                        }
                    }
                }
            }

            SessionData sessionData = new()
            {
                CountOfRecords = FitMessages.RecordMesgs.Count
            };

            SKData data = elevationProfile.GenerateImage(sessionData, new FrameData()
            {
                FileName = string.Empty,
                Altitude = null,
                Distance = 0,
                Latitude = 0,
                IndexOfCurrentRecord = default,
                Longitude = default,
                Speed = default
            }).GetAwaiter().GetResult();

            MemoryStream memoryStream = new(data.ToArray());

            MyImage = ImageSource.FromStream(() => memoryStream);
        }

        private async void PickUpFolder(Entry saveLocationEntry)
        {
            var result = await FolderPicker.Default.PickAsync();
            if (result.IsSuccessful)
            {
                saveLocationEntry.Text = Path.Combine(result.Folder.Path);
            }
        }

        /// <summary>
        /// This can fix the logic when activity immediately went into paused state after start.
        /// </summary>
        private void AjdustStartEndTimes(System.DateTime date1, System.DateTime date2)
        {
            if (date1 < StartActivityDate
                || date2 < StartActivityDate)
            {
                StartActivityDate = date1 < date2 ? date1 : date2;
            }

            if (EndActivityDate < date1
                || EndActivityDate < date2)
            {
                EndActivityDate = date1 > date2 ? date1 : date2;
            }
        }
    }
}
