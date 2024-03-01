using System.ComponentModel;
using System.Reflection;
using System.Windows.Input;

using CommunityToolkit.Maui.Storage;

using Dynastream.Fit;

using SkiaSharp;

using TelemetryExporter.Core.Attributes;
using TelemetryExporter.Core.Models;
using TelemetryExporter.Core.Utilities;
using TelemetryExporter.Core.Widgets.Distance;
using TelemetryExporter.Core.Widgets.Elevation;
using TelemetryExporter.Core.Widgets.Pace;
using TelemetryExporter.Core.Widgets.Speed;
using TelemetryExporter.Core.Widgets.Trace;
using TelemetryExporter.UI.CustomModels;

namespace TelemetryExporter.UI.ViewModels
{
    internal class SelectWidgetsViewModel : INotifyPropertyChanged
    {
        private ImageSource? elevationProfileImage;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand ChooseSaveFolder => new Command<Entry>(PickUpFolder);

        public System.DateTime StartActivityDate { get; set; }

        public System.DateTime EndActivityDate { get; set; }

        public double TotalDistance { get; set; }

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
            get => [
                new ExpanderDataItem()
                {
                    Category = "Speed",
                    Widgets = [typeof(SpeedWidget).GetCustomAttribute<WidgetDataAttribute>()!, typeof(DistanceWidget).GetCustomAttribute<WidgetDataAttribute>()!]
                },
                new ExpanderDataItem()
                {
                    Category = "Elevation",
                    Widgets = [typeof(ElevationWidget).GetCustomAttribute<WidgetDataAttribute>()!]
                },
                new ExpanderDataItem()
                {
                    Category = "Pace",
                    Widgets = [typeof(PaceWidget).GetCustomAttribute<WidgetDataAttribute>()!]
                },
                new ExpanderDataItem()
                {
                    Category = "Trace",
                    Widgets = [typeof(TraceWidget).GetCustomAttribute<WidgetDataAttribute>()!]
                },
                new ExpanderDataItem()
                {
                    Category = "Distance",
                    Widgets = [typeof(DistanceWidget).GetCustomAttribute<WidgetDataAttribute>()!]
                },
            ];
        }

        /// <param name="fitFileStream">Steam of .fit file.</param>
        public void Initialize(Stream fitFileStream)
        {
            var fitMessages = new FitDecoder(fitFileStream).FitMessages;
            ElevationWidget elevationProfile = new(fitMessages.RecordMesgs);

            IEnumerable<RecordMesg> orderedMessages = fitMessages.RecordMesgs.OrderBy(x => x.GetTimestamp().GetDateTime());
            System.DateTime firstDate = orderedMessages.First().GetTimestamp().GetDateTime();
            System.DateTime lastDate = orderedMessages.Last().GetTimestamp().GetDateTime();

            uint? activityTimestamp = fitMessages.ActivityMesgs[0].GetLocalTimestamp();
            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(activityTimestamp.Value);
            TimeSpan timeSpanOffset = TimeZoneInfo.Local.GetUtcOffset(dateTimeOffset);

            StartActivityDate = firstDate.AddHours(timeSpanOffset.TotalHours);
            EndActivityDate = lastDate.AddHours(timeSpanOffset.TotalHours);
            TotalDistance = fitMessages.SessionMesgs[0].GetTotalDistance() ?? 0;

            SessionData sessionData = new()
            {
                MaxSpeed = fitMessages.RecordMesgs.Max(x => x.GetEnhancedSpeed()) * 3.6 ?? 0,
                TotalDistance = this.TotalDistance,
                CountOfRecords = fitMessages.RecordMesgs.Count
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
            });

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
