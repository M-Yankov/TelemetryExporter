using System.ComponentModel;
using System.Windows.Input;

using TelemetryExporter.UI.Resources;

namespace TelemetryExporter.UI.ViewModels
{
    public class HomePageViewModel : INotifyPropertyChanged
    {
        private string selectedFileName;

        // Button to be with cursor-hand
        // https://vladislavantonyuk.github.io/articles/Setting-a-cursor-for-.NET-MAUI-VisualElement/
        public HomePageViewModel()
        {
            OpenActivityFileCommand = new Command(DoPickActivityFile);
        }

        public ICommand OpenActivityFileCommand { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string SelectedFileName
        {
            get => $"Selected: {selectedFileName}";
            set
            {
                selectedFileName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedFileName)));
            }
        }


        // Not implemented!
        internal void DropGestureRecognizer_Drop(object? sender, DropEventArgs e)
        {

        }

        private async void DoPickActivityFile()
        {
            // .gpx files will be added in future
            string[] fileTiles = [TEConstants.Extensions.GarminActivity];

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

                if (result != null && string.Equals(
                    Path.GetExtension(result.FileName).ToLowerInvariant(),
                    TEConstants.Extensions.GarminActivity))
                {
                    Stream stream = await result.OpenReadAsync();
                    this.SelectedFileName = result.FileName;

                    Dictionary<string, object> navigationParameter = new()
                    {
                        { TEConstants.QueryKeys.FitStreamKey, stream },
                    };

                    await Shell.Current.GoToAsync(AppShellRouterConfig.WidgetsRoute, navigationParameter);
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
