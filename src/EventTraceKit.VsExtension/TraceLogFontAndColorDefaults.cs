namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Media;
    using Windows;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Styles;

    public class TraceLogFontAndColorDefaults
        : IVsFontAndColorDefaults, IVsFontAndColorEvents
    {
        private readonly ResourceSynchronizer synchronizer;
        private List<AllColorableItemInfo> items;

        public TraceLogFontAndColorDefaults(ResourceSynchronizer synchronizer)
        {
            this.synchronizer = synchronizer;
            //AddItems();
        }

        public static uint ToWin32Color(Color source)
        {
            double scale = source.A / 255.0;
            byte r = (byte)Math.Round(source.R * scale);
            byte g = (byte)Math.Round(source.G * scale);
            byte b = (byte)Math.Round(source.B * scale);
            return ((uint)b << 16) | ((uint)g << 8) | r;
        }

        public static uint ToWin32Color(Color source, Color dest)
        {
            if (source.A == 0xFF)
                return ToWin32Color(source);

            double srcA = (source.A / 255.0);
            double srcR = (source.R / 255.0) * srcA;
            double srcG = (source.G / 255.0) * srcA;
            double srcB = (source.B / 255.0) * srcA;

            double dstR = (dest.R / 255.0);
            double dstG = (dest.G / 255.0);
            double dstB = (dest.B / 255.0);

            double blendR = srcR + (dstR * (1 - srcA));
            double blendG = srcG + (dstG * (1 - srcA));
            double blendB = srcB + (dstB * (1 - srcA));

            byte r = (byte)Math.Round(blendR * 255);
            byte g = (byte)Math.Round(blendG * 255);
            byte b = (byte)Math.Round(blendB * 255);

            return ((uint)b << 16) | ((uint)g << 8) | r;
        }

        private void AddItems()
        {
            // Hardcoded in Visual Studio's text editor implementation.
            const double textEditBackgroundOpacity = 0.4;

            var fncUtils = (IVsFontAndColorUtilities)Package.GetGlobalService(typeof(SVsFontAndColorStorage));
            var fncHelper = new FontAndColorsHelper();

            var outputWindow = new Guid("9973EFDF-317D-431C-8BC1-5E88CBFD4F7F");

            uint rowForeground = 0;
            uint rowBackground = 0;
            uint alternatingRowBackground = 0;
            uint selectedForeground = 0;
            uint selectedBackground = 0;
            uint inactiveSelectedForeground = 0;
            uint inactiveSelectedBackground = 0;
            uint frozenColumnBackground = 0;

            if (fncHelper.GetTextItemInfo(outputWindow, "Plain Text", out var fg, out var bgRow)) {
                rowForeground = ToWin32Color(fg);
                rowBackground = ToWin32Color(bgRow);
                alternatingRowBackground = ToWin32Color(GetAlternateColor(bgRow), bgRow);
                frozenColumnBackground = ToWin32Color(GetAlternateColor(bgRow, 0.15), bgRow);
            }

            if (fncHelper.GetTextItemInfo(outputWindow, "Selected Text", out fg, out var bg)) {
                bg.A = (byte)(textEditBackgroundOpacity * 255);
                selectedForeground = ToWin32Color(fg);
                selectedBackground = ToWin32Color(bg, bgRow);
            }

            if (fncHelper.GetTextItemInfo(outputWindow, "Inactive Selected Text", out fg, out bg)) {
                bg.A = (byte)(textEditBackgroundOpacity * 255);
                inactiveSelectedForeground = ToWin32Color(fg);
                inactiveSelectedBackground = ToWin32Color(bg, bgRow);
            }

            ErrorHandler.ThrowOnFailure(fncUtils.EncodeAutomaticColor(out var autoColor));

            Items.Add(CreateItem(
                "Row", "Row", null,
                rowForeground, rowBackground, autoColor));

            Items.Add(CreateItem(
                "AlternatingRow", "Alternating Row", null,
                null, alternatingRowBackground, autoColor));

            Items.Add(CreateItem(
                "SelectedRow", "Selected Row", "Select Row Text",
                selectedForeground, selectedBackground, autoColor));

            Items.Add(CreateItem(
                "InactiveSelectedRow", "Inactive Selected Row", "Row text",
                inactiveSelectedForeground, inactiveSelectedBackground, autoColor));

            Items.Add(CreateItem(
                "FrozenColumn", "Frozen Column", null,
                null, frozenColumnBackground, autoColor));
        }

        private static Color GetAlternateColor(Color color, double amount = 0.05)
        {
            var hsl = color.ToHslColor();

            bool darken = hsl.Lightness >= 0.5;
            if (darken)
                hsl.Lightness -= amount;
            else
                hsl.Lightness += amount;

            return hsl.ToColor();
        }

        private AllColorableItemInfo CreateItem(
            string id, string name, string description, uint? foreColor, uint backColor, uint autoColor)
        {
            var flags = __FCITEMFLAGS.FCIF_ALLOWBGCHANGE |
                        __FCITEMFLAGS.FCIF_ALLOWCUSTOMCOLORS;

            var item = new AllColorableItemInfo {
                bNameValid = 1,
                bstrName = id,
                bLocalizedNameValid = 1,
                bstrLocalizedName = name,
                bAutoBackgroundValid = 1,
                crAutoBackground = backColor,
                bFlagsValid = 1,
                Info = {
                    bBackgroundValid = 1,
                    crBackground = autoColor,
                    bFontFlagsValid = 1,
                    dwFontFlags = 0
                },
            };

            if (foreColor != null) {
                item.bAutoForegroundValid = 1;
                item.crAutoForeground = foreColor.Value;
                item.Info.bForegroundValid = 1;
                item.Info.crForeground = autoColor;
                flags |= __FCITEMFLAGS.FCIF_ALLOWFGCHANGE;
            }

            if (description != null) {
                item.bDescriptionValid = 1;
                item.bstrDescription = description;
            }

            item.bFlagsValid = 1;
            item.fFlags = (uint)flags;

            return item;
        }

        public const string CategoryIdString = "93F54391-5474-4289-AABA-78CAB30CBCFC";
        public static Guid CategoryId { get; } = new Guid(CategoryIdString);

        private List<AllColorableItemInfo> Items
        {
            get
            {
                if (items == null)
                    items = new List<AllColorableItemInfo>();
                return items;
            }
        }

        #region IVsFontAndColorDefaults

        public int GetFlags(out uint dwFlags)
        {
            dwFlags = 0;
            return VSConstants.S_OK;
        }

        public int GetPriority(out ushort pPriority)
        {
            pPriority = 0;
            return VSConstants.S_OK;
        }

        public int GetCategoryName(out string pbstrName)
        {
            pbstrName = "Trace Log";
            return VSConstants.S_OK;
        }

        public int GetBaseCategory(out Guid pguidBase)
        {
            pguidBase = Guid.Empty;
            return VSConstants.S_OK;
        }

        public int GetFont(FontInfo[] pInfo)
        {
            if (pInfo == null)
                return VSConstants.E_INVALIDARG;

            var fontFamily = FontUtils.GetWPFDefaultFontFamily();
            var faceName = FontUtils.GetLocalizedFaceName(fontFamily);

            pInfo[0].bFaceNameValid = 1;
            pInfo[0].bstrFaceName = faceName;
            pInfo[0].bCharSetValid = 1;
            pInfo[0].iCharSet = 1;
            pInfo[0].bPointSizeValid = 1;
            pInfo[0].wPointSize = 9;
            return VSConstants.S_OK;
        }

        public int GetItemCount(out int pcItems)
        {
            pcItems = Items.Count;
            return VSConstants.S_OK;
        }

        public int GetItem(int iItem, AllColorableItemInfo[] pInfo)
        {
            pInfo[0] = Items[iItem];
            return VSConstants.S_OK;
        }

        public int GetItemByName(string szItem, AllColorableItemInfo[] pInfo)
        {
            foreach (var item in Items) {
                if (item.bstrName == szItem) {
                    pInfo[0] = item;
                    return VSConstants.S_OK;
                }
            }

            return VSConstants.E_FAIL;
        }

        #endregion

        #region IVsFontAndColorEvents

        public int OnFontChanged(
            ref Guid rguidCategory, FontInfo[] pInfo, LOGFONTW[] pLOGFONT, uint HFONT)
        {
            if (pInfo == null)
                return VSConstants.E_INVALIDARG;
            if (rguidCategory != CategoryId)
                return VSConstants.S_FALSE;

            if (pInfo[0].bFaceNameValid == 1) {
                //synchronizer[TraceLogFonts.RowFontFamilyKey] = new FontFamily(pInfo[0].bstrFaceName);
            }
            if (pInfo[0].bPointSizeValid == 1) {
                double fontSize = FontUtils.FontSizeFromPointSize(pInfo[0].wPointSize);
                //synchronizer[TraceLogFonts.RowFontSizeKey] = fontSize;
            }

            return VSConstants.S_OK;
        }

        public int OnItemChanged(
            ref Guid rguidCategory, string szItem, int iItem,
            ColorableItemInfo[] pInfo, uint crLiteralForeground,
            uint crLiteralBackground)
        {
            if (rguidCategory != CategoryId)
                return VSConstants.S_FALSE;
            return VSConstants.S_OK;
        }

        public int OnReset(ref Guid rguidCategory)
        {
            if (rguidCategory != CategoryId)
                return VSConstants.S_FALSE;
            return VSConstants.S_OK;
        }

        public int OnResetToBaseCategory(ref Guid rguidCategory)
        {
            if (rguidCategory != CategoryId)
                return VSConstants.S_FALSE;
            return VSConstants.S_OK;
        }

        public int OnApply()
        {
            synchronizer.UpdateValues();
            return VSConstants.S_OK;
        }

        #endregion
    }
}
