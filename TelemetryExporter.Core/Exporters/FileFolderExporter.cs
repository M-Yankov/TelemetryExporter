using System.Collections.Concurrent;

using TelemetryExporter.Core.Models;
using TelemetryExporter.Core.Widgets.Interfaces;

namespace TelemetryExporter.Core.Exporters
{
    public class FileFolderExporter : IExporter
    {
        private readonly string saveDirectoryPath;
        private readonly CancellationToken cancellationToken;
        private int progressCounter = 0;
        private const int ThresHoldProgresUpdate = 100;

        public FileFolderExporter(string outputDirectory, CancellationToken cancellationToken = default)
        {
            this.cancellationToken = cancellationToken;

            this.saveDirectoryPath = outputDirectory;
            if (!Directory.Exists(saveDirectoryPath))
            {
                Directory.CreateDirectory(saveDirectoryPath);
            }
        }

        async Task IExporter.ExportImageData(
            IAsyncEnumerable<GeneratedWidgetDataModel> dataModels,
            ConcurrentDictionary<string, double> widgetDonePercentage,
            Action progressUpdateAction)
        {
            await foreach (GeneratedWidgetDataModel dataModel in dataModels.WithCancellation(this.cancellationToken))
            {
                IWidget widget = dataModel.Widget;
                string directoryPath = Path.Combine(saveDirectoryPath, widget.Category, widget.Name);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                string frameFilePath = Path.Combine(directoryPath, dataModel.Filename);
                using Stream fileStream = File.Create(frameFilePath);
                using Stream imageDataStream = dataModel.ImageData.AsStream();
                imageDataStream.CopyTo(fileStream);

                progressCounter++;
                widgetDonePercentage[widget.Name] = dataModel.PercentageDone;

                if (progressCounter % ThresHoldProgresUpdate == 0)
                {
                    progressUpdateAction();
                }
            }

            progressUpdateAction();
        }

        public string GetExportedDirectory() => saveDirectoryPath;

        public void Dispose()
        {
            if (this.cancellationToken.IsCancellationRequested
                && Directory.Exists(saveDirectoryPath))
            {
                Directory.Delete(saveDirectoryPath, true);
            }

            GC.SuppressFinalize(this);
        }
    }
}
