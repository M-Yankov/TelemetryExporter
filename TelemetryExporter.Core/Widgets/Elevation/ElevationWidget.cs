using Dynastream.Fit;

using SkiaSharp;

using TelemetryExporter.Core.Attributes;
using TelemetryExporter.Core.Models;
using TelemetryExporter.Core.Utilities;
using TelemetryExporter.Core.Widgets.Interfaces;

namespace TelemetryExporter.Core.Widgets.Elevation
{
    [WidgetData(Index = WidgetIndex, ExampleImagePath = ImagePath, Category = TECoreContsants.Categories.Elevation)]
    public class ElevationWidget : IWidget
    {
        private const int ElevationPictureWidthPixels = 700;
        private const int ElevationPictureHeightPixels = 250;
        private const int WidgetIndex = 5;
        private const string ImagePath = "Images/ExampleElevation.png";

        private readonly SKPath elevationPath;
        private readonly LineChartData lineChartData;

        public int Index => WidgetIndex;

        public string Category => TECoreContsants.Categories.Elevation;

        public string Name => "ElevationWidget";

        public string ExampleImagePath => ImagePath;

        public ElevationWidget(IReadOnlyCollection<RecordMesg> dataMessages)
        {
            lineChartData = new(dataMessages, ElevationPictureWidthPixels, ElevationPictureHeightPixels, offsetPercentageY: .20f);
            elevationPath = lineChartData.LinePath;
        }

        public Task<SKData> GenerateImage(SessionData sessionData, FrameData currentData)
        {
            SKImageInfo info = new(ElevationPictureWidthPixels, ElevationPictureHeightPixels, SKImageInfo.PlatformColorType, SKAlphaType.Unpremul);
            using SKPaint blackPaint = new()
            {
                Color = SKColors.Black,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };

            using SKPaint trasparentBlack = new()
            {
                Color = new SKColor(0, 0, 0, 170),
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
            };

            using SKPaint textPaint = new()
            {
                Color = SKColors.White,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Consolas"),
                TextSize = 35,
            };

            using SKPaint redPaint = new()
            {
                Color = SKColors.Red,
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                StrokeWidth = 2
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

            SKPath fillPath = new(elevationPath);
            fillPath.LineTo(ElevationPictureWidthPixels, ElevationPictureHeightPixels);
            fillPath.LineTo(0, ElevationPictureHeightPixels);

            canvas.DrawPath(fillPath, trasparentBlack);
            canvas.DrawPath(elevationPath, blackPaint);

            canvas.DrawText("ELEVATION", new SKPoint(25, 35), textPaint);
            using SKPaint linePaint = textPaint.Clone();
            linePaint.StrokeWidth = 10;

            canvas.DrawLine(0, 0, ElevationPictureWidthPixels, 0, linePaint);

            SKPoint elevationPoint = SKPoint.Empty;
            if (currentData.Altitude.HasValue)
            {
                elevationPoint = lineChartData.CalculateImageCoordinates((float)currentData.Altitude.Value, currentData.IndexOfCurrentRecord, sessionData.CountOfRecords);
            }

            if (!elevationPoint.IsEmpty)
            {
                canvas.DrawCircle(elevationPoint, radius: 5, redPaint);

                float reachedToEndPercentage = elevationPoint.X / ElevationPictureWidthPixels;

                int xOffset = 10;

                using SKPaint elvationPaint = textPaint.Clone();
                if (reachedToEndPercentage > .80f)
                {
                    xOffset *= -1;
                    elvationPaint.TextAlign = SKTextAlign.Right;
                }

                int yOffset = -10;
                bool isTextOverlapped = reachedToEndPercentage < .30f
                    && (elevationPoint.Y / ElevationPictureHeightPixels) < .28f;
                if (isTextOverlapped)
                {
                    // The "Elevation" text gets overlapped in some situations
                    yOffset += 40;
                }

                canvas.DrawText($"{currentData.Altitude:F1} m", elevationPoint.X + xOffset, elevationPoint.Y + yOffset, elvationPaint);
            }

            using SKImage image = surface.Snapshot();
            SKData data = image.Encode(SKEncodedImageFormat.Png, 100);

            return Task.FromResult(data);
        }
    }
}
