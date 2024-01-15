using SkiaSharp;

using TelemetryExporter.Console.Attributes;
using TelemetryExporter.Console.Models;
using TelemetryExporter.Console.Widgets.Interfaces;

namespace TelemetryExporter.Console.Widgets.Speed
{
    [WidgetData(Index = 1)]
    internal class SpeedWidget : IWidget
    {
        public async Task GenerateImageAsync(SessionData sessionData, FrameData currentData)
        {
            string folderName = Path.Combine("Telemetry", "Speed");
            if (!Directory.Exists(folderName))
            {
                Directory.CreateDirectory(folderName);
            }

            using SKBitmap radial = SKBitmap.FromImage(SKImage.FromEncodedData(Path.Combine("Images", "radial_6.png")));
            using SKBitmap dial = SKBitmap.FromImage(SKImage.FromEncodedData(Path.Combine("Images", "radial_6_dial.png")));

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
            using SKData data = imageExport.Encode(SKEncodedImageFormat.Png, 100);

            using FileStream stream = System.IO.File.OpenWrite(Path.Combine(folderName, currentData.FileName));
            using Stream s = data.AsStream();
            await s.CopyToAsync(stream);
            await stream.FlushAsync();
        }
    }
}
