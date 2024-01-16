using System.Collections.ObjectModel;

using Dynastream.Fit;

using SkiaSharp;

using TelemetryExporter.Console.Attributes;
using TelemetryExporter.Console.Models;
using TelemetryExporter.Console.Utilities;
using TelemetryExporter.Console.Widgets.Interfaces;

namespace TelemetryExporter.Console.Widgets.Elevation
{
    [WidgetData(Index = 5)]
    internal class ElevationWidget : IWidget
    {
        const int ElevationPictureWidthPixels = 700;
        const int ElevationPictureHeightPixels = 250;

        private readonly SKPath elevationPath;
        private readonly LineChartData lineChartData;

        public ElevationWidget(ReadOnlyCollection<RecordMesg> dataMessages)
        {
            lineChartData = new(dataMessages, ElevationPictureWidthPixels, ElevationPictureHeightPixels, offsetPercentageY: .20f);
            elevationPath = lineChartData.LinePath;
        }

        public async Task GenerateImageAsync(SessionData sessionData, FrameData currentData)
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

            string folderName = Path.Combine("Telemetry", "Elevation");
            if (!Directory.Exists(folderName))
            {
                Directory.CreateDirectory(folderName);
            }

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
                canvas.DrawText($"{currentData.Altitude:F1} m", elevationPoint.X + xOffset, elevationPoint.Y - 10, elvationPaint);
            }

            using SKImage image = surface.Snapshot();
            using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);

            using FileStream stream = System.IO.File.OpenWrite(Path.Combine(folderName, currentData.FileName));
            using Stream s = data.AsStream();
            await s.CopyToAsync(stream);
            await stream.FlushAsync();
        }
    }
}
