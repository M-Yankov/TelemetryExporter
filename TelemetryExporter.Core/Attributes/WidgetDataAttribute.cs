namespace TelemetryExporter.Core.Attributes
{

    /// <summary>
    /// Hold widget data as attribute is easy: doesn't require instance of the widget, but accessed via reflection.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class WidgetDataAttribute : Attribute
    {
        public int Index { get; set; }

        public string ExampleImagePath { get; set; } = "Images/ExampleWidget";

        public required string Category { get; set; }
    }
}
