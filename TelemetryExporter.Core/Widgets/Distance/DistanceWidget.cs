using SkiaSharp;

using TelemetryExporter.Core.Models;
using TelemetryExporter.Core.SettingsTypes;
using TelemetryExporter.Core.Utilities;
using TelemetryExporter.Core.Widgets.Interfaces;

namespace TelemetryExporter.Core.Widgets.Distance
{
    public class DistanceWidget : BaseWidget, IWidget
    {
        public string Category => TECoreContsants.Categories.Distance;

        public string Name => "DistanceWidget";

        public string DisplayName => "Distance";

        public string ImagePath => "Images/ExampleDistance.png";

        #region WidgetConfigSettings
        private string UnitText => GetSetting<string>(Keys.UnitText);
        private string EmptyDistanceText => GetSetting<string>(Keys.EmptyDistanceText);
        private string DistanceText => GetSetting<string>(Keys.DistanceText);
        private SKColor BackgroundColor => GetSetting<SKColor>(Keys.BackgroundColor);
        private SKColor MainColor => GetSetting<SKColor>(Keys.MainColor);
        private SKColor TextColor => GetSetting<SKColor>(Keys.TextColor);
        private string FontFamily => GetSetting<string>(Keys.FontFamily);
        private float TitleTextSize => GetSetting<float>(Keys.TitleTextSize);
        #endregion

        public Task<SKData> GenerateImage(SessionData sessionData, FrameData currentData)
        {
            const int DistanceImageWidth = 700;
            const int DistanceImageHeight = 100;

            SKImageInfo info = new(DistanceImageWidth, DistanceImageHeight, SKImageInfo.PlatformColorType, SKAlphaType.Unpremul);

            using SKPaint transparentDistancePaint = new()
            {
                Color = MainColor,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            using SKPaint trasparentBlack = new()
            {
                Color = BackgroundColor,
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
            };

            using SKPaint textDistancePaint = new()
            {
                Color = TextColor,
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Typeface = SKTypeface.FromFamilyName(FontFamily),
                TextSize = TitleTextSize,
            };

            using SKPaint textDistanceNumbersPaint = textDistancePaint.Clone();
            textDistanceNumbersPaint.TextSize += (textDistanceNumbersPaint.TextSize * .15f);

            using SKSurface surface = SKSurface.Create(info);

            SKCanvas canvas = surface.Canvas;
            canvas.DrawPaint(trasparentBlack);

            string distanceAsText;
            if (currentData.Distance.HasValue)
            {
                // https://www.calculatorsoup.com/calculators/math/percentage.php
                double currentDistancePercentage = currentData.Distance.Value / sessionData.TotalDistance;
                float imagePixelsDistanceX = DistanceImageWidth * (float)currentDistancePercentage;
                canvas.DrawRect(0, 0, imagePixelsDistanceX, DistanceImageHeight, transparentDistancePaint);
                distanceAsText = $"{currentData.Distance / 1000f:F3} {UnitText}";
            }
            else
            {
                distanceAsText = $"{EmptyDistanceText} {UnitText}";
            }

            // the Points should be percentage, not hard-coded
            canvas.DrawText(DistanceText, new SKPoint(25, 35), textDistancePaint);
            canvas.DrawText(distanceAsText, new SKPoint(25, 75), textDistanceNumbersPaint);

            using SKImage image = surface.Snapshot();
            SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
            return Task.FromResult(data);
        }

        public override void LoadDefaultSettings()
        {
            base.LoadDefaultSettings();
            settingsValues.Add(Keys.BackgroundColor,
                new SettingsModel(Keys.BackgroundColor, new SKColor(0, 0, 0, 170), Info: "To disable the background use 100% transparency"));
            settingsValues.Add(Keys.FontFamily,
    new SettingsModel(Keys.FontFamily, "Consolas", typeof(FontStringOptions)));
            settingsValues.Add(Keys.MainColor,
                new SettingsModel(Keys.MainColor, new SKColor(0, 0, 0, 100), Info: "Color of the progress bar"));
            settingsValues.Add(Keys.TextColor,
                new SettingsModel(Keys.TextColor, SKColors.White, Info: "Color of the text"));

            settingsValues.Add(Keys.TitleTextSize,
                new SettingsModel(Keys.TitleTextSize, 35f, Min: 25f, Max: 45f));
            //{ Not sure I want this to be configurable yet
            //  "DistanceTextSize", new SettingsModel("DistanceTextSize", typeof(float), 40f)
            //}
            settingsValues.Add(Keys.UnitText,
                new SettingsModel(Keys.UnitText, "KM", Info: "Text of the unit"));
            settingsValues.Add(Keys.EmptyDistanceText,
                new SettingsModel(Keys.EmptyDistanceText, "--", Info: "Will be shown when data is missing, could be temporary"));
            // Eventually can set different word, e.g. different language
            settingsValues.Add(Keys.DistanceText,
                new SettingsModel(Keys.DistanceText, "DISTANCE", Info: "The main text of the widget"));
        }

        private static class Keys
        {
            internal const string BackgroundColor = "BackgroundColor";
            internal const string MainColor = "MainColor";
            internal const string TextColor = "TextColor";
            internal const string FontFamily = "FontFamily";
            internal const string TitleTextSize = "TitleTextSize";
            internal const string UnitText = "UnitText";
            internal const string EmptyDistanceText = "EmptyTextDistance";
            internal const string DistanceText = "DistanceText";
        }
    }
}
