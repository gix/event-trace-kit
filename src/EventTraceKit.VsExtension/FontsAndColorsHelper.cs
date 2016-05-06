namespace EventTraceKit.VsExtension
{
    using System;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Settings;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Shell.Settings;
    using Microsoft.VisualStudio.TextManager.Interop;

    internal class FontsAndColorsHelper
    {
        private static bool RoundFontSizesLoaded;
        private static bool RoundFontSizes;

        private readonly IVsFontAndColorStorage fcStorage;

        public FontsAndColorsHelper()
        {
            fcStorage = (IVsFontAndColorStorage)Package.GetGlobalService(typeof(SVsFontAndColorStorage));
        }

        internal static Microsoft.VisualStudio.OLE.Interop.IServiceProvider _globalServiceProvider;

        internal static Microsoft.VisualStudio.OLE.Interop.IServiceProvider GlobalServiceProvider
        {
            get
            {
                if (_globalServiceProvider == null) {
                    _globalServiceProvider = (Microsoft.VisualStudio.OLE.Interop.IServiceProvider)
                        Package.GetGlobalService(typeof(Microsoft.VisualStudio.OLE.Interop.IServiceProvider));
                }
                return _globalServiceProvider;
            }
            set
            {
                _globalServiceProvider = value;
            }
        }

        public static double FontSizeFromPointSize(double pointSize)
        {
            if (!RoundFontSizesLoaded) {
                RoundFontSizesLoaded = true;
                using (ServiceProvider provider = new ServiceProvider(GlobalServiceProvider)) {
                    RoundFontSizes = new ShellSettingsManager(provider).GetReadOnlySettingsStore(
                        SettingsScope.UserSettings).GetBoolean("Text Editor", "RoundFontSizes", true);
                }
            }
            double a = (pointSize * 96.0) / 72.0;
            if (!RoundFontSizes) {
                return a;
            }
            return Math.Round(a);
        }

        public static double FontSizeFromPointSize(int pointSize)
        {
            return FontSizeFromPointSize((double)pointSize);
        }

        internal static Color GetWpfColor(uint win32Foreground)
        {
            var color = System.Drawing.ColorTranslator.FromWin32((int)win32Foreground);
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        internal static Typeface GetTypefaceFromFont(string typefaceName)
        {
            FontStyle normal = FontStyles.Normal;
            FontStretch stretch = FontStretches.Normal;
            return new Typeface(
                new FontFamily(typefaceName), normal, FontWeights.Normal,
                stretch, GetFallbackFontFamily());
        }

        internal static FontFamily GetFallbackFontFamily()
        {
            return new FontFamily("Global Monospace, Global User Interface");
        }

        public bool GetPlainTextItemInfo(
            Guid guid, ResourceDictionary itemValue, ColorableItemInfo[] itemInfo)
        {
            if (fcStorage == null)
                return false;

            try {
                const uint Flags = (uint)(
                   __FCSTORAGEFLAGS.FCSF_READONLY |
                   __FCSTORAGEFLAGS.FCSF_NOAUTOCOLORS |
                   __FCSTORAGEFLAGS.FCSF_LOADDEFAULTS);
                if (ErrorHandler.Failed(fcStorage.OpenCategory(ref guid, Flags)))
                    return false;

                var pInfo = new[] { new FontInfo() };
                if (ErrorHandler.Failed(fcStorage.GetFont(null, pInfo)))
                    return false;

                if (pInfo[0].bFaceNameValid == 1) {
                    Typeface typefaceFromFont = GetTypefaceFromFont(pInfo[0].bstrFaceName);
                    itemValue["Typeface"] = typefaceFromFont;
                }

                if (pInfo[0].bPointSizeValid == 1)
                    itemValue["FontRenderingSize"] = FontSizeFromPointSize(pInfo[0].wPointSize);

                if (ErrorHandler.Failed(fcStorage.GetItem("Plain Text", itemInfo)))
                    return false;

                if ((itemInfo[0].dwFontFlags & (uint)FONTFLAGS.FF_BOLD) == (uint)FONTFLAGS.FF_BOLD)
                    itemValue["IsBold"] = true;
                else
                    itemValue["IsBold"] = false;

                Color color = GetWpfColor(itemInfo[0].crForeground);
                Brush brush = new SolidColorBrush(color);
                brush.Freeze();
                itemValue["ForegroundColor"] = color;
                itemValue["Foreground"] = brush;
                itemValue.Remove("BackgroundColor");
                itemValue.Remove("Background");
                return true;
            } finally {
                fcStorage.CloseCategory();
            }
        }

        public bool GetTextViewBackgroundInfo(
            Guid categoryGuid, ResourceDictionary itemValue, ColorableItemInfo[] itemInfo)
        {
            if (fcStorage == null)
                return false;

            try {
                fcStorage.OpenCategory(
                    ref categoryGuid, (int)__FCSTORAGEFLAGS.FCSF_LOADDEFAULTS);
                if (ErrorHandler.Failed(fcStorage.GetItem("Plain Text", itemInfo)))
                    return false;

                Color color = GetWpfColor(itemInfo[0].crBackground);
                Brush brush = new SolidColorBrush(color);
                brush.Freeze();
                itemValue["Background"] = brush;
                itemValue["BackgroundColor"] = color;
                return true;
            } finally {
                fcStorage.CloseCategory();
            }
        }

        public bool GetBackgroundBrush(
            Guid categoryGuid, string name, ResourceDictionary itemValue, ColorableItemInfo[] itemInfo)
        {
            if (fcStorage == null)
                return false;

            try {
                fcStorage.OpenCategory(
                    ref categoryGuid, (int)__FCSTORAGEFLAGS.FCSF_LOADDEFAULTS);
                if (ErrorHandler.Failed(fcStorage.GetItem(name, itemInfo)))
                    return false;

                Color color = GetWpfColor(itemInfo[0].crBackground);
                Brush brush = new SolidColorBrush(color);
                brush.Freeze();
                itemValue["Background"] = brush;
                itemValue["BackgroundColor"] = color;
                return true;
            } finally {
                fcStorage.CloseCategory();
            }
        }
    }
}
