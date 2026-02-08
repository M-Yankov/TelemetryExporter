using TelemetryExporter.Core.Models;
using TelemetryExporter.Core.Widgets.Distance;
using TelemetryExporter.Core.Widgets.Elevation;
using TelemetryExporter.Core.Widgets.Grade;
using TelemetryExporter.Core.Widgets.Interfaces;
using TelemetryExporter.Core.Widgets.Pace;
using TelemetryExporter.Core.Widgets.Power;
using TelemetryExporter.Core.Widgets.Speed;
using TelemetryExporter.Core.Widgets.Time;
using TelemetryExporter.Core.Widgets.Trace;

namespace TelemetryExporter.Core.Utilities
{
    public class WidgetFactory
    {
        private static readonly Dictionary<int, IWidget> widgets = [];

        static WidgetFactory()
        {
            // don't want to work with zero index due to the default value of int
            // and avoid confusion while debugging.
            int startIndex = 1;
            widgets[startIndex++] = new DistanceWidget();
            widgets[startIndex++] = new TraceWidget();
            widgets[startIndex++] = new ElevationWidget();
            widgets[startIndex++] = new GradeWidget();
            widgets[startIndex++] = new PaceWidget();
            widgets[startIndex++] = new PowerTextWidget();
            widgets[startIndex++] = new SpeedWidget();
            widgets[startIndex++] = new CurrentTimeWidget();
            widgets[startIndex++] = new ElapsedTimeWidget();
            widgets[startIndex++] = new PowerMeterWidget();
        }

        public static IReadOnlyDictionary<int, IWidget> Widgets
            => widgets.AsReadOnly();

        public static IReadOnlyCollection<IWidget> GetWidgets(
            IEnumerable<int> widgetIds,
            IReadOnlyCollection<ChartDataModel> chartDataStats)
        {
            List<IWidget> widgets = [];
            foreach (int id in widgetIds)
            {
                IWidget? widget = GetWidget(id);
                if (widget != null)
                {
                    if (widget is INeedInitialization widgetForInitialization)
                    {
                        widgetForInitialization.Initialize(chartDataStats);
                    }

                    widgets.Add(widget);
                }
            }

            return widgets;
        }

        public static IWidget? GetWidget(int id)
        {
            if (widgets.TryGetValue(id, out IWidget? value))
            {
                return value;
            }

            return null;
        }
    }
}
