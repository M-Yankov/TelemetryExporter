namespace TelemetryExporter.UI.Services;

public partial class DirectoryProvider
{
    /// <summary>
    /// Windows
    /// </summary>
    public static partial string? GetDesktopDirectory()
       => Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
}
