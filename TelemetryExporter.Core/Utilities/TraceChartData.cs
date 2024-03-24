using System.Collections.ObjectModel;

using Dynastream.Fit;

using SkiaSharp;

using TelemetryExporter.Core.Extensions;

namespace TelemetryExporter.Core.Utilities
{
    internal class TraceChartData
    {
        private readonly SKPath tracePath;

        public TraceChartData(IReadOnlyCollection<RecordMesg> dataMessages, int widthPixels, int heightPixels, float offsetPercentage)
        {
            PictureHeightPixels = heightPixels;
            PictureWidthPixels = widthPixels;
            OffsetPixelsY = heightPixels * offsetPercentage;
            OffsetPixelsX = widthPixels * offsetPercentage;

            tracePath = BuildPath(dataMessages);
        }

        public SKPath TracePath { get => tracePath; }

        public int PictureHeightPixels { get; set; }

        public int PictureWidthPixels { get; set; }

        public float OffsetPixelsY { get; set; }

        public float OffsetPixelsX { get; set; }

        private SKPoint FarTopPoint { get; set; } = new(0, float.MinValue);

        private SKPoint FarBottomPoint { get; set; } = new(0, float.MaxValue);

        private SKPoint FarLeftPoint { get; set; } = new(float.MaxValue, 0);

        private SKPoint FarRightPoint { get; set; } = new(float.MinValue, 0);

        public SKPoint CalculateImageCoordinates(float longitude, float latitude)
        {
            // TODO: Caching


            // Calculate only X, because it's rectangular
            // *2 because the offset is from one edge and from the other edge
            float drawAreaWidthX = PictureWidthPixels - (OffsetPixelsX * 2);

            float resX = Math.Abs(FarLeftPoint.X - FarRightPoint.X);
            float resY = Math.Abs(FarTopPoint.Y - FarBottomPoint.Y);

            float percentageOfX = resX / drawAreaWidthX;
            float percentageOfY = resY / drawAreaWidthX;

            float y = Math.Abs(latitude - FarTopPoint.Y) / percentageOfY;
            float x = Math.Abs(longitude - FarLeftPoint.X) / percentageOfX;

            return new(x, y);
        }

        private SKPath BuildPath(IReadOnlyCollection<RecordMesg> dataMessages)
        {
            List<SKPoint> points = new (); 
            foreach (RecordMesg? gpsMessage in dataMessages.OrderBy(x => x.GetTimestamp().GetDateTime()))
            {
                int? lattitude = gpsMessage.GetPositionLat(); // y  y=0 equator      south ↓ negative  | positive ↑ north  max ±90
                int? longitute = gpsMessage.GetPositionLong(); // x  x=0 Prime Meridian (London)  west <- negative  |  positive -> east ±180

                if (lattitude.HasValue && longitute.HasValue)
                {
                    SKPoint currentPoint = new(longitute.Value, lattitude.Value);
                    points.Add(currentPoint);

                    if (lattitude.Value > FarTopPoint.Y)
                    {
                        FarTopPoint = currentPoint;
                    }

                    if (lattitude.Value < FarBottomPoint.Y)
                    {
                        FarBottomPoint = currentPoint;
                    }

                    if (longitute.Value < FarLeftPoint.X)
                    {
                        FarLeftPoint = currentPoint;
                    }

                    if (longitute.Value > FarRightPoint.X)
                    {
                        FarRightPoint = currentPoint;
                    }
                }
            }

            SKPath path = new();
            if (points.Count == 0)
            {
                return path;
            }

            for (int i = 0; i < points.Count; i++)
            {
                float latitude = points[i].Y;
                float longitude = points[i].X;

                SKPoint p0 = CalculateImageCoordinates(longitude, latitude);
                p0.Offset(OffsetPixelsX, OffsetPixelsY);

                if (i == 0)
                {
                    path.MoveTo(p0);
                }
                else
                {
                    path.LineTo(p0);
                }
            }

            return path;
        }
    }
}
