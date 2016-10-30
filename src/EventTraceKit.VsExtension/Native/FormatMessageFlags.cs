namespace EventTraceKit.VsExtension.Native
{
    using System;

    [Flags]
    public enum FormatMessageFlags
    {
        /// <native>FORMAT_MESSAGE_IGNORE_INSERTS</native>
        IgnoreInserts = 0x00000200,

        /// <native>FORMAT_MESSAGE_FROM_STRING</native>
        FromString = 0x00000400,

        /// <native>FORMAT_MESSAGE_FROM_HMODULE</native>
        FromHmodule = 0x00000800,

        /// <native>FORMAT_MESSAGE_FROM_SYSTEM</native>
        FromSystem = 0x00001000,

        /// <native>FORMAT_MESSAGE_ARGUMENT_ARRAY</native>
        ArgumentArray = 0x00002000,

        /// <native>FORMAT_MESSAGE_MAX_WIDTH_MASK</native>
        MaxWidthMask = 0x000000FF,
    }
}