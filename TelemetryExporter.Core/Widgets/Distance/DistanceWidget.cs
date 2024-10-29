using SkiaSharp;

using TelemetryExporter.Core.Attributes;
using TelemetryExporter.Core.Models;
using TelemetryExporter.Core.Utilities;
using TelemetryExporter.Core.Widgets.Interfaces;

namespace TelemetryExporter.Core.Widgets.Distance
{
    [WidgetData(Index = WidgetIndex, ExampleImagePath = ImagePath, Category = TECoreContsants.Categories.Distance)]
    public class DistanceWidget : IWidget
    {
        private const int WidgetIndex = 2;
        private const string ImagePath = "Images/ExampleDistance.png";

        public static int Index => WidgetIndex;

        public string Category => TECoreContsants.Categories.Distance;

        public string Name => "DistanceWidget";

        public Task<SKData> GenerateImage(SessionData sessionData, FrameData currentData)
        {
            const int DistanceImageWidth = 700;
            const int DistanceImageHeight = 100;

            SKImageInfo info = new(DistanceImageWidth, DistanceImageHeight, SKImageInfo.PlatformColorType, SKAlphaType.Unpremul);
            using SKPaint transparentDistancePaint = new()
            {
                Color = new SKColor(0, 0, 0, 170),
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            using SKPaint trasparentBlack = new()
            {
                Color = new SKColor(0, 0, 0, 100),
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
            };

            using SKPaint textDistancePaint = new()
            {
                Color = SKColors.White,
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Typeface = SKTypeface.FromFamilyName("Consolas"),
                TextSize = 35,
            };

            using SKPaint textDistanceNumbersPaint = textDistancePaint.Clone();
            textDistanceNumbersPaint.TextSize += (textDistanceNumbersPaint.TextSize * .15f);

            using SKSurface surface = SKSurface.Create(info);

            SKCanvas canvas = surface.Canvas;
            canvas.DrawPaint(trasparentBlack);

            string distanceAsText;
            if (currentData.Distance.HasValue)
            {
                // https://www.calculatorsoup.com/calculators/math/percentage.php
                double currentDistancePercentage = currentData.Distance.Value / sessionData.TotalDistance;
                float imagePixelsDistanceX = DistanceImageWidth * (float)currentDistancePercentage;
                canvas.DrawRect(0, 0, imagePixelsDistanceX, DistanceImageHeight, transparentDistancePaint);
                distanceAsText = $"{currentData.Distance / 1000f:F3} KM";
            }
            else
            {
                distanceAsText = $"-- KM";
            }

            // the Points should be percentage, not hard-coded
            canvas.DrawText("DISTANCE", new SKPoint(25, 35), textDistancePaint);
            canvas.DrawText(distanceAsText, new SKPoint(25, 75), textDistanceNumbersPaint);

            using SKImage image = surface.Snapshot();
            SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
            return Task.FromResult(data);
        }
    }
}
