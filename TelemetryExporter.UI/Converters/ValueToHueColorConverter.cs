using System.Globalization;

namespace TelemetryExporter.UI.Converters
{
    internal class ValueToHueColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => Convert(value, targetType);

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => Convert(value, targetType);

        /// <summary>
        /// Need to take in mind the min max Ranges
        /// </summary>
        private static object? Convert(object? value, Type targetType)
        {
            return targetType switch
            {
                Type t when t == typeof(Color) && value != null && value is double num => Color.FromHsv((int)num, 100, 100),
                Type t when t == typeof(double) && value is Color c && value != null => c.GetHue(),
                _ => null,
            };
        }
    }
}
