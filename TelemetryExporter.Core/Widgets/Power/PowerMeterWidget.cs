using SkiaSharp;

using TelemetryExporter.Core.Models;
using TelemetryExporter.Core.Utilities;
using TelemetryExporter.Core.Widgets.Interfaces;

namespace TelemetryExporter.Core.Widgets.Power
{
    public class PowerMeterWidget : GaugeBaseWidget, IWidget
    {
        public string Category => TECoreContsants.Categories.Power;

        public string Name => nameof(PowerMeterWidget);

        public string ImagePath => "Images/ExamplePowerMeter.png";

        public Task<SKData> GenerateImage(SessionData sessionData, FrameData currentData)
        {
            return GetImageData(sessionData.MaxPower, currentData.Power ?? 0, "W");
        }
    }
}
