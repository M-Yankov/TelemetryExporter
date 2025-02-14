using SkiaSharp;

using TelemetryExporter.Core.Widgets.Interfaces;

namespace TelemetryExporter.Core.Models
{
    internal class GeneratedWidgetDataModel
    {
        public required SKData SkData { get; set; }

        public required string Filename { get; set; }

        public required IWidget Widget { get; set; }

        public double PercentageDone { get; set; }
    }
}
