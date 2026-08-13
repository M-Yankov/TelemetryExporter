using TelemetryExporter.UI.Resources;

namespace TelemetryExporter.UI
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
            => new(new AppShell())
            {
                Title = TEConstants.ApplicationName
            };
    }
}
