using Dynastream.Fit;

using SkiaSharp;

using TelemetryExporter.Core.Attributes;
using TelemetryExporter.Core.Extensions;
using TelemetryExporter.Core.Models;
using TelemetryExporter.Core.Utilities;
using TelemetryExporter.Core.Widgets.Interfaces;

namespace TelemetryExporter.Core.Widgets.Trace
{
    [WidgetData(Index = WidgetIndex, ExampleImagePath = ImagePath, Category = TECoreContsants.Categories.Trace)]
    public class TraceWidget : IWidget, INeedInitialization
    {
        private const int GpxPictureWidthPixels = 1000;
        private const float GpxPictureOffsetPercentage = .05f;
        private const int WidgetIndex = 3;
        private const string ImagePath = "Images/ExampleTrace.png";

        private TraceChartData traceChart = new();
        private SKPath tracePath = new();
        private bool isInitialized = false;

        public static int Index => WidgetIndex;

        public string Category => TECoreContsants.Categories.Trace;

        public string Name => "TraceWidget";

        public void Initialize(IReadOnlyCollection<RecordMesg> dataMessages)
        {
            // It's square
            traceChart = new TraceChartData(dataMessages, GpxPictureWidthPixels, GpxPictureWidthPixels, GpxPictureOffsetPercentage);
            tracePath = traceChart.TracePath;
            isInitialized = true;
        }

        public Task<SKData> GenerateImage(SessionData sessionData, FrameData currentData)
        {
            if (!isInitialized)
            {
                throw new InvalidOperationException($"Cannot use widget: {nameof(TraceWidget)}, before initialization!");
            }

            SKImageInfo info = new(GpxPictureWidthPixels, GpxPictureWidthPixels, SKImageInfo.PlatformColorType, SKAlphaType.Unpremul);
            using SKPaint whitePaint = new()
            {
                Color = SKColors.White,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 3
            };

            using SKPaint redPaint = new()
            {
                Color = SKColors.Red,
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                StrokeWidth = 2
            };

            using SKSurface surface = SKSurface.Create(info);

            SKCanvas canvas = surface.Canvas;
            canvas.DrawPath(tracePath, whitePaint);

            SKPoint gpxPoint = SKPoint.Empty;
            if (currentData.Longitude.HasValue && currentData.Latitude.HasValue)
            {
                gpxPoint = traceChart.CalculateImageCoordinates(currentData.Longitude.Value, currentData.Latitude.Value);
                gpxPoint.AddOffset(GpxPictureWidthPixels * GpxPictureOffsetPercentage);
            }

            if (!gpxPoint.IsEmpty)
            {
                const int CircleRadius = 8;
                canvas.DrawCircle(gpxPoint, CircleRadius, redPaint);
            }

            using SKImage image = surface.Snapshot();
            SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
            return Task.FromResult(data);
        }
    }
}
