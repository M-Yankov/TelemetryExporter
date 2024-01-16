using System.Globalization;

using SkiaSharp;

using TelemetryExporter.Console.Attributes;
using TelemetryExporter.Console.Models;
using TelemetryExporter.Console.Widgets.Interfaces;

namespace TelemetryExporter.Console.Widgets.Pace
{
    [WidgetData(Index = 4)]
    internal class PaceWidget : IWidget
    {
        public async Task GenerateImageAsync(SessionData sessionData, FrameData frameData)
        {
            const int PaceImageWidth = 400;
            const int PaceImageHeight = 100;

            float percentageOffsetWidth = PaceImageWidth * .05f;

            string folderName = Path.Combine("Telemetry", "Pace");
            if (!Directory.Exists(folderName))
            {
                Directory.CreateDirectory(folderName);
            }

            SKImageInfo info = new(PaceImageWidth, PaceImageHeight, SKImageInfo.PlatformColorType, SKAlphaType.Unpremul);
            using SKPaint blackPaint = new()
            {
                Color = SKColors.Black,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };

            using SKSurface surface = SKSurface.Create(info);

            SKCanvas canvas = surface.Canvas;

            //  canvas.DrawPaint(blackPaint);
            SKPath pathRegion = new();
            SKPoint topLeftDrawArea = new(percentageOffsetWidth, 0);
            SKPoint topRightDrawArea = new(PaceImageWidth, 0);
            SKPoint bottomLeftDrawArea = new(0, PaceImageHeight);
            SKPoint bottomRightDrawArea = new(PaceImageWidth - percentageOffsetWidth, PaceImageHeight);

            pathRegion.MoveTo(topLeftDrawArea);
            pathRegion.LineTo(topRightDrawArea);
            pathRegion.LineTo(bottomRightDrawArea);
            pathRegion.LineTo(bottomLeftDrawArea);
            pathRegion.LineTo(topLeftDrawArea);

            SKRegion region = new(pathRegion);

            using SKPaint trasparentBlack = new()
            {
                Color = new SKColor(0, 0, 0, 100),
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                // StrokeWidth = 2
            };

            canvas.DrawRegion(region, trasparentBlack);

            const int FontSize = 60;
            using SKPaint textColor = new()
            {
                Color = SKColors.White,
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                TextAlign = SKTextAlign.Right,
                Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold),
                TextSize = FontSize,
            };

            const float MagicNumberAlignFontY = FontSize * 0.25f;
            float textPointY = (PaceImageHeight / 2) + MagicNumberAlignFontY;
            const string ColonSymbol = ":";

            NumberFormatInfo nfi = new()
            {
                NumberDecimalSeparator = ColonSymbol
            };

            string text;

            // it is to take 0.86214 from 7.86214
            double paceHundreds = frameData.Pace - (int)frameData.Pace;
            double paceSeconds = (paceHundreds * 60);

            if (frameData.Pace > 0)
            {
                text = $"{(int)frameData.Pace}:{(int)paceSeconds:D2}".PadLeft(5, ' ');
            }
            else
            {
                text = "--";
            }

            text += "/KM";

            SKPoint textCoordinates = new(bottomRightDrawArea.X - 10, textPointY);

            using SKPaint linePaint = blackPaint.Clone();
            linePaint.StrokeWidth = 10;

            canvas.DrawText(text, textCoordinates, textColor);
            canvas.DrawLine(bottomLeftDrawArea, bottomRightDrawArea, linePaint);

            using SKImage image = surface.Snapshot();
            using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);

            using FileStream stream = System.IO.File.OpenWrite(Path.Combine(folderName, frameData.FileName));
            using Stream s = data.AsStream();
            await s.CopyToAsync(stream);
            await stream.FlushAsync();
        }
    }
}
