using TelemetryExporter.Console.Widgets.Interfaces;

namespace TelemetryExporter.Console.Models
{
    internal class ImagesGenerator
    {
        public static async Task GenerateDataForWidgetAsync(SessionData sessionData, IReadOnlyCollection<FrameData> frameData, IWidget widget)
        {
            foreach (FrameData frame in frameData)
            {
                await widget.GenerateImageAsync(sessionData, frame);
            }
        }

    }
}
