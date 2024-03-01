using System.Globalization;

using CommunityToolkit.Maui.Views;

using TelemetryExporter.UI.CustomControls;

namespace TelemetryExporter.UI.Converters
{
    public class ExpanderParametersConverter : IMultiValueConverter
    {
        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values != null && values.Length > 1 &&
                values[0] is Expander expander && values[1] is Label label)
            {
                return new ExpanderWidgetArgs(expander, label);
            }

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
