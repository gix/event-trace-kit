namespace EventTraceKit.VsExtension
{
    using System;
    using System.Windows;
    using System.Windows.Media;
    using Windows;
    using Microsoft.VisualStudio.PlatformUI;
    using Styles;

    public class TraceLogResourceDictionary : ResourceDictionary
    {
        public TraceLogResourceDictionary()
        {
            UpdateResources();
            VSColorTheme.ThemeChanged += OnThemeChanged;
        }

        private void OnThemeChanged(ThemeChangedEventArgs e)
        {
            UpdateResources();
        }

        private void UpdateResources()
        {
            var outputWindow = new Guid("9973EFDF-317D-431C-8BC1-5E88CBFD4F7F");
            var fch = new FontAndColorsHelper();
            var values = new ResourceDictionary();

            if (fch.GetTextItemInfo(outputWindow, "Plain Text", values, true)) {
                var background = (SolidColorBrush)values["Background"];
                this[TraceLogFonts.RowFontFamilyKey] = ((Typeface)values["Typeface"]).FontFamily;
                this[TraceLogFonts.RowFontSizeKey] = Math.Max((double)values["FontRenderingSize"] - 0.5, 10);
                this[TraceLogColors.RowForegroundBrushKey] = values["Foreground"];
                this[TraceLogColors.RowBackgroundBrushKey] = values["Background"];
                this[TraceLogColors.AlternatingRowBackgroundBrushKey] = GetAlternateBrush(background);
                this[TraceLogColors.FrozenColumnBackgroundBrushKey] = GetAlternateBrush(background, 0.15);
            }

            if (fch.GetTextItemInfo(outputWindow, "Selected Text", values)) {
                this[TraceLogColors.SelectedRowForegroundBrushKey] = this[TraceLogColors.RowForegroundBrushKey];
                this[TraceLogColors.SelectedRowBackgroundBrushKey] = values["Background"];
            }

            if (fch.GetTextItemInfo(outputWindow, "Inactive Selected Text", values)) {
                this[TraceLogColors.InactiveSelectedRowForegroundBrushKey] = this[TraceLogColors.RowForegroundBrushKey];
                this[TraceLogColors.InactiveSelectedRowBackgroundBrushKey] = values["Background"];
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
