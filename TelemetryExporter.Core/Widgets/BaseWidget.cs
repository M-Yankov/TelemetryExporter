using TelemetryExporter.Core.Models;

namespace TelemetryExporter.Core.Widgets
{
    public class BaseWidget
    {
        private protected readonly Dictionary<string, SettingsModel> settingsValues = [];

        public IReadOnlyDictionary<string, SettingsModel> SettingsData => settingsValues;

        public T GetSetting<T>(string key)
        {
            if (SettingsData.TryGetValue(key, out SettingsModel? setting))
            {
                return (T)setting!.Value!;
            }

            throw new KeyNotFoundException($"The setting with key '{key}' was not found in the widget.");
        }

        public void SetSetting(string key, object value)
        {
            if (SettingsData.TryGetValue(key, out SettingsModel? oldSetting) && oldSetting != null)
            {
                SettingsModel? newS = oldSetting with { Value = value };
                settingsValues[key] = newS;
            }
        }
    }
}
