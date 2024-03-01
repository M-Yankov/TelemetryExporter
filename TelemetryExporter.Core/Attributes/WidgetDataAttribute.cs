namespace TelemetryExporter.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class WidgetDataAttribute : Attribute
    {
        public int Index { get; set; }

        // set default value!
        public string ExampleImagePath { get; set; }
    }
}
