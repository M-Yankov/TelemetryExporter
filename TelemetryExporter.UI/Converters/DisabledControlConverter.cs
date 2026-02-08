using System.Globalization;

namespace TelemetryExporter.UI.Converters
{
    public class DisabledControlConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool isChecked ? !isChecked : null;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool isChecked ? !isChecked : null;
    }
}
