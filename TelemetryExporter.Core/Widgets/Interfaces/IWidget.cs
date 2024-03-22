using SkiaSharp;

using TelemetryExporter.Core.Models;

namespace TelemetryExporter.Core.Widgets.Interfaces
{
    public interface IWidget
    {
        Task<SKData> GenerateImage(SessionData sessionData, FrameData currentData);
    }
}
