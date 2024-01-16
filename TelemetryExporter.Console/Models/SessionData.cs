namespace TelemetryExporter.Console.Models
{
    /// <summary>
    /// Contains constant data that is same for all frames.
    /// </summary>
    internal class SessionData
    {
        public double TotalDistance { get; init; }

        public double MaxSpeed { get; init; }

        /// <summary>
        /// Count of all <see cref="Dynastream.Fit.RecordMesg"/> in current session.
        /// </summary>
        public int CountOfRecords { get; init; }

    }
}
