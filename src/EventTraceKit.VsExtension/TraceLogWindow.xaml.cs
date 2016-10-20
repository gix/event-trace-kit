namespace EventTraceKit.VsExtension
{
    using System;
    using System.Windows;
    using System.Windows.Media;
    using Windows;
    using Microsoft.VisualStudio.PlatformUI;
    using Styles;

    /// <summary>Interaction logic for TraceLogWindow.</summary>
    public partial class TraceLogWindow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TraceLogWindow"/> class.
        /// </summary>
        public TraceLogWindow()
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
                var background = (SolidColorBrush)values["Background"];
                Resources[TraceLogFonts.EntryFontFamilyKey] = ((Typeface)values["Typeface"]).FontFamily;
                Resources[TraceLogFonts.EntryFontSizeKey] = values["FontRenderingSize"];
                Resources[TraceLogColors.ForegroundKey] = values["Foreground"];
                Resources[TraceLogColors.BackgroundKey] = values["Background"];
                Resources[TraceLogColors.BackgroundAltKey] = GetAlternateBrush(background);
                Resources[TraceLogColors.FrozenColumnBackgroundKey] = GetAlternateBrush(background, 0.15);
            }

            if (fch.GetTextItemInfo(outputWindow, "Selected Text", values)) {
                Resources[TraceLogColors.SelectedForegroundKey] = Resources[TraceLogColors.ForegroundKey];
                Resources[TraceLogColors.SelectedBackgroundKey] = values["Background"];
            }

            if (fch.GetTextItemInfo(outputWindow, "Inactive Selected Text", values)) {
                Resources[TraceLogColors.InactiveSelectedForegroundKey] = Resources[TraceLogColors.ForegroundKey];
                Resources[TraceLogColors.InactiveSelectedBackgroundKey] = values["Background"];
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
