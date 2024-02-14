using TelemetryExporter.UI.Resources;

namespace TelemetryExporter.UI
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = base.CreateWindow(activationState);
            if (window != null)
            {
                window.Title = TEConstants.ApplicationName;
            }

            return window;
        }
    }
}
