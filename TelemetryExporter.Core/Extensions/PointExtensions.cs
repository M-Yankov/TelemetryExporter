﻿using System.Drawing;

using SkiaSharp;

namespace TelemetryExporter.Core.Extensions
{
    internal static class PointExtensions
    {
        public static decimal Longitude(this Point point) => ConvertFromSemiCircles(point.X);

        public static decimal Latitude(this Point point) => ConvertFromSemiCircles(point.Y);

        public static void AddOffset(this ref SKPoint skPoint, float offset) => skPoint.Offset(offset, offset);

        private static decimal ConvertFromSemiCircles(int value)
        {
            decimal result = (long)int.MaxValue + 1;
            decimal res = 180M / result;

            return value * res;
        }
    }
}
