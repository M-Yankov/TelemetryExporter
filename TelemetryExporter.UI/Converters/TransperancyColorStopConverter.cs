using System.Globalization;

namespace TelemetryExporter.UI.Converters
{
    internal class TransperancyColorStopConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is Color color ? color.WithAlpha(1) : value;
        

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is Color color ? color.WithAlpha(1) : value;
    }
}
