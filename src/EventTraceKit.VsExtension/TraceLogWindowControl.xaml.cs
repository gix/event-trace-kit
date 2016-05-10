namespace EventTraceKit.VsExtension
{
    using System;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.VisualStudio.PlatformUI;
    using Microsoft.VisualStudio.Shell.Interop;

    /// <summary>
    /// Interaction logic for TraceLogWindowControl.
    /// </summary>
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
            var fcHelper = new FontsAndColorsHelper();

            var outputWindowCategory = new Guid("9973EFDF-317D-431C-8BC1-5E88CBFD4F7F");
            var itemInfo = new ColorableItemInfo[1];
            var resources = new ResourceDictionary();

            if (fcHelper.GetPlainTextItemInfo(outputWindowCategory, resources, itemInfo)) {
                Resources[EtkColors.TraceLogForegroundKey] = resources["Foreground"];
                Resources[EtkFonts.TraceLogEntryFontFamilyKey] = ((Typeface)resources["Typeface"]).FontFamily;
                Resources[EtkFonts.TraceLogEntryFontSizeKey] = resources["FontRenderingSize"];
            }

            if (fcHelper.GetTextViewBackgroundInfo(outputWindowCategory, resources, itemInfo))
                Resources[EtkColors.TraceLogBackgroundKey] = resources["Background"];

            if (fcHelper.GetBackgroundBrush(outputWindowCategory, "Selected Text", resources, itemInfo))
                Resources[EtkColors.TraceLogSelectedBackgroundKey] = resources["Background"];

            if (fcHelper.GetBackgroundBrush(outputWindowCategory, "Inactive Selected Text", resources, itemInfo))
                Resources[EtkColors.TraceLogInactiveSelectedBackgroundKey] = resources["Background"];
        }
    }
}
