using SkiaSharp;

using TelemetryExporter.Core.Extensions;
using TelemetryExporter.Core.Models;
using TelemetryExporter.Core.SettingsTypes;

namespace TelemetryExporter.Core.Widgets
{
    public abstract class GaugeBaseWidget : BaseWidget
    {
        #region WidgetConfigSettings
        private SKColor RadialColor => GetSetting<SKColor>(Keys.RadialColor);
        private float TextSize => GetSetting<float>(Keys.TextSize);
        private string FontFamily => GetSetting<string>(Keys.FontFamily);
        private SKColor TextColor => GetSetting<SKColor>(Keys.TextColor);
        private SKColor RadialProgressColor => GetSetting<SKColor>(Keys.RadialProgressColor);
        private float UnitTextSize => GetSetting<float>(Keys.UnitTextSize);
        #endregion

        public Task<SKData> GetImageData(double maxValue, double currentValue, string text)
        {
            using SKBitmap radial = SKBitmap.FromImage(
                SKImage.FromEncodedData(PathExtensions.Combine("Images", "radial_6.png")));
            using SKBitmap dial = SKBitmap.FromImage(
                SKImage.FromEncodedData(PathExtensions.Combine("Images", "radial_6_dial.png")));

            SKImageInfo info = new(radial.Width, radial.Height, SKImageInfo.PlatformColorType, SKAlphaType.Unpremul);
            using SKSurface surface = SKSurface.Create(info);

            using SKCanvas canvas = surface.Canvas;

            using SKPaint blendPaint = new()
            {
                ColorFilter = SKColorFilter.CreateBlendMode(RadialColor, SKBlendMode.SrcIn)
            };

            canvas.DrawBitmap(radial, 0, 0, blendPaint);

            using SKPaint textPaint = new()
            {
                Color =  TextColor,
                TextSize = TextSize,
                TextAlign = SKTextAlign.Center,
                Typeface = SKTypeface.FromFamilyName(FontFamily),
                IsAntialias = true,
            };

            // -130; 130 = 260
            double percentage = currentValue / maxValue;
            percentage = double.IsNaN(percentage) ? 0 : percentage;
            double percentageDial = 260 * percentage;
            double dialDegrees = -130 + percentageDial;

            using SKPaint radialTrailPaint = new()
            {
                Color = RadialProgressColor,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 20
            };

            canvas.DrawArc(SKRect.Create(57, 57, 186, 186), 140, (float)percentageDial, false, radialTrailPaint);

            // could use the scale from garmin
            SKPoint textZeroValueCoords = new(70, 250);
            SKPoint textMaxValueCoords = new(230, 250);
            SKPoint textAverageValueCoords = new(radial.Width / 2, 28);
            SKPoint textCurrentValueCoords = new(radial.Width / 2, 250);

            SKPoint textUnitValueCoords = new(radial.Width / 2, 250 + 45);

            canvas.DrawText("0", textZeroValueCoords, textPaint);
            canvas.DrawText($"{maxValue:0}", textMaxValueCoords, textPaint);
            canvas.DrawText($"{(maxValue / 2):0}", textAverageValueCoords, textPaint);

            textPaint.TextSize = UnitTextSize;
            canvas.DrawText($"{currentValue:0}", textCurrentValueCoords, textPaint);
            canvas.DrawText(text, textUnitValueCoords, textPaint);

            canvas.SaveLayer();

            // Move anchor point to the center
            SKPoint centerPoint = new(radial.Width / 2f, radial.Height / 2f);
            canvas.Translate(centerPoint);

            canvas.RotateDegrees((float)dialDegrees);

            canvas.Translate(-centerPoint.X, -centerPoint.Y);

            canvas.DrawBitmap(dial, 0, 0);

            canvas.Restore();

            using SKImage imageExport = surface.Snapshot();
            SKData data = imageExport.Encode(SKEncodedImageFormat.Png, 100);
            return Task.FromResult(data);
        }

        public override void LoadDefaultSettings()
        {
            base.LoadDefaultSettings();
            settingsValues.Add(Keys.RadialColor,
                new SettingsModel(Keys.RadialColor, new SKColor(200, 0, 0, 255), Info: "Color of the arc")); 
            settingsValues.Add(Keys.TextColor,
                new SettingsModel(Keys.TextColor, SKColors.White, Info: "Color of the number"));
            settingsValues.Add(Keys.FontFamily,
                new SettingsModel(Keys.FontFamily, "Consolas", typeof(FontStringOptions)));
            settingsValues.Add(Keys.TextSize,
                new SettingsModel(Keys.TextSize, 16f, Min: 6f, Max: 26f, Info: "Size of text numbers around radial, min, max and avg. "));
            settingsValues.Add(Keys.RadialProgressColor,
                new SettingsModel(Keys.RadialProgressColor, new SKColor(255, 255, 255, 100),
                Info: "Color of the radial progress indicator"));
            settingsValues.Add(Keys.UnitTextSize,
                new SettingsModel(Keys.UnitTextSize, 48f, Min: 38f, Max: 58f, Info: "Size of the main text below"));
        }

        private class Keys
        {
            internal const string RadialColor = "RadialColor";
            internal const string TextColor = "TextColor";
            internal const string FontFamily = "FontFamily";
            internal const string TextSize = "RadialNumbersTextSize";
            internal const string RadialProgressColor = "RadialProgressColor";
            internal const string UnitTextSize = "UnitTextSize";
        }
    }
}
