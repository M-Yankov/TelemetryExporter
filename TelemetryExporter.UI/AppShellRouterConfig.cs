using TelemetryExporter.UI.Views;

namespace TelemetryExporter.UI
{
    internal class AppShellRouterConfig
    {
        public const string WidgetsRoute = "selectwidgets";

        public static void Configure()
        {
            Routing.RegisterRoute(WidgetsRoute, typeof(SelectWidgets));
        }
    }
}
