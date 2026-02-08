using TelemetryExporter.Core.Models;

namespace TelemetryExporter.Core.Widgets.Interfaces
{
    public interface IHaveSettings
    {
        /// <summary>
        /// The idea of this property is to know what options can be configured for this widget.
        /// <para/> And void using hard-coded values in <see cref="IWidget.GenerateImage(SessionData, FrameData)"/> 
        /// meaning that the values will come as a parameter.
        /// </summary>
        IReadOnlyDictionary<string, SettingsModel> SettingsData { get; }

        /// <summary>
        /// The idea of this property is to know what options can be configured for this widget.
        /// <para/> And void using hard-coded values in <see cref="IWidget.GenerateImage(SessionData, FrameData)"/> 
        /// meaning that the values will come as a parameter.
        /// </summary>
        public IReadOnlyList<SettingsModel> Settings => [.. SettingsData.Values];

        public void SetSetting(string key, object value);

        public void LoadDefaultSettings();
    }
}
