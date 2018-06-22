namespace EventTraceKit.VsExtension.Resources.Styles
{
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.PlatformUI;
    using Microsoft.VisualStudio.Shell.Interop;

    public class FontAndColorsResourceDictionary : ResourceDictionary
    {
        private readonly IVsFontAndColorStorage fncStorage;

        public FontAndColorsResourceDictionary(IVsFontAndColorStorage fncStorage)
        {
            this.fncStorage = fncStorage;
        }

        protected override void OnGettingValue(object key, ref object value, out bool canCache)
        {
            if (!(value is FontAndColorsResourceKey resourceKey)) {
                base.OnGettingValue(key, ref value, out canCache);
                return;
            }

            if (key is FontAndColorsResourceKey)
                value = RealizeValue(resourceKey);
            else
                value = base[resourceKey];

            canCache = true;
        }

        private FontFamily GetFontFamily(FontAndColorsResourceKey key)
        {
            const __FCSTORAGEFLAGS flags = __FCSTORAGEFLAGS.FCSF_LOADDEFAULTS |
                                           __FCSTORAGEFLAGS.FCSF_NOAUTOCOLORS;

            fncStorage.OpenCategory(key.Category, (uint)flags);
            try {
                var fontInfos = new FontInfo[1];
                if (fncStorage.GetFont(null, fontInfos) != VSConstants.S_OK)
                    return null;

                if (fontInfos[0].bFaceNameValid == 1)
                    return new FontFamily(fontInfos[0].bstrFaceName);

                return null;
            } finally {
                fncStorage.CloseCategory();
            }
        }

        private double GetFontSize(FontAndColorsResourceKey key)
        {
            const __FCSTORAGEFLAGS flags = __FCSTORAGEFLAGS.FCSF_LOADDEFAULTS |
                                           __FCSTORAGEFLAGS.FCSF_NOAUTOCOLORS;
            const double defaultFontSize = 9;

            fncStorage.OpenCategory(key.Category, (uint)flags);
            try {
                var fontInfos = new FontInfo[1];
                if (fncStorage.GetFont(null, fontInfos) != VSConstants.S_OK)
                    return defaultFontSize;

                if (fontInfos[0].bPointSizeValid == 1)
                    return FontUtils.FontSizeFromPointSize(fontInfos[0].wPointSize);

                return defaultFontSize;
            } finally {
                fncStorage.CloseCategory();
            }
        }

        private uint GetRgbaColorValue(FontAndColorsResourceKey key)
        {
            const __FCSTORAGEFLAGS flags = __FCSTORAGEFLAGS.FCSF_LOADDEFAULTS |
                                           __FCSTORAGEFLAGS.FCSF_NOAUTOCOLORS;

            fncStorage.OpenCategory(key.Category, (uint)flags);
            try {
                var itemInfos = new ColorableItemInfo[1];
                if (fncStorage.GetItem(key.Name, itemInfos) != VSConstants.S_OK)
                    return 0xFF000000;

                uint color;
                switch (key.KeyType) {
                    case FontAndColorsResourceKeyType.ForegroundColor:
                    case FontAndColorsResourceKeyType.ForegroundBrush:
                        color = itemInfos[0].crForeground;
                        break;

                    case FontAndColorsResourceKeyType.BackgroundColor:
                    case FontAndColorsResourceKeyType.BackgroundBrush:
                        color = itemInfos[0].crBackground;
                        break;

                    default:
                        color = 0;
                        break;
                }

                color |= 0xFF000000;
                return color;
            } finally {
                fncStorage.CloseCategory();
            }
        }

        private static bool IsBrushKeyType(FontAndColorsResourceKeyType keyType)
        {
            return
                keyType == FontAndColorsResourceKeyType.BackgroundBrush ||
                keyType == FontAndColorsResourceKeyType.ForegroundBrush;
        }

        private object RealizeValue(FontAndColorsResourceKey key)
        {
            if (key.KeyType == FontAndColorsResourceKeyType.FontFamily)
                return GetFontFamily(key);
            if (key.KeyType == FontAndColorsResourceKeyType.FontSize)
                return GetFontSize(key);

            Color color = GetRgbaColorValue(key).ToColorFromRgba();
            if (!IsBrushKeyType(key.KeyType))
                return color;

            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
    }
}
