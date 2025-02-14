using System.Runtime.CompilerServices;

using SkiaSharp;

using TelemetryExporter.Core.Widgets.Interfaces;

namespace TelemetryExporter.Core.Models
{
    internal class ImagesGenerator
    {
        public static async IAsyncEnumerable<GeneratedWidgetDataModel> GenerateDataForWidgetAsync(
            SessionData sessionData,
            IReadOnlyCollection<FrameData> framesList,
            IWidget widget,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (int i = 0; i < framesList.Count; i++)
            {
                FrameData frameData = framesList.ElementAt(i);
                double percentageDone = (double)(i + 1) / framesList.Count;
                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }

                SKData skData = await widget.GenerateImage(sessionData, frameData);
                yield return new GeneratedWidgetDataModel()
                {
                    Filename = frameData.FileName,
                    SkData = skData,
                    Widget = widget,
                    PercentageDone = percentageDone
                };
            }
        }
    }
}
