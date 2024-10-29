using SkiaSharp;

using TelemetryExporter.Core.Attributes;
using TelemetryExporter.Core.Models;
using TelemetryExporter.Core.Utilities;
using TelemetryExporter.Core.Widgets.Interfaces;

namespace TelemetryExporter.Core.Widgets.Time
{
    /// <summary>
    /// May not work correctly for activities continued longer than 24H.
    /// Results of this widget could show how the program deals with FPS. 
    /// </summary>
    [WidgetData(Index = WidgetIndex, ExampleImagePath = "Images/ExampleElapsed.png", Category = TECoreContsants.Categories.Time)]
    public class ElapsedTimeWidget : TextBaseWidget, IWidget
    {
        private const int WidgetIndex = 6;

        public static int Index => WidgetIndex;

        public string Category => TECoreContsants.Categories.Time;

        public string Name => nameof(ElapsedTimeWidget);

        public override int WidgetWidth => 250;

        public override int WidgetHeight => 50;

        public Task<SKData> GenerateImage(SessionData sessionData, FrameData currentData)
        {
           return GetImageData(currentData.ElapsedTime.ToString("HH:mm:ss.fff"));
        }
    }
}
