using System.ComponentModel;

using CommunityToolkit.Maui.Core.Extensions;

namespace TelemetryExporter.UI.CustomControls;

public partial class ColorPicker : ContentView, INotifyPropertyChanged
{
    private double maxPickerX = 0;
    private double accumulatedPickerSaturation = 0;
    private double accumulatedHSVValue = 0;

    private const double hsvLayersHeight = 130;
    private const int marginBuffer = 2;

    /// <summary>
    /// null means no color selected, reset.
    /// </summary>
    public event EventHandler<Color?>? OnColorChanged;

    private Color? selectedColor;//= Colors.Red;
    public Color? SelectedColor 
    { 
        get => selectedColor; 
        set
        {
            selectedColor = value;
            OnColorChanged?.Invoke(this, selectedColor);
            OnPropertyChanged(nameof(SelectedColor));
        }
    }

    public ColorPicker()
    {
        
        InitializeComponent();

        // NOt working when color set from XAML,from the user component

        // if (selectedColor is null)
        // set values slider according to the hue value of the selected color
        // set colorPickerEllipse position according to the saturation and value of the selected color
        // transationX and transtionY and belare of the marginBuffer
        if (SelectedColor is not null)
        {
            hueSlider.Value = SelectedColor.GetHue();
            accumulatedPickerSaturation = -(SelectedColor.GetSaturation() * (Width - marginBuffer));
            accumulatedHSVValue = SelectedColor.GetPercentBlackKey() * (hsvLayersHeight - marginBuffer);
            colorPickerEllipse.TranslationX = accumulatedPickerSaturation;
            colorPickerEllipse.TranslationY = accumulatedHSVValue;
            hueLayerStop.Color = Color.FromHsv((int)hueSlider.Value, 100, 100);
            hueSlider.ThumbColor = hueLayerStop.Color;
        }else
        {
            selectedColor = Colors.Red;
        }


            this.secondRowDefinition.Height = new GridLength(hsvLayersHeight);

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
        }
    }

    private void Slider_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        if (sender is Slider slider)
        {
            Color newHue = Color.FromHsv((int)e.NewValue, 100, 100);
            slider.ThumbColor = hueLayerStop.Color = newHue;

            this.SelectedColor = this.SelectedColor.WithHue(newHue.GetHue());
        }
    }

    private void DragRegognizer_PanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Running:
                colorPickerEllipse.TranslationX = Math.Clamp(accumulatedPickerSaturation + e.TotalX, -Width + marginBuffer, 0);
                colorPickerEllipse.TranslationY = Math.Clamp(accumulatedHSVValue + e.TotalY, 0, hsvLayersHeight - marginBuffer);
                break;
            case GestureStatus.Completed:
                accumulatedPickerSaturation = colorPickerEllipse.TranslationX;
                accumulatedHSVValue = colorPickerEllipse.TranslationY;

                /// Color c = Color.FromHsv((int)this.hueSlider.Value, 100, 100);
                var h = hueLayerStop.Color.GetHue();

                float s = (float)(accumulatedPickerSaturation / (-Width + marginBuffer));
                float v = (float)(accumulatedHSVValue / (hsvLayersHeight - marginBuffer));

                this.SelectedColor = Color.FromHsv(h, 1- s, 1- v);
                ;

                break;
            default:
                break;
        }
    }
}