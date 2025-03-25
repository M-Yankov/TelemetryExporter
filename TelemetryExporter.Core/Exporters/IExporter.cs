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
        
        /// <summary>
        /// Specially for the zipArchive it will have .zip extension. <br/>
        /// Just to no avoid type checking.
        /// </summary>
        string GetExportedDirectory();
    }
}
