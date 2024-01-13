namespace TelemetryExporter.Console.Models
{
    /// <summary>
    /// This class contains variable data for the current frame. Each frame contains different values.
    /// </summary>
    internal class FrameData
    {
        /// <summary>
        /// Name of the current frame file. Like frame_001, frame_002.
        /// </summary>
        public required string FileName { get; set; }

        public double Pace { get; set; }

        /// <summary>
        /// Y
        /// </summary>
        public int? Latitude { get; set; }

        /// <summary>
        /// X
        /// </summary>
        public int? Longitude { get; set; }

        public double? Altitude { get; set; }

        public double? Distance { get; set; }

        public double Speed { get; set; }

        /// <summary>
        /// This is the current index of working <see cref="Dynastream.Fit.RecordMesg"/>. 
        /// Multiple frames can have same index of record. It depends on the used FPS and the interval (dates) between records.
        /// </summary>
        public int IndexOfCurrentRecord { get; set; }
    }
}
