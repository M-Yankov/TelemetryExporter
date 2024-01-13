using System.Collections.ObjectModel;

using Dynastream.Fit;

using SkiaSharp;

namespace TelemetryExporter.Console.Utilities
{
    /// <summary>
    /// X axis is time, Y axis is the value at that moment
    /// </summary>
    internal class LineChartData
    {
        private readonly SKPath lineChart;

        public LineChartData(ReadOnlyCollection<RecordMesg> dataMessages, int widthPixels, int heightPixels, float offsetPercentageY)
        {
            lineChart = BuildPath(dataMessages);
            PictureHeightPixels = heightPixels;
            PictureWidthPixels = widthPixels;
            OffsetPixelsY = heightPixels * offsetPercentageY;
        }

        public SKPath LinePath => lineChart;

        public float MinY { get; set; }

        public float MaxY { get; set; }

        public float TotalValue { get => Math.Abs(MaxY - MinY); }

        #region PicruteData
        public int PictureWidthPixels { get; set; }

        public int PictureHeightPixels { get; set; }

        public float OffsetPixelsY { get; set; }

        /// <summary>
        /// Calculates the point according to value and given time at index.
        /// </summary>
        /// <param name="currentValueY">It's the value of Y axis between MinY and MaxY..</param>
        /// <param name="currentIndex">The index of X axis according to <paramref name="totalValuesX"/>.</param>
        /// <param name="totalValuesX">Count of total values of X axis.</param>
        /// <returns></returns>
        public SKPoint CalculateImageCoordinates(float currentValueY, int currentIndex, float totalValuesX)
        {
            // TODO: Use caching


            float calculatedAltitude = currentValueY - MinY;
            float calculatedPercentage = calculatedAltitude / TotalValue;

            float y = PictureHeightPixels - ((PictureHeightPixels - OffsetPixelsY) * calculatedPercentage);

            float xPerentage = currentIndex / totalValuesX;
            float x = PictureWidthPixels * xPerentage;

            return new(x, y);
        }
        #endregion

        private SKPath BuildPath(ReadOnlyCollection<RecordMesg> dataMessages)
        {
            List<float> altitudeValues = new();

            foreach (RecordMesg recordMesg in dataMessages.OrderBy(x => x.GetTimestamp().GetDateTime()))
            {
                float? altitude = recordMesg?.GetEnhancedAltitude();

                if (!altitude.HasValue)
                {
                    continue;
                }

                if (altitude.Value < MinY)
                {
                    MinY = altitude.Value;
                }

                if (altitude.Value > MaxY)
                {
                    MaxY = altitude.Value;
                }

                altitudeValues.Add(altitude.Value);
            }

            SKPath skPath = new();

            for (int i = 0; i < altitudeValues.Count; i++)
            {
                SKPoint point = CalculateImageCoordinates(altitudeValues[i], i + 1, altitudeValues.Count);

                if (i == 0)
                {
                    skPath.MoveTo(point);
                }
                else
                {
                    skPath.LineTo(point);
                }
            }

            return skPath;
        }

    }
}
