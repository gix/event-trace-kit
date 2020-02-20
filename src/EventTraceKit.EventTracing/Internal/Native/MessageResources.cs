namespace EventTraceKit.EventTracing.Internal.Native
{
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct MESSAGE_RESOURCE_DATA
    {
        public uint NumberOfBlocks;
        //[MarshalAs(UnmanagedType.ByValArray, SizeParamIndex = 0)]
        public MESSAGE_RESOURCE_BLOCK[] Blocks;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MESSAGE_RESOURCE_BLOCK
    {
        public uint LowId;
        public uint HighId;
        public uint Offset;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MESSAGE_RESOURCE_ENTRY
    {
        public ushort Length;
        public ushort Flags;
        //[MarshalAs(UnmanagedType.ByValArray, SizeParamIndex = 0)]
        public string Text;
    }
}
