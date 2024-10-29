namespace TelemetryExporter.Core.Attributes
{
    /// <summary>
    /// Hold widget data as attribute is easy: doesn't require instance of the widget, but accessed via reflection.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class WidgetDataAttribute : Attribute
    {
        // Used in UI ...
        public int Index { get; set; }

        /// <summary>
        /// Image Properties > Copy to output directory > Copy Always
        /// </summary>
        public string ExampleImagePath { get; set; } = "Images/ExampleWidget.png";

        public required string Category { get; set; }
    }
}
