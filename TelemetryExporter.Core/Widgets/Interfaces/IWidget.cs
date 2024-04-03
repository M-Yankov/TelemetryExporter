using SkiaSharp;

using TelemetryExporter.Core.Models;

namespace TelemetryExporter.Core.Widgets.Interfaces
{
    public interface IWidget
    {
        /// <summary>
        /// Should be unique across all widgets.
        /// </summary>
        int Index { get; }

        /// <summary>
        /// To group widgets by category.
        /// </summary>
        string Category { get; }

        /// <summary>
        /// Should be unique across all widgets.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Example image shown in UI.
        /// </summary>
        string ExampleImagePath => "Images/ExampleWidget";

        Task<SKData> GenerateImage(SessionData sessionData, FrameData currentData);
    }
}
