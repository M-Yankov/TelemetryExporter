using SkiaSharp;

using TelemetryExporter.Core.Attributes;
using TelemetryExporter.Core.Models;
using TelemetryExporter.Core.Utilities;
using TelemetryExporter.Core.Widgets.Interfaces;

namespace TelemetryExporter.Core.Widgets.Time
{
    /// <summary>
    /// May not work correctly for activities continued longer than 24H or if the activity passes midnight.
    /// </summary>
    [WidgetData(Index = WidgetIndex, ExampleImagePath = "Images/ExampleTime.png", Category = TECoreContsants.Categories.Time)]
    public class CurrentTimeWidget : TextBaseWidget, IWidget
    {
        private const int WidgetIndex = 7;

        public static int Index => WidgetIndex;

        public string Category => TECoreContsants.Categories.Time;

        public string Name => nameof(CurrentTimeWidget);

        public override int WidgetWidth => 170;

        public override int WidgetHeight => 50;

        public Task<SKData> GenerateImage(SessionData sessionData, FrameData currentData)
        {
            string text = currentData.CurrentTime.ToString("HH:mm:ss");
            return GetImageData(text);
        }
    }
}
