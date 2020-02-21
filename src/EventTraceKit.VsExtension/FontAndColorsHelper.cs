namespace EventTraceKit.VsExtension
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Markup;
    using System.Windows.Media;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Settings;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Shell.Settings;
    using Microsoft.VisualStudio.TextManager.Interop;
    using Native;
    using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

    public static class FontUtils
    {
        private static bool roundFontSizesLoaded;
        private static bool roundFontSizes;

        public static Typeface GetTypefaceFromFont(string typefaceName)
        {
            return new Typeface(
                new FontFamily(typefaceName), FontStyles.Normal, FontWeights.Normal,
                FontStretches.Normal, GetFallbackFontFamily());
        }

        public static double FontSizeFromPointSize(int pointSize)
        {
            return FontSizeFromPointSize((double)pointSize);
        }

        public static double FontSizeFromPointSize(double pointSize)
        {
            if (!roundFontSizesLoaded) {
                roundFontSizes = GetRoundFontSizesSetting();
                roundFontSizesLoaded = true;
            }

            double size = (pointSize * 96.0) / 72.0;
            if (roundFontSizes)
                size = Math.Round(size);

            return size;
        }

        private static bool GetRoundFontSizesSetting()
        {
            var globalServiceProvider = (IServiceProvider)Package.GetGlobalService(typeof(IServiceProvider));
            using var provider = new ServiceProvider(globalServiceProvider);
            var settingsManager = new ShellSettingsManager(provider);
            var userSettings = settingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings);
            return userSettings.GetBoolean("Text Editor", "RoundFontSizes", true);
        }

        public static FontFamily GetFallbackFontFamily()
        {
            return new FontFamily("Global Monospace, Global User Interface");
        }

        public static FontFamily GetWPFDefaultFontFamily()
        {
            var name = CultureInfo.CurrentUICulture.ThreeLetterWindowsLanguageName switch
            {
                "ENU" => "Consolas",
                "JPN" => "MS Gothic",
                "KOR" => "DotumChe",
                "CHS" => "NSimSun",
                "CHT" => "MingLiU",
                _ => "Consolas",
            };

            if (NativeMethods.IsSystemFontAvailable(name))
                return new FontFamily(name);

            return new FontFamily("Courier New");
        }

        public static string GetLocalizedFaceName(FontFamily family)
        {
            var currLang = XmlLanguage.GetLanguage(CultureInfo.CurrentUICulture.Name);
            var fallbackLang = XmlLanguage.GetLanguage("en-us");

            if (family.FamilyNames.ContainsKey(currLang))
                return family.FamilyNames[currLang];

            if (family.FamilyNames.ContainsKey(fallbackLang))
                return family.FamilyNames[fallbackLang];

            if (family.FamilyNames.Count > 0)
                return family.FamilyNames.First().Value;

            return null;
        }
    }

    internal class FontAndColorsHelper
    {
        private const uint OpenFlags = (uint)(
            __FCSTORAGEFLAGS.FCSF_READONLY |
            __FCSTORAGEFLAGS.FCSF_LOADDEFAULTS);

        private readonly IVsFontAndColorStorage fncStorage;
        private readonly IVsFontAndColorUtilities fncUtils;

        public FontAndColorsHelper()
        {
            fncStorage = (IVsFontAndColorStorage)Package.GetGlobalService(typeof(SVsFontAndColorStorage));
            fncUtils = (IVsFontAndColorUtilities)Package.GetGlobalService(typeof(SVsFontAndColorStorage));
        }

        public bool GetTextItemInfo(
            Guid category, string name, out Color foreground, out Color background)
        {
            foreground = Colors.Transparent;
            background = Colors.Transparent;

            if (fncStorage == null)
                return false;

            if (ErrorHandler.Failed(fncStorage.OpenCategory(category, OpenFlags)))
                return false;

            try {
                var itemInfo = new ColorableItemInfo[1];
                if (ErrorHandler.Failed(fncStorage.GetItem(name, itemInfo)))
                    return false;

                DecodePlainTextColors(ref itemInfo[0]);

                foreground = GetWpfColor(itemInfo[0].crForeground);
                background = GetWpfColor(itemInfo[0].crBackground);

                return true;
            } finally {
                fncStorage.CloseCategory();
            }
        }

        public bool GetTextItemInfo(
            Guid category, string name, ResourceDictionary values,
            bool includeFontInfo = false, double? backgroundOpacity = 0.4)
        {
            if (fncStorage == null)
                return false;

            if (ErrorHandler.Failed(fncStorage.OpenCategory(category, OpenFlags)))
                return false;

            try {
                var itemInfo = new ColorableItemInfo[1];
                if (ErrorHandler.Failed(fncStorage.GetItem(name, itemInfo)))
                    return false;

                if (includeFontInfo) {
                    var fontInfo = new FontInfo[1];
                    if (ErrorHandler.Failed(fncStorage.GetFont(null, fontInfo)))
                        return false;

                    if (fontInfo[0].bFaceNameValid == 1)
                        values["Typeface"] = FontUtils.GetTypefaceFromFont(fontInfo[0].bstrFaceName);

                    if (fontInfo[0].bPointSizeValid == 1)
                        values["FontRenderingSize"] = FontUtils.FontSizeFromPointSize(fontInfo[0].wPointSize);

                    values["IsBold"] = (itemInfo[0].dwFontFlags & (uint)FONTFLAGS.FF_BOLD) != 0;
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
                fncStorage.CloseCategory();
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
            Marshal.ThrowExceptionForHR(fncUtils.GetColorType(color, out var type));
            switch ((__VSCOLORTYPE)type) {
                case __VSCOLORTYPE.CT_COLORINDEX:
                    color = DecodeColorIndex(color);
                    break;
                case __VSCOLORTYPE.CT_SYSCOLOR:
                    color = DecodeSystemColor(color);
                    break;
                case __VSCOLORTYPE.CT_AUTOMATIC:
                    Marshal.ThrowExceptionForHR(fncUtils.GetRGBOfIndex(automaticIndex, out color));
                    break;
            }
        }

        private uint DecodeColorIndex(uint index)
        {
            var indices = new COLORINDEX[1];
            Marshal.ThrowExceptionForHR(fncUtils.GetEncodedIndex(index, indices));
            Marshal.ThrowExceptionForHR(fncUtils.GetRGBOfIndex(indices[0], out var color));
            return color;
        }

        private uint DecodeSystemColor(uint systemColorReference)
        {
            ErrorHandler.ThrowOnFailure(fncUtils.GetEncodedSysColor(systemColorReference, out var index));
            return NativeMethods.GetSysColor(index);
        }

        public static Color GetWpfColor(uint win32Color)
        {
            var color = System.Drawing.ColorTranslator.FromWin32((int)win32Color);
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }
    }
}
