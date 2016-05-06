namespace EventTraceKit.VsExtension
{
    /// <summary>
    /// This class is used to expose the list of the IDs of the commands implemented
    /// by the client package. This list of IDs must match the set of IDs defined inside the
    /// BUTTONS section of the CTC file.
    /// </summary>
    internal static class PkgCmdId
    {
        // Define the list a set of public static members.
        public const int cmdidPersistedWindow = 0x2001;
        public const int cmdidUiEventsWindow = 0x2002;
        public const int cmdidRefreshWindowsList = 0x2003;

        public const int TraceLogToolbar = 0x1000;

        public const int cmdidTraceLog = 0x0100;
        public const int cmdidAutoLog = 0x0200;
        public const int cmdidCaptureLog = 0x0300;
        public const int cmdidClearLog = 0x0400;
        public const int cmdidConfigureLog = 0x0500;

        // Define the list of menus (these include toolbars)
        public const int IDM_MyToolbar = 0x0101;
    }
}
