namespace TelemetryExporter.UI.CustomControls;

public partial class ColorPicker : ContentView
{
	public ColorPicker()
	{
		InitializeComponent();

        // this.sliderBackground.Background.
		const int MaxHueDegree = 360;
        for (int i = 0; i <= MaxHueDegree; i+=10)
		{
			GradientStop gradientStop = new ();
			gradientStop.Offset = i / (float)MaxHueDegree;
            gradientStop.Color = Color.FromHsv(i, 100, 100);
            sliderBackgroundBrush.GradientStops.Add(gradientStop);
		}
    }

    private void Slider_ValueChanged(object sender, ValueChangedEventArgs e)
    {
		if (sender is Slider slider)
		{
			Color newHue = Color.FromHsv((int)e.NewValue, 100, 100);
            slider.ThumbColor = hueLayerStop.Color = newHue;
        }
    }
}