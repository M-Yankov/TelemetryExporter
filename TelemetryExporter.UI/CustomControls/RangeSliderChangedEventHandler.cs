namespace TelemetryExporter.UI.CustomControls
{
    public delegate void RangeSliderChangedEventHandler<T>(RangeSlider rangeSlider, RangeSliderChangedEventArgs<T> e) where T : struct;

    /// <summary>
    /// Arguments of the range slider, when start/end value changed.
    /// </summary>
    /// <typeparam name="T">Struct, in current case DateTime</typeparam>
    public class RangeSliderChangedEventArgs<T>() : EventArgs where T : struct
    {
        /// <summary>
        /// Left slider value.
        /// </summary>
        public T StartValue { get; set; }

        /// <summary>
        /// Right slider value.
        /// </summary>
        public T EndValue { get; set; }

        /// <summary>
        /// This is the value relative to <see cref="RangeSlider.MaxValue"/>.
        /// Example: If <see cref="StartValuePercentage"/> if 0.2 (20%) and <see cref="RangeSlider.MaxValue"/> is 100, then the <see cref="StartValue"/> will be 20.
        /// Default is 0.
        /// </summary>
        public double StartValuePercentage { get; set; }

        /// <summary>
        /// This is the value relative to <see cref="RangeSlider.MaxValue"/>.
        /// Example: If <see cref="StartValuePercentage"/> if 0.8 (80%) and <see cref="RangeSlider.MaxValue"/> is 100, then the <see cref="StartValue"/> will be 80.
        /// Default is 1.
        /// </summary>
        public double EndValuePercentage { get; set;}
    }
}
