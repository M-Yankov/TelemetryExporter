using TelemetryExporter.Core.Models;

namespace TelemetryExporter.Core.Widgets.Interfaces
{
    public interface INeedInitialization
    {
        void Initialize(IReadOnlyCollection<ChartDataModel> dataMessages);
    }
}
