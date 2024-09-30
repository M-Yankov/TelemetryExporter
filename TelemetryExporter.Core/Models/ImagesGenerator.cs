using SkiaSharp;

using TelemetryExporter.Core.Widgets.Interfaces;

namespace TelemetryExporter.Core.Models
{
    internal class ImagesGenerator
    {
        public static async Task GenerateDataForWidgetAsync(
            SessionData sessionData,
            IReadOnlyCollection<FrameData> frameData,
            IWidget widget,
            Action<SKData, IWidget, string, double> callBack,
            CancellationToken cancellationToken)
        {
            for (int i = 0; i < frameData.Count; i++)
            {
                FrameData frame = frameData.ElementAt(i);
                double percentageDone = (double)(i + 1) / frameData.Count;
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                SKData generatedImageData = await widget.GenerateImage(sessionData, frame);
                callBack(generatedImageData, widget, frame.FileName, percentageDone);
            }
        }
    }
}
