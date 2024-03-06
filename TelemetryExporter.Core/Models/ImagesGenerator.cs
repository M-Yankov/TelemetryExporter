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
            CancellationTokenSource cancellationToken,
            Action<SKData, IWidget, string> callBack)
        {
            foreach (FrameData frame in frameData)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                SKData generatedImageData = widget.GenerateImage(sessionData, frame);
                callBack(generatedImageData, widget, frame.FileName);
            }
        }

    }
}
