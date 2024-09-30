using SkiaSharp;
using TelemetryExporter.Core.Models;

namespace TelemetryExporter.Core.Widgets
{
    public abstract class TextBaseWidget
    {
        public abstract int WidgetWidth { get; }

        public abstract int WidgetHeight { get; }

        public abstract string GetTextValue(SessionData sessionData, FrameData currentData);

        public async Task<SKData> GetImageData(SessionData sessionData, FrameData currentData)
        {
            SKImageInfo info = new(WidgetWidth, WidgetHeight, SKImageInfo.PlatformColorType, SKAlphaType.Unpremul);

            using SKPaint textPaint = new()
            {
                Color = SKColors.White,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Consolas"),
                TextSize = 35,
            };

            using SKPaint transparentPaint = new()
            {
                Color = new SKColor(0, 0, 0, 100),
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            using SKSurface surface = SKSurface.Create(info);
            SKCanvas canvas = surface.Canvas;
            canvas.DrawPaint(transparentPaint);

            string text = this.GetTextValue(sessionData, currentData);
            canvas.DrawText(text, new SKPoint(10, textPaint.TextSize), textPaint);

            using SKImage image = surface.Snapshot();
            SKData data = image.Encode(SKEncodedImageFormat.Png, 100);

            return data;
        }
    }
}
