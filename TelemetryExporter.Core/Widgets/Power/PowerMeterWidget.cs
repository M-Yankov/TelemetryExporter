using SkiaSharp;

using TelemetryExporter.Core.Attributes;
using TelemetryExporter.Core.Models;
using TelemetryExporter.Core.Utilities;
using TelemetryExporter.Core.Widgets.Interfaces;

namespace TelemetryExporter.Core.Widgets.Power
{
    [WidgetData(Index = WidgetIndex, ExampleImagePath = "Images/ExamplePowerMeter.png", Category = TECoreContsants.Categories.Power)]
    public class PowerMeterWidget : GaugeBaseWidget, IWidget
    {
        private const int WidgetIndex = 10;
        public static int Index => WidgetIndex;

        public string Category => TECoreContsants.Categories.Power;

        public string Name => nameof(PowerMeterWidget);

        public Task<SKData> GenerateImage(SessionData sessionData, FrameData currentData)
        {
            return GetImageData(sessionData.MaxPower, currentData.Power ?? 0, "W");
        }
    }
}
