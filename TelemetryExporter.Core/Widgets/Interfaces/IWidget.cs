using TelemetryExporter.Core.Models;

namespace TelemetryExporter.Core.Widgets.Interfaces
{
    internal interface IWidget
    {
        Task GenerateImageAsync(SessionData sessionData, FrameData currentData);
    }
}
