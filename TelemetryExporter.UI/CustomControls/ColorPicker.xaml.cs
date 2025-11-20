using CommunityToolkit.Maui.Core.Extensions;

using TelemetryExporter.UI.Converters;
using TelemetryExporter.UI.Extensions;

namespace TelemetryExporter.UI.CustomControls;

public partial class ColorPicker : ContentView
{
    private double accumulatedPickerSaturation = 0;
    private double accumulatedHSVValue = 0;

    private bool adjustColorPickerPosition;

    private const double HsvLayersHeight = 130;
    private const int MarginBuffer = 2;

    private Color? selectedColor;

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

            if ((selectedColor != null && selectedColor.Equals(value))
                || selectedColor == null && value == null)
            {
                return;
            }

            selectedColor = value;
            OnColorChanged?.Invoke(this, selectedColor);
            InitializeColorPickerXYPostion(selectedColor!);
            
            // this should come last
            SetValue(SelectedColorProperty, value);
        }
    }

    public ColorPicker()
    {
        // Test with each side of the colorpicker at the edge, or at corners.
        // Where to set the default color
        InitializeComponent();

        this.secondRowDefinition.Height = new GridLength(HsvLayersHeight);

        PanGestureRecognizer dragRegognizer = new();
        dragRegognizer.PanUpdated += DragRegognizer_PanUpdated;
        this.colorPickerEllipse.GestureRecognizers.Add(dragRegognizer);

        const int MaxHueDegree = 360;
        for (int i = 0; i <= MaxHueDegree; i += 10)
        {
            GradientStop gradientStop = new();
            gradientStop.Offset = i / (float)MaxHueDegree;
            gradientStop.Color = Color.FromHsv(i, 100, 100);
            sliderBackgroundBrush.GradientStops.Add(gradientStop);
        }

        hueSlider.ValueChanged += Slider_ValueChanged;
        transperancySlider.ValueChanged += Transperancy_ValueChanged;

        transperancySlider.BindingContext = this;
        transperancySlider.SetBinding(Slider.ThumbColorProperty, nameof(SelectedColor));
        transparancyLayerColor.BindingContext = this;
        transparancyLayerColor.SetBinding(GradientStop.ColorProperty, nameof(SelectedColor), converter: new TransperancyColorStopConverter());

        hueSlider.BindingContext = hueSlider;
        hueSlider.SetBinding(Slider.ThumbColorProperty, nameof(hueSlider.Value), converter: new ValueToHueColorConverter());

        hueLayerStop.BindingContext = hueSlider;
        hueLayerStop.SetBinding(GradientStop.ColorProperty, nameof(hueSlider.ThumbColor));    
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        // width is -1 before components are not initialized
        if (width > 0)
        {
            if (this.adjustColorPickerPosition)
            {
                InitializeColorPickerXYPostion(SelectedColor);
            }
        }
    }

    // this event is also triggered when setting the slider value from code when "SelectedColor" is still null
    private void Slider_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        if (this.SelectedColor != null)
        {
            // using color instance to automatically calculate hue value, because the e.NewValue is between 0-360,
            // but WithHue expects float value between 0-1
            Color newHue = Color.FromHsv((int)e.NewValue, 100, 100);
            this.SelectedColor = this.SelectedColor.WithHue(newHue.GetHue());
        }
    }

    private void Transperancy_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        if (this.SelectedColor != null)
        {
            this.SelectedColor = this.SelectedColor.WithAlpha((float)e.NewValue);
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
                var h = hueLayerStop.Color?.GetHue() ?? 1;

                float s = (float)(accumulatedPickerSaturation / (-Width + MarginBuffer));
                float v = (float)(accumulatedHSVValue / (HsvLayersHeight - MarginBuffer));

                this.SelectedColor = Color.FromHsva(h, 1 - s, 1 - v, (float)transperancySlider.Value);
                break;
            default:
                break;
        }
    }

    private void InitializeColorPickerXYPostion(Color newValue)
    {
        if (Width <= 0)
        {
            return; 
        }

        // prevent change SelectedColor
        hueSlider.ValueChanged -= Slider_ValueChanged;
        transperancySlider.ValueChanged -= Transperancy_ValueChanged;

        // hue is between 0 and 1, slider is between 0 and 360
        // this also triggers the Slider_ValueChanged event, which sets the hueLayerStop.Color and the slider.ThumbColor
        hueSlider.Value = 360 * newValue.GetHue();
        transperancySlider.Value = newValue.Alpha;

        hueSlider.ValueChanged += Slider_ValueChanged;
        transperancySlider.ValueChanged += Transperancy_ValueChanged;

        double saturationX = (1 - newValue.GetHsvSaturation()) * -(Width - MarginBuffer);
        double hsvValueY = newValue.GetPercentBlackKey() * (HsvLayersHeight - MarginBuffer);

        accumulatedPickerSaturation
           = colorPickerEllipse.TranslationX
           = Math.Clamp(saturationX, -Width + MarginBuffer, 0);

        accumulatedHSVValue
            = colorPickerEllipse.TranslationY
            = Math.Clamp(hsvValueY, 0, HsvLayersHeight - MarginBuffer);
    }
}