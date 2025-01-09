namespace TelemetryExporter.Core.Models
{
    public class FitMessageModel
    {
        public double? Speed { get; set; }
        public double? Distance { get; set; }
        public float? Altitude { get; set; }
        public int? Lattitude { get; set; }
        public int? Longitude { get; set; }
        public ushort? Power { get; set; }
        public DateTime RecordDateTime { get; set; }

        /// <summary>
        /// Means the index of the Record in FitMessages.RecordMessages
        /// </summary>
        public int IndexOfRecord { get; set; }
    }
}
