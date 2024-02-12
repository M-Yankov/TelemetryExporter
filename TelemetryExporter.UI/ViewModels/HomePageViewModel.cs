using System.ComponentModel;
using System.Windows.Input;

using SkiaSharp;

using TelemetryExporter.Core.Models;
using TelemetryExporter.Core.Utilities;
using TelemetryExporter.Core.Widgets.Elevation;


namespace TelemetryExporter.UI.ViewModels
{
    public class HomePageViewModel : INotifyPropertyChanged
    {
        private ImageSource? elevationProfileImage;

        public event PropertyChangedEventHandler? PropertyChanged;

        // Button to be with cursor-hand
        // https://vladislavantonyuk.github.io/articles/Setting-a-cursor-for-.NET-MAUI-VisualElement/
        public HomePageViewModel()
        {
            OpenActivityFileCommand = new Command(() => DoPickActivityFile());
        }

        public ICommand OpenActivityFileCommand { get; set; }

        public ImageSource? MyImage
        {
            get => elevationProfileImage;

            set
            {
                elevationProfileImage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MyImage)));
            }
        }

        private async void DoPickActivityFile()
        {
            // .gpx files will be added in future
            string[] fileTiles = [".fit"];

            FilePickerFileType customFileType =
                new(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, fileTiles },
                    { DevicePlatform.macOS, fileTiles },
                    /* currently do need for other systems
                     * { DevicePlatform.iOS, new[] { "public.my.comic.extension" } }, // or general UTType values
                    { DevicePlatform.Android, new[] { "application/comics" } },
                    { DevicePlatform.Tizen, new[] { "* / *" } },
                    */
                });

            PickOptions options = new()
            {
                PickerTitle = "Select your .fit file activity from Garmin",
                FileTypes = customFileType,
            };

            await PickAndShow(options);
        }

        private async Task<FileResult?> PickAndShow(PickOptions options)
        {
            try
            {
                FileResult? result = await FilePicker.PickAsync(options);

                if (result != null)
                {
                    // var size = await GetStreamSizeAsync(result);

                    // Text = $"File Name: {result.FileName} ({size:0.00} KB)";

                    string ext = Path.GetExtension(result.FileName).ToLowerInvariant();
                    if (ext == ".fit")
                    {
                        Stream stream = await result.OpenReadAsync();

                        var fitMessages = new FitDecoder(stream).FitMessages;
                        ElevationWidget elevationProfile = new(fitMessages.RecordMesgs);

                        SessionData sessionData = new()
                        {
                            MaxSpeed = fitMessages.RecordMesgs.Max(x => x.GetEnhancedSpeed()) * 3.6 ?? 0,
                            TotalDistance = fitMessages.SessionMesgs[0].GetTotalDistance() ?? 0,
                            CountOfRecords = fitMessages.RecordMesgs.Count
                        };

                        const string FileName = "elevationProfile.png";
                        SKData data = elevationProfile.GenerateImage(sessionData, new FrameData()
                        {
                            FileName = FileName,
                            Altitude = null,
                            Distance = 0,
                            Latitude = 0,
                            IndexOfCurrentRecord = default,
                            Longitude = default,
                            Speed = default
                        });

                        MemoryStream memoryStream = new(data.ToArray());
                        // data.SaveTo(memoryStream);
                        //using Stream s = data.AsStream();
                        //await s.CopyToAsync(memoryStream);
                        //await memoryStream.FlushAsync();

                        byte[] a = File.ReadAllBytes(@"C:\Users\M.Yankov\Documents\GitHub\TelemetryExporter\TelemetryExporter.UI\Resources\Images\dotnet_bot.png");



                        MyImage = ImageSource.FromStream(() => memoryStream);

                        // IsImageVisible = true;
                    }
                    else
                    {
                        /// IsImageVisible = false;
                    }
                }
                else
                {
                    // Text = $"Pick canceled.";
                }

                return result;
            }
            catch (Exception ex)
            {
                // Text = ex.ToString();
                // IsImageVisible = false;
                return null;
            }
        }
    }
}
