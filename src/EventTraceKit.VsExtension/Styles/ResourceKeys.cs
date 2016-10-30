namespace EventTraceKit.VsExtension.Styles
{
    using System;

    public static class TraceLogColors
    {
        public static readonly Guid Category = TraceLogFontAndColorDefaults.CategoryId;

        private static FontAndColorsResourceKey rowForegroundBrushKey;
        private static FontAndColorsResourceKey rowBackgroundBrushKey;
        private static FontAndColorsResourceKey alternatingRowBackgroundBrushKey;
        private static FontAndColorsResourceKey selectedRowBackgroundBrushKey;
        private static FontAndColorsResourceKey selectedRowForegroundBrushKey;
        private static FontAndColorsResourceKey inactiveSelectedRowBackgroundBrushKey;
        private static FontAndColorsResourceKey inactiveSelectedForegroundKey;
        private static FontAndColorsResourceKey frozenColumnBackgroundBrushKey;

        public static FontAndColorsResourceKey RowForegroundBrushKey =>
            rowForegroundBrushKey ?? (rowForegroundBrushKey = new FontAndColorsResourceKey(
                Category, "Row", FontAndColorsResourceKeyType.ForegroundBrush));

        public static FontAndColorsResourceKey RowBackgroundBrushKey =>
            rowBackgroundBrushKey ?? (rowBackgroundBrushKey = new FontAndColorsResourceKey(
                Category, "Row", FontAndColorsResourceKeyType.BackgroundBrush));

        public static FontAndColorsResourceKey AlternatingRowBackgroundBrushKey =>
            alternatingRowBackgroundBrushKey ?? (alternatingRowBackgroundBrushKey = new FontAndColorsResourceKey(
                Category, "AlternatingRow", FontAndColorsResourceKeyType.BackgroundBrush));

        public static FontAndColorsResourceKey SelectedRowForegroundBrushKey =>
            selectedRowForegroundBrushKey ?? (selectedRowForegroundBrushKey = new FontAndColorsResourceKey(
                Category, "SelectedRow", FontAndColorsResourceKeyType.ForegroundBrush));

        public static object SelectedRowBackgroundBrushKey =>
            selectedRowBackgroundBrushKey ?? (selectedRowBackgroundBrushKey = new FontAndColorsResourceKey(
                Category, "SelectedRow", FontAndColorsResourceKeyType.BackgroundBrush));

        public static FontAndColorsResourceKey InactiveSelectedRowForegroundBrushKey =>
            inactiveSelectedForegroundKey ?? (inactiveSelectedForegroundKey = new FontAndColorsResourceKey(
                Category, "InactiveSelectedRow", FontAndColorsResourceKeyType.ForegroundBrush));

        public static FontAndColorsResourceKey InactiveSelectedRowBackgroundBrushKey =>
            inactiveSelectedRowBackgroundBrushKey ?? (inactiveSelectedRowBackgroundBrushKey = new FontAndColorsResourceKey(
                Category, "InactiveSelectedRow", FontAndColorsResourceKeyType.BackgroundBrush));

        public static FontAndColorsResourceKey FrozenColumnBackgroundBrushKey =>
            frozenColumnBackgroundBrushKey ?? (frozenColumnBackgroundBrushKey = new FontAndColorsResourceKey(
                Category, "FrozenColumn", FontAndColorsResourceKeyType.BackgroundBrush));
    }

    public static class TraceLogFonts
    {
        public static readonly Guid Category = TraceLogFontAndColorDefaults.CategoryId;

        private static FontAndColorsResourceKey rowFontFamilyKey;
        private static FontAndColorsResourceKey rowFontSizeKey;

        public static FontAndColorsResourceKey RowFontFamilyKey =>
            rowFontFamilyKey ?? (rowFontFamilyKey = new FontAndColorsResourceKey(
                Category, null, FontAndColorsResourceKeyType.FontFamily));

        public static FontAndColorsResourceKey RowFontSizeKey =>
            rowFontSizeKey ?? (rowFontSizeKey = new FontAndColorsResourceKey(
                Category, null, FontAndColorsResourceKeyType.FontSize));
    }
}
