using System.ComponentModel;
using System.Windows.Input;

using CommunityToolkit.Maui.Storage;

using Dynastream.Fit;

using SkiaSharp;

using TelemetryExporter.Core.ComparerModels;
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
        private List<FitMessageModel> recordMessages = [];

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
            recordMessages = [.. fitInitializer.Records];

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

        /// <summary>
        /// CoPilot generated.
        /// </summary>
        public FitMessageModel GetClosestFitMessage(System.DateTime date)
        {
            // Need to convert to UTC, because the UI works with dates in local time
            System.DateTime searchDateTime = date.ToUniversalTime();
            int index = recordMessages.BinarySearch(
                new FitMessageModel { RecordDateTime = searchDateTime },
                new FitMessageModelComparer());

            if (index >= 0)
            {
                // Exact match found
                return recordMessages[index];
            }
            else
            {
                // No exact match found, calculate the closest
                int nextIndex = ~index;
                int prevIndex = nextIndex - 1;

                if (nextIndex >= recordMessages.Count)
                {
                    // The date is after the last element
                    return recordMessages[prevIndex];
                }
                else if (prevIndex < 0)
                {
                    // The date is before the first element
                    return recordMessages[nextIndex];
                }
                else
                {
                    // The date is between two elements, find the closest one
                    TimeSpan prevDiff = (searchDateTime - recordMessages[prevIndex].RecordDateTime).Duration();
                    TimeSpan nextDiff = (recordMessages[nextIndex].RecordDateTime - searchDateTime).Duration();

                    return prevDiff <= nextDiff ? recordMessages[prevIndex] : recordMessages[nextIndex];
                }
            }
        }
    }
}
