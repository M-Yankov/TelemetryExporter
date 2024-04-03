using SkiaSharp;

using TelemetryExporter.Core.Attributes;
using TelemetryExporter.Core.Extensions;
using TelemetryExporter.Core.Models;
using TelemetryExporter.Core.Utilities;
using TelemetryExporter.Core.Widgets.Interfaces;

namespace TelemetryExporter.Core.Widgets.Speed
{
    [WidgetData(Index = WidgetIndex, ExampleImagePath = ImagePath, Category = TECoreContsants.Categories.Speed)]
    public class SpeedWidget : IWidget
    {
        private const int WidgetIndex = 1;
        private const string ImagePath = "Images/ExampleSpeed.png";

        public int Index => WidgetIndex;

        public string Category => TECoreContsants.Categories.Speed;

        public string Name => "SpeedWidget";

        public string ExampleImagePath => ImagePath;

        public Task<SKData> GenerateImage(SessionData sessionData, FrameData currentData)
        {
            using SKBitmap radial = SKBitmap.FromImage(
                SKImage.FromEncodedData(PathExtensions.Combine("Images", "radial_6.png")));
            using SKBitmap dial = SKBitmap.FromImage(
                SKImage.FromEncodedData(PathExtensions.Combine("Images", "radial_6_dial.png")));

            SKImageInfo info = new(radial.Width, radial.Height, SKImageInfo.PlatformColorType, SKAlphaType.Unpremul);
            using SKSurface surface = SKSurface.Create(info);

            using SKCanvas canvas = surface.Canvas;

            using SKPaint blendPaint = new()
            {
                ColorFilter = SKColorFilter.CreateBlendMode(new SKColor(200, 0, 0, 255), SKBlendMode.SrcIn)
            };

            canvas.DrawBitmap(radial, 0, 0, blendPaint);

            using SKPaint textPaint = new()
            {
                Color = SKColors.White,
                TextSize = 16,
                TextAlign = SKTextAlign.Center,
                Typeface = SKTypeface.FromFamilyName("Consolas"),
                IsAntialias = true,
            };

            // -130; 130 = 260
            double percentage = currentData.Speed / sessionData.MaxSpeed;
            percentage = double.IsNaN(percentage) ? 0 : percentage;
            double percentageDial = 260 * percentage;
            double dialDegrees = -130 + percentageDial;

            using SKPaint radialTrailPaint = new()
            {
                Color = new SKColor(255, 255, 255, 100),
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 20
            };

            canvas.DrawArc(SKRect.Create(57, 57, 186, 186), 140, (float)percentageDial, false, radialTrailPaint);

            // could use the scale from garmin
            SKPoint textZeroValueCoords = new(70, 250);
            SKPoint textMaxValueCoords = new(230, 250);
            SKPoint textAverageValueCoords = new(radial.Width / 2, 28);
            SKPoint textCurrentValueCoords = new(radial.Width / 2, 250);

            SKPoint textUnitValueCoords = new(radial.Width / 2, 250 + 45);

            canvas.DrawText("0", textZeroValueCoords, textPaint);
            canvas.DrawText($"{sessionData.MaxSpeed:0}", textMaxValueCoords, textPaint);
            canvas.DrawText($"{(sessionData.MaxSpeed / 2):0}", textAverageValueCoords, textPaint);

            textPaint.TextSize = 48;
            canvas.DrawText($"{currentData.Speed:0}", textCurrentValueCoords, textPaint);
            canvas.DrawText("KM/H", textUnitValueCoords, textPaint);

            canvas.SaveLayer();

            // Move anchor point to the center
            SKPoint centerPoint = new(radial.Width / 2f, radial.Height / 2f);
            canvas.Translate(centerPoint);

            canvas.RotateDegrees((float)dialDegrees);

            canvas.Translate(-centerPoint.X, -centerPoint.Y);

            canvas.DrawBitmap(dial, 0, 0);

            canvas.Restore();

            using SKImage imageExport = surface.Snapshot();
            SKData data = imageExport.Encode(SKEncodedImageFormat.Png, 100);
            return Task.FromResult(data);
        }
    }
}
