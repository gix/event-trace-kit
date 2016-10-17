namespace EventTraceKit.VsExtension
{
    using System;

    /// <devdoc>Keep in sync with PkgCmd.vsct</devdoc>
    internal static class PkgCmdId
    {
        public const int TraceLogToolbar = 0x1000;

        public static readonly Guid TraceLogCmdSet = new Guid("46A772AB-D554-45B9-8DE2-EF68FCEF6732");
        public const int cmdidTraceLog = 0x0100;
        public const int cmdidAutoLog = 0x0200;
        public const int cmdidCaptureLog = 0x0300;
        public const int cmdidClearLog = 0x0400;
        public const int cmdidConfigureSession = 0x0500;
        public const int cmdidOpenViewEditor = 0x0600;

        public static readonly Guid TraceLogPresetMenuCmdSet = new Guid("6C7C7E94-B649-4837-95D2-82850C3E441C");
        public const int cmdidPresetMenuDynamicStartCommand = 0x2000;

        public static readonly Guid ComboBoxCmdSet = new Guid("C7EE3D3B-D75A-4404-BE56-E8EEF4D19485");
        public const int cmdidMyDropDownCombo = 0x101;
        public const int cmdidMyDropDownComboGetList = 0x102;
    }
}
