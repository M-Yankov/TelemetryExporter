using Dynastream.Fit;

namespace TelemetryExporter.Core.Widgets.Interfaces
{
    public interface INeedInitialization
    {
        void Initialize(IReadOnlyCollection<RecordMesg> dataMessages);
    }
}
