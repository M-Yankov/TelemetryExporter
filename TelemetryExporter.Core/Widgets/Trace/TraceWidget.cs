using System.Collections.ObjectModel;

using Dynastream.Fit;

using SkiaSharp;

using TelemetryExporter.Core.Attributes;
using TelemetryExporter.Core.Extensions;
using TelemetryExporter.Core.Models;
using TelemetryExporter.Core.Utilities;
using TelemetryExporter.Core.Widgets.Interfaces;

namespace TelemetryExporter.Core.Widgets.Trace
{
    [WidgetData(Index = 3)]
    internal class TraceWidget : IWidget
    {
        private const int GpxPictureWidthPixels = 1000;
        private const float GpxPictureOffsetPercentage = .05f;

        private readonly TraceChartData traceChart;
        private readonly SKPath tracePath;

        public TraceWidget(ReadOnlyCollection<RecordMesg> dataMessages)
        {
            // It's square
            traceChart = new TraceChartData(dataMessages, GpxPictureWidthPixels, GpxPictureWidthPixels, GpxPictureOffsetPercentage);
            tracePath = traceChart.TracePath;
        }

        public async Task GenerateImageAsync(SessionData sessionData, FrameData currentData)
        {
            SKImageInfo info = new(GpxPictureWidthPixels, GpxPictureWidthPixels, SKImageInfo.PlatformColorType, SKAlphaType.Unpremul);
            using SKPaint blackPaint = new()
            {
                Color = SKColors.Black,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };

            using SKPaint redPaint = new()
            {
                Color = SKColors.Red,
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                StrokeWidth = 2
            };

            string folderName = Path.Combine("Telemetry", "Trace");
            if (!Directory.Exists(folderName))
            {
                Directory.CreateDirectory(folderName);
            }

            using SKSurface surface = SKSurface.Create(info);

            SKCanvas canvas = surface.Canvas;
            canvas.DrawPath(tracePath, blackPaint);

            SKPoint gpxPoint = SKPoint.Empty;
            if (currentData.Longitude.HasValue && currentData.Latitude.HasValue)
            {
                gpxPoint = traceChart.CalculateImageCoordinates(currentData.Longitude.Value, currentData.Latitude.Value);
                gpxPoint.AddOffset(GpxPictureWidthPixels * GpxPictureOffsetPercentage);
            }

            if (!gpxPoint.IsEmpty)
            {
                const int CircleRadius = 5;
                canvas.DrawCircle(gpxPoint, CircleRadius, redPaint);
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
