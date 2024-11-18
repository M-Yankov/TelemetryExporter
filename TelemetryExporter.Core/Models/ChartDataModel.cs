namespace TelemetryExporter.Core.Models;

/// <summary>
/// Model used for charts initialization.
/// </summary>
public class ChartDataModel
{
    public float? Altitude { get; set; }

    public int? Latitude { get; set; }

    public int? Longitude { get; set; }
    /// <summary>
    /// DateTime of the Record;
    /// </summary>
    public DateTime RecordDateTime { get; set; }
}

