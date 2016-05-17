namespace EventTraceKit.VsExtension
{
    using System;
    using System.Windows.Media;

    public static class ColorExtensions
    {
        public static HsvColor ToHsvColor(this Color color)
        {
            double max = Math.Max(color.ScR, Math.Max(color.ScG, color.ScB));
            double min = Math.Min(color.ScR, Math.Min(color.ScG, color.ScB));
            double chroma = max - min;

            double hue;
            double saturation;
            double value;

            if (chroma == 0 || max == 0) {
                hue = 0;
                saturation = 0;
                value = 0;
            } else {
                if (max == color.ScR)
                    hue = (color.ScG - color.ScB) / chroma;
                else if (max == color.G)
                    hue = (color.ScB - color.ScR) / chroma + 2;
                else
                    hue = (color.ScR - color.ScG) / chroma + 4;

                hue *= 60;
                if (hue < 0)
                    hue += 360;

                saturation = chroma / max;
                value = max;
            }

            return new HsvColor(hue, saturation, value);
        }

        public static HslColor ToHslColor(this Color color)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double chroma = max - min;

            double hue;
            double saturation;
            double lightness;

            if (r == g && g == b) {
                hue = 0;
                saturation = 0;
                lightness = max;
            } else {
                if (max == r)
                    hue = (g - b) / chroma;
                else if (max == g)
                    hue = (b - r) / chroma + 2;
                else
                    hue = (r - g) / chroma + 4;

                hue *= 60;
                if (hue < 0)
                    hue += 360;

                saturation = chroma / (1 - Math.Abs(max + min - 1));
                lightness = (max + min) / 2;
            }

            return new HslColor(color.A / 255.0, hue, saturation, lightness);
        }
    }
}