using System.Globalization;

using SkiaSharp;

using TelemetryExporter.Core.Attributes;
using TelemetryExporter.Core.Models;
using TelemetryExporter.Core.Utilities;
using TelemetryExporter.Core.Widgets.Interfaces;

namespace TelemetryExporter.Core.Widgets.Pace
{
    [WidgetData(Index = WidgetIndex, ExampleImagePath = ImagePath, Category = TECoreContsants.Categories.Speed)]
    public class PaceWidget : IWidget
    {
        // below that speed it's assumed as walking (For running only)
        // https://www.convert-me.com/en/convert/speed/?u=minperkm_1&v=30
        private const double SpeedCutoff = 0.5556;
        private const int WidgetIndex = 4;
        private const string ImagePath = "Images/ExamplePace.png";

        public string Category => TECoreContsants.Categories.Speed;

        public int Index => WidgetIndex;

        public string Name => "PaceWidget";

        public Task<SKData> GenerateImage(SessionData sessionData, FrameData frameData)
        {
            double currentSpeed = frameData.Speed < SpeedCutoff ? 0 : frameData.Speed;
            double pace = currentSpeed > 0 ? (60 / currentSpeed)  : default;

            const int PaceImageWidth = 400;
            const int PaceImageHeight = 100;

            float percentageOffsetWidth = PaceImageWidth * .05f;

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
            double paceHundreds = pace - (int)pace;
            double paceSeconds = (paceHundreds * 60);

            if (pace > 0)
            {
                text = $"{(int)pace}:{(int)paceSeconds:D2}".PadLeft(5, ' ');
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
            SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
            return Task.FromResult(data);
        }
    }
}
