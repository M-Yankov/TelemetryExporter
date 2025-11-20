namespace TelemetryExporter.UI.Extensions
{
    public static class ColorExtensions
    {
        public static float GetHsvBlack(this Color color)
        {
            return Math.Max(Math.Max(color.Red, color.Green), color.Blue);
        }

        public static float GetHsvSaturation(this Color color)
        {
            color.ToHsv(out _, out float s, out _);
            return s;
        }

        private static void ToHsv(this Color color, out float h, out float s, out float v)
        {
            var r = color.Red;
            var g = color.Green;
            var b = color.Blue;

            v = Math.Max(Math.Max(r, g), b);
            float m = Math.Min(Math.Min(r, g), b);
            float diff = v - m;

            // Hue (0..1)
            if (diff == 0f)
            {
                h = 0f;
            }
            else if (v == r)
            {
                h = (g - b) / diff;
                // bring into range 0..6 then divide by 6 below
                if (h < 0) h += 6f;
            }
            else if (v == g)
            {
                h = (b - r) / diff + 2f;
            }
            else // v == b
            {
                h = (r - g) / diff + 4f;
            }

            if (diff == 0f)
            {
                h = 0f;
            }
            else
            {
                h /= 6f;
                if (h < 0f) h += 1f;
            }

            // HSV saturation
            s = v == 0f ? 0f : diff / v;
        }
    }
}