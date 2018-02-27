namespace EventTraceKit.VsExtension
{
    using System;
    using System.Windows.Media;

    public struct HslColor
    {
        public HslColor(double alpha, double hue, double saturation, double lightness)
        {
            Hue = hue;
            Saturation = saturation;
            Lightness = lightness;
            Alpha = alpha;
        }

        public double Alpha { get; set; }
        public double Hue { get; set; }
        public double Saturation { get; set; }
        public double Lightness { get; set; }

        public Color ToColor()
        {
            double chroma = (1 - Math.Abs(2 * Lightness - 1)) * Saturation;
            double h = Hue / 60;
            double m = Lightness - chroma / 2;
            double x = chroma * (1 - Math.Abs((h % 2) - 1)) + m;
            double c = chroma + m;

            double red, blue, green;
            switch ((int)Math.Floor(h) % 6) {
                case 0:
                    red = c;
                    green = x;
                    blue = m;
                    break;
                case 1:
                    red = x;
                    green = c;
                    blue = m;
                    break;
                case 2:
                    red = m;
                    green = c;
                    blue = x;
                    break;
                case 3:
                    red = m;
                    green = x;
                    blue = c;
                    break;
                case 4:
                    red = x;
                    green = m;
                    blue = c;
                    break;
                case 5:
                    red = c;
                    green = m;
                    blue = x;
                    break;
                default:
                    red = 0;
                    green = 0;
                    blue = 0;
                    break;
            }

            return Color.FromArgb(
                (byte)(Alpha * 255),
                (byte)(red * 255),
                (byte)(green * 255),
                (byte)(blue * 255));
        }
    }
}
