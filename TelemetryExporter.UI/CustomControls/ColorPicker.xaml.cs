using System.ComponentModel;

using CommunityToolkit.Maui.Core.Extensions;

namespace TelemetryExporter.UI.CustomControls;

public partial class ColorPicker : ContentView
{
    private double maxPickerX = 0;
    private double accumulatedPickerSaturation = 0;
    private double accumulatedHSVValue = 0;

    private bool adjustColorPickerPosition;

    private const double HsvLayersHeight = 130;
    private const int MarginBuffer = 2;

    private Color? selectedColor;//= Colors.Red;

    /// <summary>
    /// null means no color selected, reset.
    /// </summary>
    public event EventHandler<Color?>? OnColorChanged;

    public readonly BindableProperty SelectedColorProperty =
        BindableProperty.Create(
            nameof(SelectedColor),
            typeof(Color),
            typeof(ColorPicker),
            null, // Colors.Red
            BindingMode.TwoWay);


    //public Color? SelectedColor 
    //{ 
    //    get => selectedColor; 
    //    set
    //    {
    //        selectedColor = value;
    //        OnColorChanged?.Invoke(this, selectedColor);
    //        OnPropertyChanged(nameof(SelectedColor));
    //    }
    //}

    public Color? SelectedColor
    {
        get => (Color?)GetValue(SelectedColorProperty);
        set
        {
            if (selectedColor == null && value != null)
            {
                // InitializeColorPickerXYPostion(value);
                adjustColorPickerPosition = true;
            }

            SetValue(SelectedColorProperty, value);
            selectedColor = value;
            OnColorChanged?.Invoke(this, selectedColor);
        }
    }

    public ColorPicker()
    {
        // Test with each side of the colorpicker at the edge, or at corners.
        // Where to set the default color
        InitializeComponent();

        this.secondRowDefinition.Height = new GridLength(HsvLayersHeight);

        PanGestureRecognizer dragRegognizer = new();
        dragRegognizer.PanUpdated += DragRegognizer_PanUpdated; ;
        this.colorPickerEllipse.GestureRecognizers.Add(dragRegognizer);

        const int MaxHueDegree = 360;
        for (int i = 0; i <= MaxHueDegree; i += 10)
        {
            GradientStop gradientStop = new();
            gradientStop.Offset = i / (float)MaxHueDegree;
            gradientStop.Color = Color.FromHsv(i, 100, 100);
            sliderBackgroundBrush.GradientStops.Add(gradientStop);
        }
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        // width is -1 before components are not initialized
        if (width > 0)
        {
            maxPickerX = width;

            if (this.adjustColorPickerPosition)
            {
                InitializeColorPickerXYPostion(SelectedColor);
            }
        }
    }

    private void Slider_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        if (sender is Slider slider)
        {
            Color newHue = Color.FromHsv((int)e.NewValue, 100, 100);
            slider.ThumbColor = hueLayerStop.Color = newHue;

            // this event is also triggered when setting the slider value from code when "SelectedColor" is still null
            if (this.SelectedColor != null)
            {
                this.SelectedColor = this.SelectedColor.WithHue(newHue.GetHue());
            }
        }
    }

    private void DragRegognizer_PanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Running:
                colorPickerEllipse.TranslationX = Math.Clamp(accumulatedPickerSaturation + e.TotalX, -Width + MarginBuffer, 0);
                colorPickerEllipse.TranslationY = Math.Clamp(accumulatedHSVValue + e.TotalY, 0, HsvLayersHeight - MarginBuffer);
                break;
            case GestureStatus.Completed:
                accumulatedPickerSaturation = colorPickerEllipse.TranslationX;
                accumulatedHSVValue = colorPickerEllipse.TranslationY;

                // Color c = Color.FromHsv((int)this.hueSlider.Value, 100, 100);
                var h = hueLayerStop.Color.GetHue();

                float s = (float)(accumulatedPickerSaturation / (-Width + MarginBuffer));
                float v = (float)(accumulatedHSVValue / (HsvLayersHeight - MarginBuffer));

                this.SelectedColor = Color.FromHsv(h, 1 - s, 1 - v);
                ;

                break;
            default:
                break;
        }
    }

    private void InitializeColorPickerXYPostion(Color newValue)
    {
        // hue is between 0 and 1, slider is between 0 and 360
        // this also triggers the Slider_ValueChanged event, which sets the hueLayerStop.Color and the slider.ThumbColor
        hueSlider.Value = 360 * newValue.GetHue();

        double saturationX = -(Width - (newValue.GetSaturation() * Width));
        double hsvValueY = newValue.GetPercentBlackKey() * (HsvLayersHeight - MarginBuffer);

        DragRegognizer_PanUpdated(null, new PanUpdatedEventArgs(GestureStatus.Running, 1, saturationX, hsvValueY));
        DragRegognizer_PanUpdated(null, new PanUpdatedEventArgs(GestureStatus.Completed, 1));
    }
}