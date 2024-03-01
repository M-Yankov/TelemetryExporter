namespace TelemetryExporter.UI.CustomControls;

public class WidgetCheckBox : ContentView
{
    private readonly CheckBox internalCheckBox;

    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(int), typeof(VisualElement), 0);
    
    public WidgetCheckBox()
    {
        internalCheckBox = new CheckBox();
        internalCheckBox.CheckedChanged += InternalCheckBox_CheckedChanged;
        Content = new VerticalStackLayout
        {
            Children = { internalCheckBox }
        };
    }

    public event EventHandler<WidgetCheckedChangedEventArgs>? CheckedChanged;

    public int Value
    {
        get => (int)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public bool IsChecked { get => internalCheckBox.IsChecked; }

    private void InternalCheckBox_CheckedChanged(object? sender, CheckedChangedEventArgs e) => 
        CheckedChanged?.Invoke(sender, new WidgetCheckedChangedEventArgs(e.Value, this.Value));
}
