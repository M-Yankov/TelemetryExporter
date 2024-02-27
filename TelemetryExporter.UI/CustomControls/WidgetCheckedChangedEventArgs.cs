namespace TelemetryExporter.UI.CustomControls;

public class WidgetCheckedChangedEventArgs(bool value, int elementValue) : CheckedChangedEventArgs(value)
{
    public int ElementValue { get; init; } = elementValue;
}