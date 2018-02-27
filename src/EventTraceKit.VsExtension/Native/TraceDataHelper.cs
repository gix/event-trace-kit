namespace EventTraceKit.VsExtension.Native
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    public struct TRACE_EVENT_INFO
    {
        public Guid ProviderGuid;
        public Guid EventGuid;
        public EVENT_DESCRIPTOR EventDescriptor;
        public DecodingSource DecodingSource;
        public uint ProviderNameOffset;
        public uint LevelNameOffset;
        public uint ChannelNameOffset;
        public uint KeywordsNameOffset;
        public uint TaskNameOffset;
        public uint OpcodeNameOffset;
        public uint EventMessageOffset;
        public uint ProviderMessageOffset;
        public uint BinaryXMLOffset;
        public uint BinaryXMLSize;
        public uint ActivityIDNameOffset;
        public uint RelatedActivityIDNameOffset;
        public uint PropertyCount;
        public uint TopLevelPropertyCount;
        public TEMPLATE_FLAGS Flags;
        public EVENT_PROPERTY_INFO EventPropertyInfoArray;

        public static unsafe EVENT_PROPERTY_INFO* GetEventPropertyInfoArray(
            TRACE_EVENT_INFO* pTraceEventInfo)
        {
            return &pTraceEventInfo->EventPropertyInfoArray;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EVENT_PROPERTY_INFO
    {
        public PROPERTY_FLAGS Flags;
        public uint NameOffset;
        private Union union;
        public ushort countAndCountPropertyIndex;
        public ushort lengthAndLengthPropertyIndex;
        public uint Reserved;

        public TDH_IN_TYPE InType
        {
            get { return (TDH_IN_TYPE)union.field0UInt16; }
            set { union.field0UInt16 = (ushort)value; }
        }

        public TDH_OUT_TYPE OutType
        {
            get { return (TDH_OUT_TYPE)union.field1UInt16; }
            set { union.field1UInt16 = (ushort)value; }
        }

        public uint MapNameOffset
        {
            get { return union.field2UInt32; }
            set { union.field2UInt32 = value; }
        }

        public ushort StructStartIndex => union.field0UInt16;
        public ushort NumOfStructMembers => union.field1UInt16;
        public uint padding => union.field2UInt32;

        public ushort count => countAndCountPropertyIndex;
        public ushort countPropertyIndex => countAndCountPropertyIndex;
        public ushort length => lengthAndLengthPropertyIndex;
        public ushort lengthPropertyIndex => lengthAndLengthPropertyIndex;

        [StructLayout(LayoutKind.Explicit)]
        private struct Union
        {
            [FieldOffset(0)]
            public ushort field0UInt16;

            [FieldOffset(2)]
            public ushort field1UInt16;

            [FieldOffset(4)]
            public uint field2UInt32;
        }
    }

    public enum DecodingSource
    {
        Manifest,
        WBEM,
        WPP
    }

    [Flags]
    public enum TEMPLATE_FLAGS
    {
        TEMPLATE_EVENT_DATA = 1,
        TEMPLATE_USER_DATA = 2
    }

    [Flags]
    public enum PROPERTY_FLAGS
    {
        PropertyParamCount = 4,
        PropertyParamFixedLength = 0x10,
        PropertyParamLength = 2,
        PropertyStruct = 1,
        PropertyWBEMXmlFragment = 8
    }

    public enum TDH_IN_TYPE : ushort
    {
        ANSICHAR = 0x133,
        ANSISTRING = 2,
        BINARY = 14,
        BOOLEAN = 13,
        COUNTEDANSISTRING = 0x12d,
        COUNTEDSTRING = 300,
        DOUBLE = 12,
        FILETIME = 0x11,
        FLOAT = 11,
        GUID = 15,
        HEXDUMP = 0x135,
        HEXINT32 = 20,
        HEXINT64 = 0x15,
        INT16 = 5,
        INT32 = 7,
        INT64 = 9,
        INT8 = 3,
        NONNULLTERMINATEDANSISTRING = 0x131,
        NONNULLTERMINATEDSTRING = 0x130,
        NULL = 0,
        POINTER = 0x10,
        REVERSEDCOUNTEDANSISTRING = 0x12f,
        REVERSEDCOUNTEDSTRING = 0x12e,
        SID = 0x13,
        SIZET = 0x134,
        SYSTEMTIME = 0x12,
        UINT16 = 6,
        UINT32 = 8,
        UINT64 = 10,
        UINT8 = 4,
        UNICODECHAR = 0x132,
        UNICODESTRING = 1,
        WBEMSID = 310
    }

    public enum TDH_OUT_TYPE
    {
        BOOLEAN = 13,
        BYTE = 3,
        CIMDATETIME = 0x1a,
        CULTURE_INSENSITIVE_DATETIME = 0x21,
        DATETIME = 2,
        DOUBLE = 12,
        ERRORCODE = 0x1d,
        ETWTIME = 0x1b,
        FLOAT = 11,
        GUID = 14,
        HEXBINARY = 15,
        HEXINT16 = 0x11,
        HEXINT32 = 0x12,
        HEXINT64 = 0x13,
        HEXINT8 = 0x10,
        HRESULT = 0x20,
        INT = 7,
        IPV4 = 0x17,
        IPV6 = 0x18,
        LONG = 9,
        NOPRINT = 0x12d,
        NTSTATUS = 0x1f,
        NULL = 0,
        PID = 20,
        PORT = 0x16,
        REDUCEDSTRING = 300,
        SHORT = 5,
        SOCKETADDRESS = 0x19,
        STRING = 1,
        TID = 0x15,
        UNSIGNEDBYTE = 4,
        UNSIGNEDINT = 8,
        UNSIGNEDLONG = 10,
        UNSIGNEDSHORT = 6,
        WIN32ERROR = 30,
        XML = 0x1c
    }
}
