using System.Collections.Concurrent;

using TelemetryExporter.Core.Models;

namespace TelemetryExporter.Core.Exporters
{
    internal interface IExporter : IDisposable
    {
        Task ExportImageData(
            IAsyncEnumerable<GeneratedWidgetDataModel> dataModels,
            ConcurrentDictionary<string, double> widgetDonePercentage,
            Action progressUpdateAction);
    }
}
