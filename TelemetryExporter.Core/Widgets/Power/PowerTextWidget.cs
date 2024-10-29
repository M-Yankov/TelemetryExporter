using SkiaSharp;

using TelemetryExporter.Core.Attributes;
using TelemetryExporter.Core.Models;
using TelemetryExporter.Core.Utilities;
using TelemetryExporter.Core.Widgets.Interfaces;

namespace TelemetryExporter.Core.Widgets.Power
{
    [WidgetData(Index = WidgetIndex, ExampleImagePath = "Images/ExamplePowerTextWidget.png", Category = TECoreContsants.Categories.Power)]
    public class PowerTextWidget : TextBaseWidget, IWidget
    {
        private const int WidgetIndex = 9;

        public static int Index => WidgetIndex;

        public string Category => TECoreContsants.Categories.Power;

        public string Name => nameof(PowerTextWidget);

        public override int WidgetWidth => 136;

        public override int WidgetHeight => 50;

        public Task<SKData> GenerateImage(SessionData sessionData, FrameData currentData)
        {
            string text = currentData.Power.HasValue
                ? $"{currentData.Power.Value,4} W"
                : "---- W";

            return GetImageData(text);
        }
    }
}
