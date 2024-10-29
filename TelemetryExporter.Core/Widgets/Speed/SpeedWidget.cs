using SkiaSharp;

using TelemetryExporter.Core.Attributes;
using TelemetryExporter.Core.Models;
using TelemetryExporter.Core.Utilities;
using TelemetryExporter.Core.Widgets.Interfaces;

namespace TelemetryExporter.Core.Widgets.Speed
{
    [WidgetData(Index = WidgetIndex, ExampleImagePath = ImagePath, Category = TECoreContsants.Categories.Speed)]
    public class SpeedWidget : GaugeBaseWidget, IWidget
    {
        private const int WidgetIndex = 1;
        private const string ImagePath = "Images/ExampleSpeed.png";

        public static int Index => WidgetIndex;

        public string Category => TECoreContsants.Categories.Speed;

        public string Name => "SpeedWidget";

        public Task<SKData> GenerateImage(SessionData sessionData, FrameData currentData)
        {
            return GetImageData(sessionData.MaxSpeed, currentData.Speed, "KM/H");
        }
    }
}
