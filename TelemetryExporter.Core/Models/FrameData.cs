namespace TelemetryExporter.Core.Models
{
    /// <summary>
    /// This class contains variable data for the current frame. Each frame contains different values.
    /// </summary>
    public class FrameData
    {
        /// <summary>
        /// Name of the current frame file. Like frame_001, frame_002.
        /// </summary>
        public required string FileName { get; init; }

        /// <summary>
        /// Y
        /// </summary>
        public float? Latitude { get; init; }

        /// <summary>
        /// X
        /// </summary>
        public float? Longitude { get; init; }

        public double? Altitude { get; init; }

        public double? Distance { get; init; }

        public double Speed { get; init; }

        /// <summary>
        /// This is the current index of working <see cref="Dynastream.Fit.RecordMesg"/>. 
        /// Multiple frames can have same index of record. It depends on the used FPS and the interval (dates) between records.
        /// </summary>
        public int IndexOfCurrentRecord { get; init; }

        public TimeOnly ElapsedTime { get; init; }

        public TimeOnly CurrentTime { get; init; }

        public double? Grade {  get; init; }

        public ushort? Power { get; set; }
    }
}
