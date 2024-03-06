namespace TelemetryExporter.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class WidgetDataAttribute : Attribute
    {
        public int Index { get; set; }

        public string ExampleImagePath { get; set; } = "Images/ExampleWidget";

        public required string Category { get; set; }
    }
}
