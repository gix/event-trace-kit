namespace EventTraceKit.Dev14
{
    using System;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.VisualStudio.PlatformUI;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    /// <summary>
    /// Interaction logic for ToolWindow1Control.
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

            var viewModel = new TraceLogWindowViewModel();
            viewModel.Status = "New";
            viewModel.Events.Add(new TraceEvent {
                Id = 1,
                Version = 2,
                ChannelId = 3,
                LevelId = 2,
                OpcodeId = 10,
                TaskId = 1000,
                KeywordMask = 0x8000000,
                Time = new DateTime(2000, 10, 11, 12, 13, 14),
                Message = "First event",
                Formatted = true
            });
            viewModel.Events.Add(new TraceEvent {
                Id = 2,
                Version = 2,
                ChannelId = 3,
                LevelId = 2,
                OpcodeId = 11,
                TaskId = 2000,
                KeywordMask = 0x8000000,
                Time = new DateTime(2000, 10, 11, 12, 13, 15),
                Message = "Second event",
                Formatted = true
            });
            viewModel.Events.Add(new TraceEvent {
                Id = 3,
                Version = 2,
                ChannelId = 3,
                LevelId = 2,
                OpcodeId = 12,
                TaskId = 3000,
                KeywordMask = 0x8000000,
                Time = new DateTime(2000, 10, 11, 12, 14, 14),
                Message = "Another event",
                Formatted = true
            });

            DataContext = viewModel;
        }

        private void OnThemeChanged(ThemeChangedEventArgs e)
        {
            UpdateThemeResources();
        }

        private void UpdateThemeResources()
        {
            var fcHelper = new FontsAndColorsHelper();

            var outputWindowCategory = new Guid("{9973efdf-317d-431c-8bc1-5e88cbfd4f7f}");
            var itemInfo = new ColorableItemInfo[1];
            var resources = new ResourceDictionary();

            fcHelper.GetPlainTextItemInfo(outputWindowCategory, resources, itemInfo);
            Resources[EtkColors.TraceLogForegroundKey] = resources["Foreground"];
            Resources[EtkFonts.TraceLogEntryFontFamilyKey] = ((Typeface)resources["Typeface"]).FontFamily;
            Resources[EtkFonts.TraceLogEntryFontSizeKey] = resources["FontRenderingSize"];

            fcHelper.GetTextViewBackgroundInfo(outputWindowCategory, resources, itemInfo);
            Resources[EtkColors.TraceLogBackgroundKey] = resources["Background"];

            if (fcHelper.GetBackgroundBrush(outputWindowCategory, "Selected Text", resources, itemInfo))
                Resources[EtkColors.TraceLogSelectedBackgroundKey] = resources["Background"];

            if (fcHelper.GetBackgroundBrush(outputWindowCategory, "Inactive Selected Text", resources, itemInfo)) {
                Resources[EtkColors.TraceLogSelectedBackgroundKey] = resources["Background"];
                Resources[EtkColors.TraceLogInactiveSelectedBackgroundKey] = resources["Background"];
            }
        }
    }
}
