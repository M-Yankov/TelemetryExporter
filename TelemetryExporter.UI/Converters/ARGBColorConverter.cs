using System.Globalization;

using Microsoft.Maui.Controls;

namespace TelemetryExporter.UI.Converters
{
    internal class ARGBColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is Color color ? color.ToArgbHex(true) : value;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string input && input.Length == 9 &&
                Color.TryParse(input, out Color parsedColor))
            {
                return parsedColor;
            }

            // this still logs some errors in the logs.
            return Binding.DoNothing;
        }
    }
}
