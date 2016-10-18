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
        public const int cmdidViewPresetCombo = 0x700;
        public const int cmdidViewPresetComboGetList = 0x710;
    }
}
