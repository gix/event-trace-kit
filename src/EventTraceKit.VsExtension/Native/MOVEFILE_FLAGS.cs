namespace EventTraceKit.VsExtension.Native
{
    using System;

    [Flags]
    public enum MOVEFILE_FLAGS
    {
        NONE = 0x0,
        REPLACE_EXISTING = 0x1,
        COPY_ALLOWED = 0x2,
        DELAY_UNTIL_REBOOT = 0x4,
        WRITE_THROUGH = 0x8,
        CREATE_HARDLINK = 0x10,
        FAIL_IF_NOT_TRACKABLE = 0x20,
    }
}
