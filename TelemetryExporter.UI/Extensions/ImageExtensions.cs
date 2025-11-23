using SkiaSharp;

namespace TelemetryExporter.UI.Extensions
{
    public static class ImageExtensions
    {
        public static void GenerateCheckedBoardBackground(this Image image)
        {
            SKBitmap bitmap = new(16, 16);
            using SKCanvas canvas = new(bitmap);
            using SKPaint grayPaint = new() { Color = SKColors.Gray };
            using SKPaint whitePaint = new() { Color = SKColors.White };

            float rectSize = bitmap.Width / 2;
            canvas.Clear(SKColors.White);
            canvas.DrawRect(0, 0, rectSize, rectSize, grayPaint);
            canvas.DrawRect(rectSize, 0, rectSize, rectSize, whitePaint);
            canvas.DrawRect(0, rectSize, rectSize, rectSize, whitePaint);
            canvas.DrawRect(rectSize, rectSize, rectSize, rectSize, grayPaint);

            using SKPaint repeatPaint = new()
            {
                Shader = SKShader.CreateBitmap(bitmap, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat)
            };

            SKImageInfo info = new((int)image.WidthRequest, (int)image.HeightRequest, SKImageInfo.PlatformColorType, SKAlphaType.Unpremul);
            using SKSurface surface = SKSurface.Create(info);

            SKCanvas canvas2 = surface.Canvas;
            canvas2.DrawRect(0, 0, info.Size.Width, info.Size.Height, repeatPaint);
            using SKImage skImage = surface.Snapshot();
            SKData data = skImage.Encode(SKEncodedImageFormat.Png, 100);

            MemoryStream memoryStream = new(data.ToArray());

            image.Source = ImageSource.FromStream(() => memoryStream);
        }
    }
}
