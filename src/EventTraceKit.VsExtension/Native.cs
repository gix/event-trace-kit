namespace EventTraceKit.VsExtension
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security.Principal;

    [StructLayout(LayoutKind.Sequential)]
    public struct EVENT_DESCRIPTOR : IEquatable<EVENT_DESCRIPTOR>
    {
        public readonly ushort Id;
        public readonly byte Version;
        public readonly byte Channel;
        public readonly byte Level;
        public readonly byte Opcode;
        public readonly ushort Task;
        public readonly ulong Keyword;

        public bool Equals(EVENT_DESCRIPTOR other)
        {
            return
                Id == other.Id &&
                Version == other.Version &&
                Channel == other.Channel &&
                Level == other.Level &&
                Opcode == other.Opcode &&
                Task == other.Task &&
                Keyword == other.Keyword;
        }

        public override bool Equals(object other)
        {
            return other is EVENT_DESCRIPTOR && Equals((EVENT_DESCRIPTOR)other);
        }

        public override int GetHashCode()
        {
            return 0;//FIXME
            //return HashCode.Combine(
            //    Id.GetHashCode(),
            //    Version.GetHashCode(),
            //    Channel.GetHashCode(),
            //    Level.GetHashCode(),
            //    Opcode.GetHashCode(),
            //    Task.GetHashCode(),
            //    Keyword.GetHashCode());
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EVENT_HEADER
    {
        public readonly ushort Size;
        public readonly ushort HeaderType;
        public readonly ushort Flags;
        public readonly ushort EventProperty;
        public readonly uint ThreadId;
        public readonly uint ProcessId;
        public readonly TimePoint TimeStamp;
        public readonly Guid ProviderId;
        public readonly EVENT_DESCRIPTOR EventDescriptor;
        public readonly TimeUnionStruct TimeUnion;
        public readonly Guid ActivityId;

        public bool HasExtendedInfo => HasFlag(0x01);
        public bool IsPrivateSession => HasFlag(0x02);
        public bool IsStringOnly => HasFlag(0x04);
        public bool IsTraceMessage => HasFlag(0x08);
        public bool NoCpuTime => HasFlag(0x10);
        public bool HasFlag32BitHeader => HasFlag(0x20);
        public bool HasFlag64BitHeader => HasFlag(0x40);
        public bool ClassicHeader => HasFlag(0x100);
        public bool HasFlagProcessorIndex => HasFlag(0x200);

        public uint KernelTime => TimeUnion.KernelTime;
        public uint UserTime => TimeUnion.UserTime;
        public ulong ProcessorTime => TimeUnion.ProcessorTime;

        private bool HasFlag(int flag)
        {
            return (Flags & flag) != 0;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct TimeUnionStruct
        {
            [FieldOffset(0)]
            public readonly uint KernelTime;

            [FieldOffset(0)]
            public readonly ulong ProcessorTime;

            [FieldOffset(4)]
            public readonly uint UserTime;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ETW_BUFFER_CONTEXT
    {
        public readonly ushort ProcessorIndex;
        public readonly ushort LoggerId;
        public byte ProcessorNumber => (byte)(ProcessorIndex & 0xFF);
        public byte Alignment => (byte)(ProcessorIndex >> 8);
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct EVENT_RECORD
    {
        public EVENT_HEADER EventHeader;
        public ETW_BUFFER_CONTEXT BufferContext;
        public ushort ExtendedDataCount;
        public ushort UserDataLength;
        public EVENT_HEADER_EXTENDED_DATA_ITEM* ExtendedData;
        public IntPtr UserData;
        public IntPtr UserContext;

        public ulong ProcessorIndex =>
            EventHeader.HasFlagProcessorIndex ?
                BufferContext.ProcessorIndex :
                BufferContext.ProcessorNumber;

        public bool Is32Bit(int nativePointerSize)
        {
            return
                EventHeader.HasFlag32BitHeader ||
                (!EventHeader.HasFlag64BitHeader && nativePointerSize == 4);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct EventHeaderCPtr
    {
        private readonly EVENT_HEADER* pEventHeader;

        public EventHeaderCPtr(EVENT_HEADER* pEventHeader)
        {
            this.pEventHeader = pEventHeader;
        }

        public EVENT_HEADER* Pointer => pEventHeader;
        public bool HasData => pEventHeader != null;
        public bool IsStringOnly => HasData && pEventHeader->IsStringOnly;
        public Guid ProviderId => HasData ? pEventHeader[0].ProviderId : Guid.Empty;
        public EVENT_DESCRIPTOR EventDescriptor => HasData ? pEventHeader->EventDescriptor : new EVENT_DESCRIPTOR();
        public Guid ActivityId => HasData ? pEventHeader->ActivityId : Guid.Empty;
        public uint ThreadId => HasData ? pEventHeader->ThreadId : uint.MaxValue;
        public uint ProcessId => HasData ? pEventHeader->ProcessId : 0;
    }

    public enum DECODING_SOURCE
    {
        DecodingSourceXMLFile,
        DecodingSourceWbem,
        DecodingSourceWPP,
        DecodingSourceMax
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

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct EventPropertyInfoCPtr
    {
        private readonly EVENT_PROPERTY_INFO* pEventPropertyInfo;

        public EventPropertyInfoCPtr(EVENT_PROPERTY_INFO* pEventPropertyInfo)
        {
            this.pEventPropertyInfo = pEventPropertyInfo;
        }

        public bool HasData => pEventPropertyInfo != null;

        public uint Length => HasData ? pEventPropertyInfo->length : 0u;

        public uint LengthPropertyIndex => HasData ? pEventPropertyInfo->lengthPropertyIndex : 0u;

        public ushort Count
        {
            get
            {
                if (!HasData)
                    return 0;
                return pEventPropertyInfo->count;
            }
        }

        public ushort CountPropertyIndex
        {
            get
            {
                if (!HasData)
                    return 0;
                if (!HasFlag(PROPERTY_FLAGS.PropertyParamCount))
                    throw new InvalidOperationException();
                return pEventPropertyInfo->countPropertyIndex;
            }
        }

        public ushort StructStartIndex
        {
            get
            {
                if (!HasData)
                    return 0;
                if (!HasFlag(PROPERTY_FLAGS.PropertyStruct))
                    throw new InvalidOperationException();
                return pEventPropertyInfo->StructStartIndex;
            }
        }

        public ushort NumOfStructMembers
        {
            get
            {
                if (!HasData)
                    return 0;
                if (!HasFlag(PROPERTY_FLAGS.PropertyStruct))
                    throw new InvalidOperationException();
                return pEventPropertyInfo->NumOfStructMembers;
            }
        }

        public TDH_IN_TYPE SimpleTdhInType => pEventPropertyInfo->InType;
        public TDH_OUT_TYPE SimpleTdhOutType => pEventPropertyInfo->OutType;

        public bool HasFlag(PROPERTY_FLAGS flag)
        {
            if (!HasData)
                return false;
            return (pEventPropertyInfo[0].Flags & flag) != 0;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TRACE_EVENT_INFO
    {
        public Guid ProviderGuid;
        public Guid EventGuid;
        public EVENT_DESCRIPTOR EventDescriptor;
        public DECODING_SOURCE DecodingSource;
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
    public struct UnmanagedString
        : IEquatable<UnmanagedString>
        , IComparable<UnmanagedString>
        , IComparable<string>
    {
        private readonly unsafe char* str;

        public unsafe UnmanagedString(char* str)
        {
            this.str = str;
        }

        public static readonly UnmanagedString Empty;

        public unsafe bool IsEmpty
        {
            get
            {
                if (HasValue)
                    return str[0] == '\0';
                return true;
            }
        }

        public unsafe bool HasValue => str != null;

        public override unsafe string ToString()
        {
            return str != null ? new string(str) : string.Empty;
        }

        public static implicit operator string(UnmanagedString value)
        {
            return value.ToString();
        }

        public bool Equals(UnmanagedString other)
        {
            throw new NotImplementedException();
        }

        public int CompareTo(UnmanagedString other)
        {
            throw new NotImplementedException();
        }

        public int CompareTo(string other)
        {
            throw new NotImplementedException();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct TraceEventInfoCPtr
    {
        private readonly TRACE_EVENT_INFO* pTraceEventInfo;
        private readonly uint cbTraceEventInfo;

        public TraceEventInfoCPtr(TRACE_EVENT_INFO* pTraceEventInfo, uint cbTraceEventInfo)
        {
            this.pTraceEventInfo = pTraceEventInfo;
            this.cbTraceEventInfo = cbTraceEventInfo;
        }

        public TRACE_EVENT_INFO* Ptr => pTraceEventInfo;
        public uint Size => cbTraceEventInfo;

        public bool HasValue => pTraceEventInfo != null && cbTraceEventInfo != 0;

        public EVENT_DESCRIPTOR EventDescriptor =>
            HasValue ? pTraceEventInfo->EventDescriptor : new EVENT_DESCRIPTOR();

        public uint TopLevelPropertyCount => HasValue ? pTraceEventInfo->TopLevelPropertyCount : 0;
        public uint PropertyCount => HasValue ? pTraceEventInfo->PropertyCount : 0;

        public ushort Id => HasValue ? pTraceEventInfo->EventDescriptor.Id : (ushort)0;
        public byte Version => HasValue ? pTraceEventInfo->EventDescriptor.Version : (byte)0;
        public byte Channel => HasValue ? pTraceEventInfo->EventDescriptor.Channel : (byte)0;
        public byte Level => HasValue ? pTraceEventInfo->EventDescriptor.Level : (byte)0;
        public ushort Task => HasValue ? pTraceEventInfo->EventDescriptor.Task : (ushort)0;
        public byte OpCode => HasValue ? pTraceEventInfo->EventDescriptor.Opcode : (byte)0;
        public ulong Keyword => HasValue ? pTraceEventInfo->EventDescriptor.Keyword : 0L;

        public UnmanagedString GetChannelName()
        {
            if (HasValue)
                return GetTdhString(pTraceEventInfo->ChannelNameOffset);
            return UnmanagedString.Empty;
        }

        public UnmanagedString GetLevelName()
        {
            if (HasValue)
                return GetTdhString(pTraceEventInfo->LevelNameOffset);
            return UnmanagedString.Empty;
        }

        public UnmanagedString GetTaskName()
        {
            if (HasValue)
                return GetTdhString(pTraceEventInfo->TaskNameOffset);
            return UnmanagedString.Empty;
        }

        public UnmanagedString GetOpcodeName()
        {
            if (HasValue)
                return GetTdhString(pTraceEventInfo->OpcodeNameOffset);
            return UnmanagedString.Empty;
        }

        private UnmanagedString GetTdhString(uint offset)
        {
            if (!HasValue || offset >= cbTraceEventInfo)
                return UnmanagedString.Empty;

            if (offset < 4 || offset % 2 != 0)
                return UnmanagedString.Empty;

            char* str = (char*)((byte*)pTraceEventInfo + offset);
            int maxLength = (int)((cbTraceEventInfo - offset) / 2);
            if (GetLength(str, maxLength) >= maxLength)
                return UnmanagedString.Empty;

            return new UnmanagedString(str);
        }

        private static int GetLength(char* str, int maxLength)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));
            if (maxLength < 0)
                throw new ArgumentOutOfRangeException(nameof(maxLength));

            int length = 0;
            while (length < maxLength && *str != 0) {
                ++length;
                ++str;
            }

            return length;
        }

        public EventPropertyInfoCPtr GetPropertyInfo(uint propertyIndex)
        {
            if (!HasValue || propertyIndex >= PropertyCount)
                return new EventPropertyInfoCPtr();

            return new EventPropertyInfoCPtr(
                TRACE_EVENT_INFO.GetEventPropertyInfoArray(pTraceEventInfo) + propertyIndex);
        }

        public Guid ProviderId => HasValue ? pTraceEventInfo->ProviderGuid : new Guid();

        private UnmanagedString ProviderNameTdh =>
            HasValue ? GetTdhString(pTraceEventInfo->ProviderNameOffset) : UnmanagedString.Empty;

        public UnmanagedString ProviderName
        {
            get
            {
                if (!HasValue)
                    return UnmanagedString.Empty;

                UnmanagedString providerNameTdh = ProviderNameTdh;
                if (providerNameTdh.HasValue)
                    return providerNameTdh;

                UnmanagedString providerMessage = ProviderMessage;
                if (providerMessage.HasValue)
                    return providerMessage;

                return new UnmanagedString();
                //return EventNameInfoSourceExtensions.InternProviderName(ProviderId);
            }
        }

        public UnmanagedString ProviderMessage =>
            HasValue ? GetTdhString(pTraceEventInfo->ProviderMessageOffset) : UnmanagedString.Empty;

        public UnmanagedString GetPropertyMap(uint propertyIndex)
        {
            if (!HasValue || propertyIndex >= PropertyCount)
                return UnmanagedString.Empty;

            var infoArray = TRACE_EVENT_INFO.GetEventPropertyInfoArray(pTraceEventInfo);
            if ((infoArray[0].Flags & PROPERTY_FLAGS.PropertyStruct) == 0) {
                infoArray += propertyIndex;
                uint nameOffset = infoArray[propertyIndex].MapNameOffset;
                if (nameOffset > 0)
                    return new UnmanagedString(
                        (char*)(pTraceEventInfo + nameOffset));
            }

            return UnmanagedString.Empty;
        }

        public string GetHeaderForProperty(uint propertyIndex)
        {
            if (!HasValue)
                return string.Empty;
            if (propertyIndex >= PropertyCount)
                return string.Empty;

            var infoArray = TRACE_EVENT_INFO.GetEventPropertyInfoArray(pTraceEventInfo);
            uint nameOffset = infoArray[propertyIndex].NameOffset;
            return new string((char*)((byte*)pTraceEventInfo + nameOffset));
        }

        public UnmanagedString Message =>
            HasValue ? GetTdhString(pTraceEventInfo->EventMessageOffset) : UnmanagedString.Empty;
    }

    public enum EVENT_HEADER_EXT_TYPE : ushort
    {
        RELATED_ACTIVITYID = 1,
        SID = 2,
        TS_ID = 3,
        INSTANCE_INFO = 4,
        STACK_TRACE32 = 5,
        STACK_TRACE64 = 6,
        PEBS_INDEX = 7,
        PMC_COUNTERS = 8,
        PSM_KEY = 9,
        EVENT_KEY = 10,
        EVENT_SCHEMA_TL = 11,
        PROV_TRAITS = 12,
        PROCESS_START_KEY = 13,
        MAX = 14,
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct EVENT_HEADER_EXTENDED_DATA_ITEM
    {
        private readonly ushort Reserved1;

        [MarshalAs(UnmanagedType.U2)]
        public readonly EVENT_HEADER_EXT_TYPE ExtType;

        private readonly ushort Reserved2;
        public readonly ushort DataSize;
        private readonly ulong DataPtr;

        public IntPtr Data => (IntPtr)DataPtr;

        public bool IsRelatedActivityId => ExtType == EVENT_HEADER_EXT_TYPE.RELATED_ACTIVITYID;
        public bool IsUserSecurityIdentifier => ExtType == EVENT_HEADER_EXT_TYPE.SID;
        public bool IsSessionId => ExtType == EVENT_HEADER_EXT_TYPE.TS_ID;
        public bool IsEventKey => ExtType == EVENT_HEADER_EXT_TYPE.EVENT_KEY;

        public Guid RelatedActivityId
        {
            get
            {
                if (!IsRelatedActivityId)
                    return Guid.Empty;
                if (DataSize < sizeof(Guid))
                    return Guid.Empty;
                return *(Guid*)Data;
            }
        }

        public SecurityIdentifier UserSecurityIdentifier
        {
            get
            {
                if (!IsUserSecurityIdentifier)
                    return null;
                if (DataSize < 2)
                    return null;

                byte* ptr = (byte*)Data;
                byte subAuthorityCount = ptr[1];
                int size = 8 + 4 * subAuthorityCount;
                if (size > DataSize)
                    return null;

                return new SecurityIdentifier(Data);
            }
        }

        public uint SessionId
        {
            get
            {
                if (!IsSessionId)
                    return 0;
                if (DataSize < 4)
                    return 0;
                return *(uint*)Data;
            }
        }

        public ulong EventKey
        {
            get
            {
                if (!IsEventKey)
                    return 0;
                if (DataSize < 8)
                    return 0;
                return *(ulong*)Data;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct EventRecordCPtr
    {
        private readonly EVENT_RECORD* pEventRecord;

        public EventRecordCPtr(EVENT_RECORD* pEventRecord)
        {
            this.pEventRecord = pEventRecord;
        }

        public EVENT_RECORD* Ptr => pEventRecord;
        public bool HasData => pEventRecord != null;

        public IntPtr UserData => HasData ? pEventRecord[0].UserData : IntPtr.Zero;
        public ushort UserDataLength => HasData ? pEventRecord[0].UserDataLength : (ushort)0;
        public TimePoint TimePoint => HasData ? pEventRecord->EventHeader.TimeStamp : TimePoint.Zero;
        public ulong ProcessorIndex => HasData ? pEventRecord->ProcessorIndex : ulong.MaxValue;

        public EventHeaderCPtr EventHeader =>
            HasData ? new EventHeaderCPtr(&pEventRecord->EventHeader) : new EventHeaderCPtr();

        public bool Is32Bit(int nativePointerSize)
        {
            if (HasData)
                return pEventRecord->Is32Bit(nativePointerSize);
            return nativePointerSize == 4;
        }

        public EVENT_HEADER_EXTENDED_DATA_ITEM* FindExtendedData(EVENT_HEADER_EXT_TYPE type)
        {
            for (int i = 0; i < pEventRecord->ExtendedDataCount; ++i) {
                if (pEventRecord->ExtendedData[i].ExtType == type)
                    return &pEventRecord->ExtendedData[i];
            }

            return null;
        }

        //public IEnumerable<EVENT_HEADER_EXTENDED_DATA_ITEM> ExtendedData
        //{
        //    get
        //    {
        //        return MarshalUtils.PtrToStructureArray<EVENT_HEADER_EXTENDED_DATA_ITEM>(
        //            pEventRecord->ExtendedData, pEventRecord->ExtendedDataCount, true);
        //    }
        //}

        public bool IsTraceLoggingEvent()
        {
            if (!HasData)
                return false;

            if (EventHeader.EventDescriptor.Channel == 11)
                return true;

            for (int i = 0; i < pEventRecord->ExtendedDataCount; ++i) {
                if (pEventRecord->ExtendedData[i].ExtType == EVENT_HEADER_EXT_TYPE.EVENT_SCHEMA_TL)
                    return true;
            }

            return false;
        }
    }

    public enum EventType
    {
        Unknown,
        Classic,
        Manifested,
        TraceLogging,
        Wpp,
        MaxValue
    }
}