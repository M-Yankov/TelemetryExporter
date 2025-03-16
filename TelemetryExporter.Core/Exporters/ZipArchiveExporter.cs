using SkiaSharp;

using System.Collections.Concurrent;
using System.IO.Compression;

using TelemetryExporter.Core.Models;
using TelemetryExporter.Core.Widgets.Interfaces;

namespace TelemetryExporter.Core.Exporters
{
    internal class ZipArchiveExporter : IExporter, IDisposable
    {
        private const int ThresHoldProgresUpdate = 100;
        private readonly ZipArchive zipArchive;
        private readonly string tempZipFileDirectory;
        private readonly string saveDirectoryPath;
        private readonly CancellationToken cancellationToken;
        private readonly SemaphoreSlim semaphore = new(1, 1);
        private int processedCounter = 0;

        public ZipArchiveExporter(string tempDirectoryPath, string outputDirectory, CancellationToken cancellationToken = default)
        {
            this.cancellationToken = cancellationToken;

            if (string.IsNullOrWhiteSpace(Path.GetExtension(outputDirectory)))
            {
                outputDirectory += ".zip";
            }

            this.saveDirectoryPath = outputDirectory;
            if (!Directory.Exists(tempDirectoryPath))
            {
                Directory.CreateDirectory(tempDirectoryPath);
            }

            string genratedFileName = $"{Guid.NewGuid()}.zip";

            tempZipFileDirectory = Path.Combine(tempDirectoryPath, genratedFileName);
            FileStream tempDirectoryStream = new(tempZipFileDirectory, FileMode.OpenOrCreate, FileAccess.Write);
            zipArchive = new(tempDirectoryStream, ZipArchiveMode.Create);
        }

        /// <summary>
        /// This method will be used from multiple threads, from single instance. <para/>
        /// Not good approach to know how the method will be used, but it will either work in single thread.
        /// That's why it's not create a zipStream inside.
        /// </summary>
        public async Task ExportImageData(
            IAsyncEnumerable<GeneratedWidgetDataModel> dataModels,
            ConcurrentDictionary<string, double> widgetDonePercentage,
            Action updateProgressAction)
        {
            List<Task> exportTasks = [];
            await foreach (GeneratedWidgetDataModel dataModel in dataModels.WithCancellation(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                SKData imageData = dataModel.ImageData;
                string fileNameOfFrame = dataModel.Filename;
                IWidget widget = dataModel.Widget;
                double percentage = dataModel.PercentageDone;

                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    string zipEntryPath = Path.Combine(widget.Category, widget.Name, fileNameOfFrame);
                    exportTasks.Add(ExportImageDataInternal(zipEntryPath, imageData));
                }
                finally
                {
                    semaphore.Release();
                }

                processedCounter++;
                widgetDonePercentage[widget.Name] = percentage;

                if (processedCounter % ThresHoldProgresUpdate == 0)
                {
                    updateProgressAction();
                }
            }

            await Task.WhenAll(exportTasks);
            updateProgressAction();
        }

        public string GetExportedDirectory() => saveDirectoryPath;

        public void Dispose()
        {
            // Don't invoke tempDirectoryStream.Dispose();
            // It's invoked internally form zipArchive.Dispose().
            zipArchive.Dispose();

            if (!cancellationToken.IsCancellationRequested)
            {
                File.Move(tempZipFileDirectory, saveDirectoryPath);
            }

            File.Delete(tempZipFileDirectory);
        }

        private Task ExportImageDataInternal(string zipEntryPath, SKData imageData)
        {
            ZipArchiveEntry entry = zipArchive.CreateEntry(zipEntryPath);
            using Stream streamZipFile = entry.Open();
            using Stream imageDataStream = imageData.AsStream();
            imageDataStream.CopyTo(streamZipFile);
            return Task.CompletedTask;
        }
    }
}
