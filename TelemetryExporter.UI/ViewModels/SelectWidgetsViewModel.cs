using System.ComponentModel;

using SkiaSharp;

using TelemetryExporter.Core.Models;
using TelemetryExporter.Core.Utilities;

using TelemetryExporter.Core.Widgets.Elevation;

namespace TelemetryExporter.UI.ViewModels
{
    internal class SelectWidgetsViewModel : INotifyPropertyChanged
    {
        private ImageSource? elevationProfileImage;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <param name="fitFileStream">Steam of .fit file.</param>
        public SelectWidgetsViewModel(Stream fitFileStream)
        {
            var fitMessages = new FitDecoder(fitFileStream).FitMessages;
            ElevationWidget elevationProfile = new(fitMessages.RecordMesgs);

            SessionData sessionData = new()
            {
                MaxSpeed = fitMessages.RecordMesgs.Max(x => x.GetEnhancedSpeed()) * 3.6 ?? 0,
                TotalDistance = fitMessages.SessionMesgs[0].GetTotalDistance() ?? 0,
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

        public ImageSource? MyImage
        {
            get => elevationProfileImage;

            set
            {
                elevationProfileImage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MyImage)));
            }
        }

    }
}
