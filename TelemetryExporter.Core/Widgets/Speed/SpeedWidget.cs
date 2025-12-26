using SkiaSharp;

using TelemetryExporter.Core.Models;
using TelemetryExporter.Core.Utilities;
using TelemetryExporter.Core.Widgets.Interfaces;

namespace TelemetryExporter.Core.Widgets.Speed
{
    public class SpeedWidget : GaugeBaseWidget, IWidget
    {
        private const string UnitTextKey = "UnitText";

        public string Category => TECoreContsants.Categories.Speed;

        public string Name => "SpeedWidget";

        public string DisplayName => "Speed";

        public string ImagePath => "Images/ExampleSpeed.png";

        public Task<SKData> GenerateImage(SessionData sessionData, FrameData currentData)
        {
            string unitTextValue = GetSetting<string>(UnitTextKey);
            return GetImageData(sessionData.MaxSpeed, currentData.Speed, unitTextValue);
        }

        public override void LoadDefaultSettings()
        {
            base.LoadDefaultSettings();
            settingsValues.Add(UnitTextKey, new SettingsModel(UnitTextKey, "KM/H"));
        }
    }
}
