namespace EventTraceKit.VsExtension
{
    using System;

    /// <devdoc>Keep in sync with PkgCmd.vsct</devdoc>
    internal static class PkgCmdId
    {
        public static readonly Guid ViewCmdSet = new Guid("893F7D3D-AA53-4053-A0AC-F2B098E210A7");
        public const int cmdidTraceLog = 0x0100;

        public static readonly Guid TraceLogCmdSet = new Guid("46A772AB-D554-45B9-8DE2-EF68FCEF6732");
        public const int TraceLogToolbar = 0x1000;
        public const int cmdidAutoLog = 0x100;
        public const int cmdidCaptureLog = 0x200;
        public const int cmdidClearLog = 0x300;
        public const int cmdidAutoScroll = 0x350;
        public const int cmdidConfigureSession = 0x400;
        public const int cmdidOpenViewEditor = 0x500;
        public const int cmdidViewPresetCombo = 0x600;
        public const int cmdidViewPresetComboGetList = 0x610;
        public const int cmdidEnableFilter = 0x700;
        public const int cmdidOpenFilterEditor = 0x750;
        public const int cmdidToggleColumnHeaders = 0x800;
        public const int cmdidToggleStatusBar = 0x900;

        public static readonly Guid ProjectContextMenuCmdSet = new Guid("A9913707-D677-4EF7-BEA9-3865257F817E");
        public const int cmdidTraceSettings = 0x0100;
    }
}
