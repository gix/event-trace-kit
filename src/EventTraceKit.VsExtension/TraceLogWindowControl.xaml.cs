namespace EventTraceKit.VsExtension
{
    using System;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.VisualStudio.PlatformUI;

    /// <summary>Interaction logic for TraceLogWindowControl.</summary>
    public partial class TraceLogWindowControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TraceLogWindowControl"/> class.
        /// </summary>
        public TraceLogWindowControl()
        {
            InitializeComponent();
            UpdateThemeResources();
            VSColorTheme.ThemeChanged += OnThemeChanged;
        }

        private void OnThemeChanged(ThemeChangedEventArgs e)
        {
            UpdateThemeResources();
        }

        private void UpdateThemeResources()
        {
            var outputWindow = new Guid("9973EFDF-317D-431C-8BC1-5E88CBFD4F7F");
            var fch = new FontsAndColorsHelper();
            var values = new ResourceDictionary();

            if (fch.GetTextItemInfo(outputWindow, "Plain Text", values, true)) {
                Resources[Fonts.TraceLogEntryFontFamilyKey] = ((Typeface)values["Typeface"]).FontFamily;
                Resources[Fonts.TraceLogEntryFontSizeKey] = values["FontRenderingSize"];
                Resources[Colors.TraceLogForegroundKey] = values["Foreground"];
                Resources[Colors.TraceLogBackgroundKey] = values["Background"];
                Resources[Colors.TraceLogBackgroundAltKey] =
                    GetAlternateBrush((SolidColorBrush)values["Background"]);
            }

            if (fch.GetTextItemInfo(outputWindow, "Selected Text", values)) {
                Resources[Colors.TraceLogSelectedForegroundKey] = Resources[Colors.TraceLogForegroundKey];
                Resources[Colors.TraceLogSelectedBackgroundKey] = values["Background"];
            }

            if (fch.GetTextItemInfo(outputWindow, "Inactive Selected Text", values)) {
                Resources[Colors.TraceLogInactiveSelectedForegroundKey] = Resources[Colors.TraceLogForegroundKey];
                Resources[Colors.TraceLogInactiveSelectedBackgroundKey] = values["Background"];
            }
        }

        private SolidColorBrush GetAlternateBrush(
                SolidColorBrush brush, double amount = 0.05)
        {
            return new SolidColorBrush(GetAlternateColor(brush.Color, amount));
        }

        private Color GetAlternateColor(Color color, double amount = 0.05)
        {
            var hsl = color.ToHslColor();

            bool darken = hsl.Lightness >= 0.5;
            if (darken)
                hsl.Lightness -= amount;
            else
                hsl.Lightness += amount;

            return hsl.ToColor();
        }
    }
}
