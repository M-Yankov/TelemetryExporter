using SkiaSharp;

using TelemetryExporter.Core.Models;
using TelemetryExporter.Core.SettingsTypes;

namespace TelemetryExporter.Core.Widgets
{
    public abstract class TextBaseWidget : BaseWidget
    {
        public abstract int WidgetWidth { get; }

        public abstract int WidgetHeight { get; }

        #region WidgetConfigSettings
        private SKColor BackgroundColor => GetSetting<SKColor>(Keys.BackgroundColor);
        private SKColor TextColor => GetSetting<SKColor>(Keys.TextColor);
        private float TextSize => GetSetting<float>(Keys.TextSize);
        private string FontFamily => GetSetting<string>(Keys.FontFamily);
        #endregion

        public Task<SKData> GetImageData(string text)
        {
            SKImageInfo info = new(WidgetWidth, WidgetHeight, SKImageInfo.PlatformColorType, SKAlphaType.Unpremul);

            using SKPaint textPaint = new()
            {
                Color = TextColor,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName(FontFamily),
                TextSize = TextSize,
            };

            using SKPaint transparentPaint = new()
            {
                Color = BackgroundColor,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            using SKSurface surface = SKSurface.Create(info);
            SKCanvas canvas = surface.Canvas;
            canvas.DrawPaint(transparentPaint);
            
            canvas.DrawText(text, new SKPoint(10, 35), textPaint);

            using SKImage image = surface.Snapshot();
            SKData data = image.Encode(SKEncodedImageFormat.Png, 100);

            return Task.FromResult(data);
        }

        public override void LoadDefaultSettings()
        {
            base.LoadDefaultSettings();
            settingsValues.Add(Keys.BackgroundColor,
                new SettingsModel(Keys.BackgroundColor, new SKColor(0, 0, 0, 100)));
            settingsValues.Add(Keys.FontFamily,
                new SettingsModel(Keys.FontFamily, "Consolas", typeof(FontStringOptions)));
            settingsValues.Add(Keys.TextColor,
                new SettingsModel(Keys.TextColor, SKColors.White));
            settingsValues.Add(Keys.TextSize,
                new SettingsModel(Keys.TextSize, 35f, Min: 25f, Max: 45f));
        }

        private class Keys
        {
            internal const string TextColor = "TextColor";
            internal const string BackgroundColor = "BackgroundColor";
            internal const string FontFamily = "FontFamily";
            internal const string TextSize = "TextSize";
        }
    }
}
