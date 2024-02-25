using TelemetryExporter.Core.Widgets.Interfaces;

namespace TelemetryExporter.Core.Models
{
    internal class ImagesGenerator
    {
        public static async Task GenerateDataForWidgetAsync(SessionData sessionData, IReadOnlyCollection<FrameData> frameData, IWidget widget)
        {
            foreach (FrameData frame in frameData)
            {
                var _ = widget.GenerateImage(sessionData, frame);
            }
        }

    }
}
