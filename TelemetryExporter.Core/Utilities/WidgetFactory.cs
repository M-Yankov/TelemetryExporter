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
        private readonly Dictionary<int, IWidget> widgets;

        public WidgetFactory()
        {
            IWidget[] widgetsArray = [
                new DistanceWidget(), new TraceWidget(), new ElevationWidget(),
                new GradeWidget(), new PaceWidget(), new PowerTextWidget(),
                new SpeedWidget(), new CurrentTimeWidget(), new ElapsedTimeWidget(),
                new PowerMeterWidget()
                ];

            this.widgets = [];
            for (int i = 0; i < widgetsArray.Length; i++)
            {
                int widgetIndex = i + 1;
                this.widgets[widgetIndex] = widgetsArray[i];
            }
        }

        public IReadOnlyDictionary<int, IWidget> Widgets
            => this.widgets.AsReadOnly();

        public IReadOnlyCollection<IWidget> GetWidgets(
            IEnumerable<int> widgetIds,
            IReadOnlyCollection<ChartDataModel> chartDataStats)
        {
            List<IWidget> widgets = [];
            foreach (int id in widgetIds)
            {
                IWidget? widget = this.GetWidget(id);
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

        private IWidget? GetWidget(int id)
        {
            IWidget? widget = null;

            if (this.widgets.ContainsKey(id))
            {
                widget = widgets[id];
                return widget;
            }

            return widget;
        }
    }
}
