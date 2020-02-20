namespace EventTraceKit.EventTracing.Compilation.ResGen.Crimson
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct FileHeader
    {
        public uint Magic; // 'CRIM'
        public uint Length;
        public ushort Major;
        public ushort Minor;
        public uint NumProviders;
        //ProviderEntry Providers[];
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ProviderEntry
    {
        public Guid Guid;
        public uint Offset;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ProviderBlock
    {
        public uint Magic; // 'WEVT'
        public uint Length;
        public uint MessageId;
        public uint NumOffsets;
        //ProviderListOffset Offsets[11];
    }

    internal enum EventFieldKind : uint
    {
        Level = 0,
        Task = 1,
        Opcode = 2,
        Keyword = 3,
        Event = 4,
        Channel = 5,
        Maps = 6,
        Template = 7,
        NamedQueries = 8,
        Filter = 9,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ProviderListOffset
    {
        public EventFieldKind Type;
        public uint Offset;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ListBlock
    {
        public uint Magic;
        public uint Length;
        public uint NumEntries;
    }

    internal enum ChannelFlags : uint
    {
        None = 0,
        Imported = 1,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ChannelEntry
    {
        public ChannelFlags Flags; // (?)
        public uint NameOffset;
        public uint Value;
        public uint MessageId;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct OpcodeEntry
    {
        public ushort TaskId;
        public ushort Value;
        public uint MessageId;
        public uint NameOffset;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LevelEntry
    {
        public uint Value;
        public uint MessageId;
        public uint NameOffset;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TaskEntry
    {
        public uint Value;
        public uint MessageId;
        public Guid EventGuid;
        public uint NameOffset;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct KeywordEntry
    {
        public ulong Mask;
        public uint MessageId;
        public uint NameOffset;
    }

    internal enum MapFlags : uint
    {
        None = 0,
        Bitmap = 1,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MapEntry
    {
        public uint Magic; // 'VMAP', 'BMAP'
        public uint Length;
        public uint NameOffset;
        public MapFlags Flags; // (?)
        public uint NumItems;
        //MapItemEntry Items[];
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MapItemEntry
    {
        public uint Value;
        public uint MessageId;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PatternMapEntry
    {
        public uint Magic; // 'QUER'
        public uint Length;
        public uint NameOffset;
        public uint FormatOffset;
        public uint NumItems;
        //PatternMapItem Items[];
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PatternMapItem
    {
        public uint NameOffset;
        public uint ValueOffset;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TemplateEntry
    {
        public uint Magic; // 'TEMP'
        public uint Length;
        public uint NumParams;
        public uint NumProperties;
        public uint PropertyOffset;
        public uint Flags; // (?)
        public Guid TemplateId;
        //char BinXml[];
        //PropertyEntry Properties[];
    }

    [Flags]
    internal enum PropertyFlags : uint
    {
        Struct = 0x1,
        FixedLength = 0x2,
        VarLength = 0x4,
        FixedCount = 0x8,
        VarCount = 0x10,
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct PropertyEntry
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct NonStructType
        {
            public InTypeKind InputType;
            public OutTypeKind OutputType;
            public uint MapOffset;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct StructType
        {
            public ushort StructStartIndex;
            public ushort NumStructMembers;
        }

        [FieldOffset(0)]
        public PropertyFlags Flags;
        [FieldOffset(4)]
        public NonStructType nonStructType;
        [FieldOffset(4)]
        public StructType structType;
        [FieldOffset(12)]
        public ushort Count;
        [FieldOffset(14)]
        public ushort Length;
        [FieldOffset(16)]
        public uint NameOffset;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FilterBlock
    {
        public uint Magic; // 'FLTR'
        public uint Length;
        public uint NumFilters;
        public uint Junk;
        //FilterEntry Filter[];
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FilterEntry
    {
        public byte Value;
        public byte Version;
        public uint MessageId;
        public uint NameOffset;
        public uint TemplateOffset;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct EventBlock
    {
        public uint Magic; // 'EVNT'
        public uint Length;
        public uint NumEvents;
        public uint Unknown;
        //EventEntry Events[];
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct EventDescriptor
    {
        public ushort Id;
        public byte Version;
        public byte Channel;
        public byte Level;
        public byte Opcode;
        public ushort Task;
        public ulong Keyword;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct EventEntry
    {
        public EventDescriptor Descriptor;
        public uint MessageId;
        public uint TemplateOffset;
        public uint OpcodeOffset;
        public uint LevelOffset;
        public uint TaskOffset;
        public uint NumKeywords;
        public uint KeywordsOffset;
        public uint ChannelOffset;
    }

    internal enum InTypeKind : byte
    {
        Unknown = 0,
        UnicodeString = 1,
        AnsiString = 2,
        Int8 = 3,
        UInt8 = 4,
        Int16 = 5,
        UInt16 = 6,
        Int32 = 7,
        UInt32 = 8,
        Int64 = 9,
        UInt64 = 10,
        Float = 11,
        Double = 12,
        Boolean = 13,
        Binary = 14,
        GUID = 15,
        Pointer = 16,
        FILETIME = 17,
        SYSTEMTIME = 18,
        SID = 19,
        HexInt32 = 20,
        HexInt64 = 21,
    }

    internal enum OutTypeKind : byte
    {
        Unknown = 0,
        String = 1,
        DateTime = 2,
        Byte = 3,
        UnsignedByte = 4,
        Short = 5,
        UnsignedShort = 6,
        Int = 7,
        UnsignedInt = 8,
        Long = 9,
        UnsignedLong = 10,
        Float = 11,
        Double = 12,
        Boolean = 13,
        GUID = 14,
        HexBinary = 15,
        HexInt8 = 16,
        HexInt16 = 17,
        HexInt32 = 18,
        HexInt64 = 19,
        PID = 20,
        TID = 21,
        Port = 22,
        IPv4 = 23,
        IPv6 = 24,
        SocketAddress = 25,
        CIMDateTime = 26,
        ETWTIME = 27,
        Xml = 28,
        ErrorCode = 29,
        Win32Error = 30,
        NTSTATUS = 31,
        HResult = 32,
        DateTimeCultureInsensitive = 33,
    }
}
