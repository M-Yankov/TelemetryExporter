using SkiaSharp;

using TelemetryExporter.Core.Models;
using TelemetryExporter.Core.Utilities;
using TelemetryExporter.Core.Widgets.Interfaces;

namespace TelemetryExporter.Core.Widgets.Speed
{
    public class SpeedWidget : GaugeBaseWidget, IWidget
    {
        private const int WidgetIndex = 1;

        public static int Index => WidgetIndex;

        public string Category => TECoreContsants.Categories.Speed;

        public string Name => "SpeedWidget";

        public string DisplayName => "Speed";

        public string ImagePath => "Images/ExampleSpeed.png";

        public Task<SKData> GenerateImage(SessionData sessionData, FrameData currentData)
        {
            return GetImageData(sessionData.MaxSpeed, currentData.Speed, "KM/H");
        }
    }
}
