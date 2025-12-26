using SkiaSharp;

using TelemetryExporter.Core.Models;
using TelemetryExporter.Core.SettingsTypes;
using TelemetryExporter.Core.Utilities;
using TelemetryExporter.Core.Widgets.Interfaces;

namespace TelemetryExporter.Core.Widgets.Elevation
{
    public class ElevationWidget : BaseWidget, IWidget, INeedInitialization
    {
        private const int ElevationPictureWidthPixels = 700;
        private const int ElevationPictureHeightPixels = 250;

        private SKPath elevationPath = new ();
        private LineChartData? lineChartData = null;
        private bool isInitialized = false;

        public string Category => TECoreContsants.Categories.Elevation;

        public string Name => "ElevationWidget";

        public string DisplayName => "Elevation";

        public string ImagePath => "Images/ExampleElevation.png";

        #region WidgetConfigSettings
        private string ElevationText => GetSetting<string>(Keys.ElevationText);
        private SKColor ElevationLine => GetSetting<SKColor>(Keys.ElevationLine);
        private SKColor BottomColor => GetSetting<SKColor>(Keys.PathBottomColor);
        private SKColor TextColor => GetSetting<SKColor>(Keys.TextColor);
        private SKColor BackgroundColor => GetSetting<SKColor>(Keys.BackgroundColor);
        private SKColor DotColor => GetSetting<SKColor>(Keys.DotColor);
        private string FontFamily => GetSetting<string>(Keys.FontFamily);
        private float TitleTextSize => GetSetting<float>(Keys.TitleTextSize);
        #endregion

        public void Initialize(IReadOnlyCollection<ChartDataModel> dataMessages)
        {
            lineChartData = new(dataMessages, ElevationPictureWidthPixels, ElevationPictureHeightPixels, offsetPercentageY: .20f);
            elevationPath = lineChartData.LinePath;
            isInitialized = true;
        }

        public Task<SKData> GenerateImage(SessionData sessionData, FrameData currentData)
        {
            if (!isInitialized)
            {
                throw new InvalidOperationException($"Cannot use widget ${nameof(ElevationWidget)}, before initialization!");
            }

            SKImageInfo info = new(ElevationPictureWidthPixels, ElevationPictureHeightPixels, SKImageInfo.PlatformColorType, SKAlphaType.Unpremul);
            using SKPaint blackPaint = new()
            {
                Color = ElevationLine, 
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };

            using SKPaint trasparentBlack = new()
            {
                Color = BottomColor,
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
            };

            using SKPaint textPaint = new()
            {
                Color = TextColor,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName(FontFamily),
                TextSize = TitleTextSize,
            };

            using SKPaint redPaint = new()
            {
                Color = DotColor,
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                StrokeWidth = 2
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

            SKPath fillPath = new(elevationPath);
            fillPath.LineTo(ElevationPictureWidthPixels, ElevationPictureHeightPixels);
            fillPath.LineTo(0, ElevationPictureHeightPixels);

            canvas.DrawPath(fillPath, trasparentBlack);
            canvas.DrawPath(elevationPath, blackPaint);

            canvas.DrawText(ElevationText, new SKPoint(25, 35), textPaint);
            using SKPaint linePaint = textPaint.Clone();
            linePaint.StrokeWidth = 10;

            canvas.DrawLine(0, 0, ElevationPictureWidthPixels, 0, linePaint);

            SKPoint elevationPoint = SKPoint.Empty;
            if (currentData.Altitude.HasValue && lineChartData != null)
            {
                elevationPoint = lineChartData.CalculateImageCoordinates((float)currentData.Altitude.Value, currentData.IndexOfCurrentRecord, sessionData.CountOfRecords);
            }

            if (!elevationPoint.IsEmpty)
            {
                canvas.DrawCircle(elevationPoint, radius: 5, redPaint);

                float reachedToEndPercentage = elevationPoint.X / ElevationPictureWidthPixels;

                int xOffset = 10;

                using SKPaint elvationPaint = textPaint.Clone();
                if (reachedToEndPercentage > .80f)
                {
                    xOffset *= -1;
                    elvationPaint.TextAlign = SKTextAlign.Right;
                }

                int yOffset = -10;
                bool isTextOverlapped = reachedToEndPercentage < .30f
                    && (elevationPoint.Y / ElevationPictureHeightPixels) < .28f;
                if (isTextOverlapped)
                {
                    // The "Elevation" text gets overlapped in some situations
                    yOffset += 40;
                }

                canvas.DrawText($"{currentData.Altitude:F1} m", elevationPoint.X + xOffset, elevationPoint.Y + yOffset, elvationPaint);
            }

            using SKImage image = surface.Snapshot();
            SKData data = image.Encode(SKEncodedImageFormat.Png, 100);

            return Task.FromResult(data);
        }

        public override void LoadDefaultSettings()
        {
            base.LoadDefaultSettings();
            settingsValues.Add(Keys.ElevationLine,
                           new SettingsModel(Keys.ElevationLine, SKColors.Black, Info: "Color of elevation line"));
            settingsValues.Add(Keys.PathBottomColor,
                new SettingsModel(Keys.PathBottomColor, new SKColor(0, 0, 0, 170), Info: "The color below the elevation line"));
            settingsValues.Add(Keys.TextColor,
                new SettingsModel(Keys.TextColor, SKColors.White, Info: "Color of the text"));
            settingsValues.Add(Keys.BackgroundColor,
                new SettingsModel(Keys.BackgroundColor, new SKColor(0, 0, 0, 100)));
            settingsValues.Add(Keys.DotColor,
                new SettingsModel(Keys.DotColor, SKColors.Red, Info: "Color of the progress point"));
            settingsValues.Add(Keys.FontFamily,
                new SettingsModel(Keys.FontFamily, "Consolas", typeof(FontStringOptions)));
            settingsValues.Add(Keys.TitleTextSize,
                new SettingsModel(Keys.TitleTextSize, 35f, Min: 25f, Max: 45f));
            settingsValues.Add(Keys.ElevationText,
                new SettingsModel(Keys.ElevationText, "ELEVATION", Info: "The main text of the widget"));
        }

        private static class Keys
        {
            internal const string ElevationLine = "ElevationLine";
            internal const string PathBottomColor = "PathBottomColor";
            internal const string TextColor = "TextColor";
            internal const string BackgroundColor = "BackgroundColor";
            internal const string FontFamily = "FontFamily";
            internal const string TitleTextSize = "TitleTextSize";
            internal const string ElevationText = "DistanceText";
            internal const string DotColor = "DotColor";
        }
    }
}
