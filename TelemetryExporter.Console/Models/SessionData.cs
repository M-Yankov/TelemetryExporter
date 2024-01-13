namespace TelemetryExporter.Console.Models
{
    /// <summary>
    /// Contains constant data that is same for all frames.
    /// </summary>
    internal class SessionData
    {
        public double TotalDistance { get; set; }

        public double MaxSpeed { get; set; }

        /// <summary>
        /// Count of all <see cref="Dynastream.Fit.RecordMesg"/> in current session.
        /// </summary>
        public int CountOfRecords { get; set; }

    }
}
