namespace TelemetryExporter.Core.Attributes
{
    [System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    internal sealed class WidgetDataAttribute : Attribute
    {
        public int Index { get; set; }

        // set default value!
        public string ExampleImagePath { get; set; }
    }
}
