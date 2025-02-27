using System.ComponentModel;
using System.Windows.Input;

using CommunityToolkit.Maui.Storage;

using Dynastream.Fit;

using SkiaSharp;

using TelemetryExporter.Core.Exporters;
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
        private ExportType exportType = ExportType.ZipFileArchive;

        public SelectWidgetsViewModel()
        {
            WidgetFactory widgetFactory = new();

            widgetElements = widgetFactory.Widgets
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
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand ChooseSaveFolder => new Command<Entry>(PickUpFolder);

        public System.DateTime StartActivityDate { get; set; }

        public System.DateTime EndActivityDate { get; set; }

        public double TotalDistance { get; set; }

        public FitMessages FitMessages { get; private set; }

        public ExportType ExportType

        {
            get => exportType;
            set
            {
                if (exportType != value)
                {
                    exportType = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExportType)));
                }
            }
        }

        public IReadOnlyCollection<(System.DateTime start, System.DateTime end)> PausePeriods { get; private set; } = [];

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
            
            FitInitializer fitInitializer = FitInitializer.Initialize(FitMessages);
            ElevationWidget elevationProfile = new();
            elevationProfile.Initialize(fitInitializer.ChartDataStats);

            PausePeriods = fitInitializer.PausePeriods;
            TotalDistance = fitInitializer.Distance;

            StartActivityDate = fitInitializer.StartDate.ToLocalTime();
            EndActivityDate = fitInitializer.EndDate.ToLocalTime();

            /*
            uint? activityTimestamp = fitMessages.ActivityMesgs[0].GetLocalTimestamp();
            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(activityTimestamp.Value);
            TimeSpan timeSpanOffset = TimeZoneInfo.Local.GetUtcOffset(dateTimeOffset);
            timeSpanOffset.TotalHours
            */

            SessionData sessionData = new()
            {
                CountOfRecords = fitInitializer.Records.Count,
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
