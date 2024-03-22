using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Input;

using CommunityToolkit.Maui.Storage;

using Dynastream.Fit;

using SkiaSharp;

using TelemetryExporter.Core.Attributes;
using TelemetryExporter.Core.Models;
using TelemetryExporter.Core.Utilities;
using TelemetryExporter.Core.Widgets.Elevation;
using TelemetryExporter.Core.Widgets.Interfaces;
using TelemetryExporter.UI.CustomModels;

namespace TelemetryExporter.UI.ViewModels
{
    internal class SelectWidgetsViewModel : INotifyPropertyChanged
    {
        private readonly ICollection<ExpanderDataItem> widgetElements;
        private ImageSource? elevationProfileImage;

        public SelectWidgetsViewModel()
        {
            Dictionary<string, ExpanderDataItem> widgetCategories = [];
            IEnumerable<WidgetDataAttribute> widgetDataCollection = Assembly
                .GetAssembly(typeof(Core.Program))!
                .GetTypes()
                .Where(t => t.IsAssignableTo(typeof(IWidget)) && t.IsClass)
                .Select(t => t.GetCustomAttribute<WidgetDataAttribute>()!);

            foreach (WidgetDataAttribute widgetData in widgetDataCollection)
            {
                if (widgetCategories.TryGetValue(widgetData.Category, out ExpanderDataItem? value))
                {
                    value.Widgets.Add(widgetData);
                }
                else
                {
                    widgetCategories[widgetData.Category] = new ExpanderDataItem() { Category = widgetData.Category, Widgets = [widgetData] };
                }
            };

            widgetElements = widgetCategories.Values;
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
            ElevationWidget elevationProfile = new(FitMessages.RecordMesgs);

            IEnumerable<RecordMesg> orderedMessages = FitMessages.RecordMesgs.OrderBy(x => x.GetTimestamp().GetDateTime());
            System.DateTime firstDate = orderedMessages.First().GetTimestamp().GetDateTime();
            System.DateTime lastDate = orderedMessages.Last().GetTimestamp().GetDateTime();

            /*
            uint? activityTimestamp = fitMessages.ActivityMesgs[0].GetLocalTimestamp();
            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(activityTimestamp.Value);
            TimeSpan timeSpanOffset = TimeZoneInfo.Local.GetUtcOffset(dateTimeOffset);
            timeSpanOffset.TotalHours
            */

            StartActivityDate = firstDate.ToLocalTime();
            EndActivityDate = lastDate.ToLocalTime();
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
                            PausePeriods.Add((eventMessage.GetTimestamp().GetDateTime().ToLocalTime(), nextEventMessage.GetTimestamp().GetDateTime().ToLocalTime()));
                            break;
                        }
                    }
                }
            }

            SessionData sessionData = new()
            {
                MaxSpeed = FitMessages.RecordMesgs.Max(x => x.GetEnhancedSpeed()) * 3.6 ?? 0,
                TotalDistance = this.TotalDistance,
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
    }
}
