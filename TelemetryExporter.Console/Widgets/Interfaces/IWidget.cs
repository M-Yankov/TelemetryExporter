using TelemetryExporter.Console.Models;

namespace TelemetryExporter.Console.Widgets.Interfaces
{
    internal interface IWidget
    {
        void GenerateImage(SessionData sessionData, FrameData currentData);
    }
}
