namespace TelemetryExporter.Console.Models
{
    internal class UIWidgetData(int index, string name)
    {
        public int Index { get; set; } = index;

        public string Name { get; set; } = name;
    }
}
