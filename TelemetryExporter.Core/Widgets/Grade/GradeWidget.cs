using SkiaSharp;

using TelemetryExporter.Core.Models;
using TelemetryExporter.Core.SettingsTypes;
using TelemetryExporter.Core.Utilities;
using TelemetryExporter.Core.Widgets.Interfaces;

namespace TelemetryExporter.Core.Widgets.Grade
{
    public class GradeWidget : BaseWidget, IWidget
    {
        public string Category => TECoreContsants.Categories.Grade;

        public string Name => nameof(GradeWidget);

        public string DisplayName => "Grade";

        public string ImagePath => "Images/ExampleGrade.png";

        #region WidgetConfigSettings
        private SKColor TextColor => GetSetting<SKColor>(Keys.TextColor);
        private SKColor BackgroundColor => GetSetting<SKColor>(Keys.BackgroundColor);
        private string FontFamily => GetSetting<string>(Keys.FontFamily);
        private float TitleTextSize => GetSetting<float>(Keys.TitleTextSize);

        private float InclineLineSize => GetSetting<float>(Keys.InclineLineSize);
        #endregion

        public Task<SKData> GenerateImage(SessionData sessionData, FrameData currentData)
        {
            SKImageInfo info = new(150, 250, SKImageInfo.PlatformColorType, SKAlphaType.Unpremul);
            using SKPaint transparentPaint = new()
            {
                Color = BackgroundColor,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            using SKSurface surface = SKSurface.Create(info);
            using SKCanvas canvas = surface.Canvas;
            canvas.DrawPaint(transparentPaint);

            using SKPaint textPaint = new()
            {
                Color = TextColor,
                TextSize = TitleTextSize,
                TextAlign = SKTextAlign.Center,
                Typeface = SKTypeface.FromFamilyName(FontFamily),
                IsAntialias = true,
            };

            if (currentData.Grade.HasValue 
                && !double.IsInfinity(currentData.Grade.Value)
                && !double.IsNaN(currentData.Grade.Value))
            {
                // Check if the calculation could come directly 
                canvas.DrawText((currentData.Grade.Value / 100).ToString("p0"), new SKPoint(75, 100), textPaint);
                canvas.SaveLayer();

                // rotateDegree = skPointTranslateX
                // -90 = 85 = 10
                // -45 = 80 = 5 
                // 0 = 75 = 0
                // 45 = 70 = -5
                // 90 = 65 = -10В
                float translateXOffset = (float) Math.Abs(currentData.Grade.Value / 9.0) * -1;

                textPaint.TextSize = InclineLineSize;
                canvas.Translate(new SKPoint(75 + translateXOffset, 180));
                canvas.RotateDegrees((float)-currentData.Grade.Value);
                canvas.DrawText("—————→", SKPoint.Empty, textPaint);
                canvas.Restore();
            }

            using SKImage imageExport = surface.Snapshot();
            SKData data = imageExport.Encode(SKEncodedImageFormat.Png, 100);
            return Task.FromResult(data);
        }

        public override void LoadDefaultSettings()
        {
            base.LoadDefaultSettings();
            settingsValues.Add(Keys.TextColor,
                new SettingsModel(Keys.TextColor, SKColors.White, Info: "Color of the text"));
            settingsValues.Add(Keys.BackgroundColor,
                new SettingsModel(Keys.BackgroundColor, new SKColor(0, 0, 0, 100)));
            settingsValues.Add(Keys.FontFamily,
                new SettingsModel(Keys.FontFamily, "Consolas", typeof(FontStringOptions)));
            settingsValues.Add(Keys.TitleTextSize,
                new SettingsModel(Keys.TitleTextSize, 60f, Min: 50f, Max: 70f));
            settingsValues.Add(Keys.InclineLineSize,
                new SettingsModel(Keys.InclineLineSize, 35f, Min: 25f, Max: 45f));
        }

        private static class Keys
        {
            internal const string TextColor = "TextColor";
            internal const string BackgroundColor = "BackgroundColor";
            internal const string FontFamily = "FontFamily";
            internal const string TitleTextSize = "TitleTextSize";
            internal const string InclineLineSize = "InclineLineSize";
        }
    }
}
