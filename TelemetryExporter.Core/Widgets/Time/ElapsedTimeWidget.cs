using SkiaSharp;

using TelemetryExporter.Core.Models;
using TelemetryExporter.Core.Utilities;
using TelemetryExporter.Core.Widgets.Interfaces;

namespace TelemetryExporter.Core.Widgets.Time
{
    /// <summary>
    /// May not work correctly for activities continued longer than 24H.
    /// Results of this widget could show how the program deals with FPS. 
    /// </summary>
    public class ElapsedTimeWidget : TextBaseWidget, IWidget
    {
        public string Category => TECoreContsants.Categories.Time;

        public string Name => nameof(ElapsedTimeWidget);

        public string DisplayName => "Elapsed Time";

        public string ImagePath => "Images/ExampleElapsed.png";

        public override int WidgetWidth => 250;

        public override int WidgetHeight => 50;

        public Task<SKData> GenerateImage(SessionData sessionData, FrameData currentData)
        {
           return GetImageData(currentData.ElapsedTime.ToString("HH:mm:ss.fff"));
        }
    }
}
