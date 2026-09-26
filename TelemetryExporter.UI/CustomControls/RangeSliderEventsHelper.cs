namespace TelemetryExporter.UI.CustomControls
{
    public static class RangeSliderEventsHelper
    {
        public static void SubscribeEvents(RangeSlider slider)
        {
            slider.Loaded += slider.OnContentLoaded;
            slider.SizeChanged += slider.OnContentSizeChanged;
        }

        public static void UnsubscribeEvents(RangeSlider slider)
        {
            slider.Loaded -= slider.OnContentLoaded;
            slider.SizeChanged -= slider.OnContentSizeChanged;
        }
    }
}
