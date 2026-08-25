using Foundation;

namespace TelemetryExporter.UI.Services;

/// <summary>
/// https://learn.microsoft.com/en-us/dotnet/maui/macios/system-special-folders?view=net-maui-10.0#match-macos-behavior-on-mac-catalyst
/// </summary>
public partial class DirectoryProvider
{
    public static partial string? GetDesktopDirectory()
        => new NSFileManager().GetUrls(NSSearchPathDirectory.DesktopDirectory, NSSearchPathDomain.User)
                .FirstOrDefault()
                ?.Path;
}
