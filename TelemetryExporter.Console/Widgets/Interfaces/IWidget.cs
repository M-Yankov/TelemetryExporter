using TelemetryExporter.Console.Models;

namespace TelemetryExporter.Console.Widgets.Interfaces
{
    internal interface IWidget
    {
        Task GenerateImageAsync(SessionData sessionData, FrameData currentData);
    }
}
