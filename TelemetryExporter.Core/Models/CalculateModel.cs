namespace TelemetryExporter.Core.Models
{
    public class CalculateModel(DateTime previousDate, DateTime nextDate, double? previousValue, double? nextValue, DateTime calculateAt)
    {
        public DateTime PreviousDate { get; set; } = previousDate;

        public DateTime NextDate { get; set; } = nextDate;

        public double? PreviousValue { get; set; } = previousValue;

        public double? NextValue { get; set; } = nextValue;

        public DateTime CalculateAt { get; set; } = calculateAt;
    }
}
