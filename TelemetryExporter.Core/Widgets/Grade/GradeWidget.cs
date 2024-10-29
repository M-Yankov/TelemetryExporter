using SkiaSharp;

using TelemetryExporter.Core.Attributes;
using TelemetryExporter.Core.Models;
using TelemetryExporter.Core.Utilities;
using TelemetryExporter.Core.Widgets.Interfaces;

namespace TelemetryExporter.Core.Widgets.Grade
{
    [WidgetData(Index = WidgetIndex, ExampleImagePath = ImagePath, Category = WidgetCategory)]
    public class GradeWidget : IWidget
    {
        private const string ImagePath = "Images/ExampleGrade.png";
        private const string WidgetCategory = TECoreContsants.Categories.Grade;
        private const int WidgetIndex = 8;

        public static int Index => WidgetIndex;

        public string Category => WidgetCategory;

        public string Name => nameof(GradeWidget);

        public Task<SKData> GenerateImage(SessionData sessionData, FrameData currentData)
        {
            SKImageInfo info = new(150, 250, SKImageInfo.PlatformColorType, SKAlphaType.Unpremul);
            using SKPaint transparentPaint = new()
            {
                Color = new SKColor(0, 0, 0, 100),
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            using SKSurface surface = SKSurface.Create(info);
            using SKCanvas canvas = surface.Canvas;
            canvas.DrawPaint(transparentPaint);

            using SKPaint textPaint = new()
            {
                Color = SKColors.White,
                TextSize = 60,
                TextAlign = SKTextAlign.Center,
                Typeface = SKTypeface.FromFamilyName("Consolas"),
                IsAntialias = true,
            };

            if (currentData.Grade.HasValue 
                && !double.IsInfinity(currentData.Grade.Value)
                && !double.IsNaN(currentData.Grade.Value))
            {
                // Check if the calculation could come directly 
                canvas.DrawText((currentData.Grade.Value / 100).ToString("p0"), new SKPoint(75, 100), textPaint);
                canvas.SaveLayer();

                // rotateDegree = skPointTranslateX
                // -90 = 85 = 10
                // -45 = 80 = 5 
                // 0 = 75 = 0
                // 45 = 70 = -5
                // 90 = 65 = -10В
                float translateXOffset = (float) Math.Abs(currentData.Grade.Value / 9.0) * -1;

                textPaint.TextSize = 35;
                canvas.Translate(new SKPoint(75 + translateXOffset, 180));
                canvas.RotateDegrees((float)-currentData.Grade.Value);
                canvas.DrawText("—————→", SKPoint.Empty, textPaint);
                canvas.Restore();
            }

            using SKImage imageExport = surface.Snapshot();
            SKData data = imageExport.Encode(SKEncodedImageFormat.Png, 100);
            return Task.FromResult(data);
        }
    }
}
