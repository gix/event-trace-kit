namespace EventTraceKit.VsExtension
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Media;
    using Native;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Settings;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Shell.Settings;
    using Microsoft.VisualStudio.TextManager.Interop;
    using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

    internal class FontsAndColorsHelper
    {
        private const uint OpenFlags = (uint)(
            __FCSTORAGEFLAGS.FCSF_READONLY |
            //__FCSTORAGEFLAGS.FCSF_NOAUTOCOLORS |
            __FCSTORAGEFLAGS.FCSF_LOADDEFAULTS);

        private static IServiceProvider GlobalServiceProviderCache;

        private static bool RoundFontSizesLoaded;
        private static bool RoundFontSizes;

        private readonly IVsFontAndColorStorage fcStorage;
        private readonly IVsFontAndColorUtilities fcUtilities;

        public FontsAndColorsHelper()
        {
            fcStorage = (IVsFontAndColorStorage)Package.GetGlobalService(typeof(SVsFontAndColorStorage));
            fcUtilities = (IVsFontAndColorUtilities)Package.GetGlobalService(typeof(SVsFontAndColorStorage));
        }

        internal static IServiceProvider GlobalServiceProvider =>
            GlobalServiceProviderCache ??
            (GlobalServiceProviderCache = (IServiceProvider)Package.GetGlobalService(
                typeof(IServiceProvider)));

        public static double FontSizeFromPointSize(double pointSize)
        {
            if (!RoundFontSizesLoaded) {
                RoundFontSizesLoaded = true;
                using (var provider = new ServiceProvider(GlobalServiceProvider)) {
                    var settingsManager = new ShellSettingsManager(provider);
                    var userSettings = settingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings);
                    RoundFontSizes = userSettings.GetBoolean("Text Editor", "RoundFontSizes", true);
                }
            }

            double size = (pointSize * 96.0) / 72.0;
            if (RoundFontSizes)
                return Math.Round(size);
            return size;
        }

        public static double FontSizeFromPointSize(int pointSize)
        {
            return FontSizeFromPointSize((double)pointSize);
        }

        public bool GetTextItemInfo(
            Guid category, string name, ResourceDictionary values,
            bool includeFontInfo = false, double? backgroundOpacity = 0.4)
        {
            if (fcStorage == null)
                return false;

            if (ErrorHandler.Failed(fcStorage.OpenCategory(category, OpenFlags)))
                return false;

            try {
                var itemInfo = new ColorableItemInfo[1];
                if (ErrorHandler.Failed(fcStorage.GetItem(name, itemInfo)))
                    return false;

                if (includeFontInfo) {
                    var fontInfo = new FontInfo[1];
                    if (ErrorHandler.Failed(fcStorage.GetFont(null, fontInfo)))
                        return false;

                    if (fontInfo[0].bFaceNameValid == 1)
                        values["Typeface"] = GetTypefaceFromFont(fontInfo[0].bstrFaceName);

                    if (fontInfo[0].bPointSizeValid == 1)
                        values["FontRenderingSize"] = FontSizeFromPointSize(fontInfo[0].wPointSize);

                    if ((itemInfo[0].dwFontFlags & (uint)FONTFLAGS.FF_BOLD) != 0)
                        values["IsBold"] = true;
                    else
                        values["IsBold"] = false;
                }

                DecodePlainTextColors(ref itemInfo[0]);

                {
                    Color color = GetWpfColor(itemInfo[0].crForeground);
                    Brush brush = new SolidColorBrush(color);
                    brush.Freeze();
                    values["Foreground"] = brush;
                }

                {
                    Color color = GetWpfColor(itemInfo[0].crBackground);
                    if (backgroundOpacity != null)
                        color.A = (byte)(255 * backgroundOpacity.Value);
                    Brush brush = new SolidColorBrush(color);
                    brush.Freeze();
                    values["Background"] = brush;
                }

                return true;
            } finally {
                fcStorage.CloseCategory();
            }
        }

        private void DecodePlainTextColors(ref ColorableItemInfo info)
        {
            if (info.bForegroundValid == 1)
                DecodePlainTextColor(ref info.crForeground, COLORINDEX.CI_SYSPLAINTEXT_FG);
            if (info.bBackgroundValid == 1)
                DecodePlainTextColor(ref info.crBackground, COLORINDEX.CI_SYSPLAINTEXT_BK);
        }

        private void DecodePlainTextColor(ref uint color, COLORINDEX automaticIndex)
        {
            int type;
            Marshal.ThrowExceptionForHR(fcUtilities.GetColorType(color, out type));
            switch ((__VSCOLORTYPE)type) {
                case __VSCOLORTYPE.CT_COLORINDEX:
                    color = DecodeColorIndex(color);
                    break;
                case __VSCOLORTYPE.CT_SYSCOLOR:
                    color = DecodeSystemColor(color);
                    break;
                case __VSCOLORTYPE.CT_AUTOMATIC:
                    Marshal.ThrowExceptionForHR(fcUtilities.GetRGBOfIndex(automaticIndex, out color));
                    break;
            }
        }

        private uint DecodeColorIndex(uint index)
        {
            uint color;
            var indices = new COLORINDEX[1];
            Marshal.ThrowExceptionForHR(fcUtilities.GetEncodedIndex(index, indices));
            Marshal.ThrowExceptionForHR(fcUtilities.GetRGBOfIndex(indices[0], out color));
            return color;
        }

        private uint DecodeSystemColor(uint systemColorReference)
        {
            int index;
            ErrorHandler.ThrowOnFailure(fcUtilities.GetEncodedSysColor(systemColorReference, out index));
            return NativeMethods.GetSysColor(index);
        }

        private static Color GetWpfColor(uint win32Color)
        {
            var color = System.Drawing.ColorTranslator.FromWin32((int)win32Color);
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        private static Typeface GetTypefaceFromFont(string typefaceName)
        {
            FontStyle normal = FontStyles.Normal;
            FontStretch stretch = FontStretches.Normal;
            return new Typeface(
                new FontFamily(typefaceName), normal, FontWeights.Normal,
                stretch, GetFallbackFontFamily());
        }

        private static FontFamily GetFallbackFontFamily()
        {
            return new FontFamily("Global Monospace, Global User Interface");
        }
    }
}
