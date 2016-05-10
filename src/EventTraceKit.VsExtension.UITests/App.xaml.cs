namespace EventTraceKit.VsExtension.UITests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Media;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using EventTraceKit.VsExtension;
    using Microsoft.VisualStudio.Shell;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly List<string> availableThemes = new List<string>();

        public new static App Current => Application.Current as App;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            availableThemes.AddRange(FindAvailableThemes());
            TryLoadTheme("VisualStudio.Dark");
        }

        public IReadOnlyList<string> AvailableThemes => availableThemes;

        public string ActiveTheme { get; private set; }

        public bool TryLoadTheme(string name)
        {
            if (ActiveTheme == name)
                return true;

            if (!LoadThemeFromResource(name + ".vstheme"))
                return false;

            UpdateTraceLogTheme();
            ActiveTheme = name;
            return true;
        }

        private List<string> FindAvailableThemes()
        {
            var assembly = GetType().Assembly;
            return assembly.GetManifestResourceNames()
                .Select(GetThemeName).Where(n => n != null)
                .OrderBy(x => x).ToList();
        }

        private string GetThemeName(string resourceName)
        {
            string prefix = typeof(App).Namespace + ".Themes.";
            string suffix = ".vstheme";
            if (!resourceName.StartsWith(prefix) || !resourceName.EndsWith(suffix))
                return null;

            int length = resourceName.Length - prefix.Length - suffix.Length;
            return resourceName.Substring(prefix.Length, length);
        }

        private bool LoadThemeFromResource(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream(typeof(App), "Themes." + name);
            if (stream == null)
                return false;

            using (stream)
                LoadThemeFromStream(stream);

            return true;
        }

        private void LoadThemeFromStream(Stream input)
        {
            var doc = XDocument.Load(input);

            foreach (var category in doc.XPathSelectElements("Themes/Theme/Category")) {
                Guid id = Guid.Parse(category.Attribute("GUID").Value);

                foreach (var color in category.Elements("Color")) {
                    string name = color.Attribute("Name")?.Value;

                    var foreground = GetColor(color.Element("Foreground"));
                    if (foreground != null)
                        SetColor(id, name, foreground.Value, true);

                    var background = GetColor(color.Element("Background"));
                    if (background != null)
                        SetColor(id, name, background.Value, false);

                    if (foreground == null && name == "Plain Text")
                        SetColor(id, name, SystemColors.ControlTextColor, true);

                    if (background == null && name == "Plain Text")
                        SetColor(id, name, SystemColors.ControlColor, false);
                }
            }
        }

        private void UpdateTraceLogTheme()
        {
            var id = new Guid("9973EFDF-317D-431C-8BC1-5E88CBFD4F7F");
            var foregroundKey = new ThemeResourceKey(id, "Plain Text", ThemeResourceKeyType.ForegroundBrush);
            var backgroundKey = new ThemeResourceKey(id, "Plain Text", ThemeResourceKeyType.BackgroundBrush);
            var selectedBackgroundKey = new ThemeResourceKey(
                id, "Selected Text", ThemeResourceKeyType.BackgroundBrush);
            var inactiveSelectedBackgroundKey = new ThemeResourceKey(
                id, "Inactive Selected Text", ThemeResourceKeyType.BackgroundBrush);

            Resources[EtkFonts.TraceLogEntryFontFamilyKey] = new FontFamily("Consolas");
            Resources[EtkFonts.TraceLogEntryFontSizeKey] = 9;
            Resources[EtkColors.TraceLogForegroundKey] = Resources[foregroundKey];
            Resources[EtkColors.TraceLogBackgroundKey] = Resources[backgroundKey];
            Resources[EtkColors.TraceLogBackgroundAltKey] = Darken((SolidColorBrush)Resources[backgroundKey]);
            Resources[EtkColors.TraceLogSelectedBackgroundKey] = Resources[selectedBackgroundKey];
            Resources[EtkColors.TraceLogInactiveSelectedBackgroundKey] = Resources[inactiveSelectedBackgroundKey];
        }

        private SolidColorBrush Darken(SolidColorBrush brush)
        {
            var hsl = brush.Color.ToHslColor();
            hsl.Lightness = Math.Max(0, hsl.Lightness - 0.05);
            return new SolidColorBrush(hsl.ToColor());
        }

        private void SetColor(Guid id, string name, Color color, bool isForeground)
        {
            var colorType = isForeground
                ? ThemeResourceKeyType.ForegroundColor
                : ThemeResourceKeyType.BackgroundColor;

            var brushType = isForeground
                ? ThemeResourceKeyType.ForegroundBrush
                : ThemeResourceKeyType.BackgroundBrush;

            var colorKey = new ThemeResourceKey(id, name, colorType);
            var brushKey = new ThemeResourceKey(id, name, brushType);

            Resources[colorKey] = color;
            Resources[brushKey] = new SolidColorBrush(color);
        }

        [DllImport("User32.dll")]
        private static extern uint GetSysColor(int nIndex);

        private Color? GetColor(XElement element)
        {
            if (element == null)
                return null;

            string type = element.Attribute("Type")?.Value;
            string source = element.Attribute("Source")?.Value;

            switch (type) {
                case "CT_RAW":
                    if (source == null || source.Length != 8)
                        return null;

                    byte a = Convert.ToByte(source.Substring(0, 2), 16);
                    byte r = Convert.ToByte(source.Substring(2, 2), 16);
                    byte g = Convert.ToByte(source.Substring(4, 2), 16);
                    byte b = Convert.ToByte(source.Substring(6, 2), 16);

                    return Color.FromArgb(a, r, g, b);

                case "CT_INVALID":
                    return null;

                case "CT_SYSCOLOR":
                    int index = Convert.ToInt32(source, 16);
                    uint color = GetSysColor(index);
                    a = (byte)((color >> 32) & 0xFF);
                    r = (byte)((color >> 16) & 0xFF);
                    g = (byte)((color >> 8) & 0xFF);
                    b = (byte)((color >> 0) & 0xFF);

                    return Color.FromArgb(a, r, g, b);

                default:
                    return null;
            }
        }
    }

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

    public struct HsvColor
    {
        public HsvColor(double hue, double saturation, double value)
        {
            Hue = hue;
            Saturation = saturation;
            Value = value;
        }

        public double Hue { get; set; }
        public double Saturation { get; set; }
        public double Value { get; set; }
    }

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

            double red, green, blue;
            switch ((int)Math.Floor(h) % 6) {
                case 0:
                    red = c;
                    blue = x;
                    green = m;
                    break;
                case 1:
                    red = x;
                    blue = c;
                    green = m;
                    break;
                case 2:
                    red = m;
                    blue = c;
                    green = x;
                    break;
                case 3:
                    red = m;
                    blue = x;
                    green = c;
                    break;
                case 4:
                    red = x;
                    blue = m;
                    green = c;
                    break;
                case 5:
                    red = c;
                    blue = m;
                    green = x;
                    break;
                default:
                    red = 0;
                    blue = 0;
                    green = 0;
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
