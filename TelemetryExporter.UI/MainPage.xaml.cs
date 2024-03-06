using TelemetryExporter.UI.ViewModels;

namespace TelemetryExporter.UI
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            Title = "";
            HomePageViewModel homePageViewModel = new ();
            BrowseButton.Command = homePageViewModel.OpenActivityFileCommand;
            
            // elevationImage.Source = homePageViewModel.MyImage;
            BindingContext = homePageViewModel;

            HomeScreenDropGestureRecognizer.Drop += homePageViewModel.DropGestureRecognizer_Drop;
            LabelFileName.SetBinding(Label.TextProperty, new Binding(nameof(homePageViewModel.SelectedFileName), source: homePageViewModel));
            browseFileLoader.SetBinding(ActivityIndicator.IsRunningProperty, new Binding(nameof(homePageViewModel.IsLoading), source: homePageViewModel));
            BrowseButton.SetBinding(Button.IsEnabledProperty, new Binding(nameof(homePageViewModel.BrowseButtonEnabled), source: homePageViewModel));

            /*
                new Button().Behaviors;
                new DropGestureRecognizer();
                new GestureRecognizers();
                new VerticalStackLayout().GestureRecognizers[0];
                new DropGestureRecognizer().DropCommand
            */
        }

        // SemanticScreenReader.Announce(CounterBtn.Text);

        // https://vladislavantonyuk.github.io/articles/Drag-and-Drop-any-content-to-a-.NET-MAUI-application/
        private void DropGestureRecognizer_DragOver(object sender, DragEventArgs e)
        {
            TheBorder.Stroke = Colors.Red;
        }

        private void DropGestureRecognizer_DragLeave(object sender, DragEventArgs e)
        {
            TheBorder.Stroke = Colors.White;
        }
    }
}
