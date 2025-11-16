using System.Xml.Linq;

using TelemetryExporter.Core.Models;

namespace TelemetryExporter.Core.Widgets
{
    public class BaseWidget
    {
        public BaseWidget()
        {
            this.settingsValues = [];
        }

        internal protected readonly Dictionary<string, SettingsModel> settingsValues;

        public IReadOnlyDictionary<string, SettingsModel> SettingsData => settingsValues;

        public T GetSetting<T>(string key)
        {
            if (SettingsData.TryGetValue(key, out SettingsModel? setting))
            {
                return (T)setting!.Value!;
            }

            throw new KeyNotFoundException($"The setting with key '{key}' was not found in the widget.");
        }
    }
}
