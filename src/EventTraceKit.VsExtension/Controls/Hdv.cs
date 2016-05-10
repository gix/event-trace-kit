namespace EventTraceKit.VsExtension.Controls.Hdv
{
    using System;
    using System.ComponentModel;
    using System.Data.SqlClient;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Security.Principal;
    using System.Windows;

    public static class DataColumn
    {
        public static DataColumn<T> Create<T>(Func<int, T> staticGenerator)
        {
            return Create<T>(null);
        }
    }

    public class DataColumn<T>
    {
    }

    public class HdvColumnViewModelPreset
    {
    }

    public class DataTableGenerator
    {
        public DataColumn<T> AddColumn<T>(
            HdvColumnViewModelPreset columnPreset, DataColumn<T> dataColumn,
            bool isHierarchical, IFormatProvider formatProvider = null,
            DataColumn<string> headerColumn = null, bool isPercent = false,
            bool isDynamic = false)
        {
            throw new NotImplementedException();
        }

        public DataColumn<T> AddColumn<T>(
            Guid columnGuid, string columnName, DataColumn<T> dataColumn,
            bool isHierarchical, bool isVisible, int width,
            SortOrder sortOrder = 0,
            int sortPriority = -1,
            TextAlignment? textAlignment = new TextAlignment?(),
            string format = null,
            IFormatProvider formatProvider = null,
            DataColumn<string> headerColumn = null,
            bool isPercent = false,
            bool isDynamic = false,
            string columnHelpText = null)
        {
            throw new NotImplementedException();
        }
    }

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

    public sealed class TimestampConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return (sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string str = value as string;
            if (str != null)
                return TimePoint.Parse(str.Trim());
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
                throw new ArgumentNullException(nameof(destinationType));
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    [TypeConverter(typeof(TimestampConverter))]
    public struct TimePoint
        : IComparable<TimePoint>
        , IEquatable<TimePoint>
    {
        private readonly long nanoseconds;

        public TimePoint(long nanoseconds)
        {
            this.nanoseconds = nanoseconds;
        }

        public static TimePoint FromNanoseconds(long nanoseconds)
        {
            return new TimePoint(nanoseconds);
        }

        public long ToNanoseconds => nanoseconds;
        public long ToMicroseconds => nanoseconds / 1000;
        public long ToMilliseconds => nanoseconds / 1000000;
        public long ToSeconds => nanoseconds / 1000000000;

        public static TimePoint Abs(TimePoint value)
        {
            return FromNanoseconds(Math.Abs(value.nanoseconds));
        }

        public static TimePoint Min(TimePoint lhs, TimePoint rhs)
        {
            return new TimePoint(Math.Min(lhs.ToNanoseconds, rhs.ToNanoseconds));
        }

        public static TimePoint Max(TimePoint lhs, TimePoint rhs)
        {
            return new TimePoint(Math.Max(lhs.ToNanoseconds, rhs.ToNanoseconds));
        }

        public static TimePoint Zero => new TimePoint();

        public static TimePoint MinValue => new TimePoint(-9223372036854775808L);

        public static TimePoint MaxValue => new TimePoint(9223372036854775807);

        public int CompareTo(TimePoint other)
        {
            return
                nanoseconds < other.nanoseconds ? -1 :
                nanoseconds <= other.nanoseconds ? 0 :
                1;
        }

        public bool Equals(TimePoint other)
        {
            return nanoseconds == other.nanoseconds;
        }

        public static bool operator ==(TimePoint lhs, TimePoint rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(TimePoint lhs, TimePoint rhs)
        {
            return !lhs.Equals(rhs);
        }

        public static bool operator <(TimePoint lhs, TimePoint rhs)
        {
            return lhs.CompareTo(rhs) < 0;
        }

        public static bool operator >(TimePoint lhs, TimePoint rhs)
        {
            return lhs.CompareTo(rhs) > 0;
        }

        public static bool operator <=(TimePoint lhs, TimePoint rhs)
        {
            return lhs.CompareTo(rhs) <= 0;
        }

        public static bool operator >=(TimePoint lhs, TimePoint rhs)
        {
            return lhs.CompareTo(rhs) >= 0;
        }

        public override bool Equals(object other)
        {
            return other is TimePoint && Equals((TimePoint)other);
        }

        public override int GetHashCode()
        {
            return nanoseconds.GetHashCode();
        }

        public override string ToString()
        {
            return nanoseconds.ToString("F0");
        }

        public static TimePoint Parse(string s)
        {
            return new TimePoint(long.Parse(s));
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

        public bool HasFlagProcessorIndex => HasFlag(0x200);
        public bool HasFlag32BitHeader => HasFlag(0x20);
        public bool HasFlag64BitHeader => HasFlag(0x40);

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
        public readonly ushort ExtendedDataCount;
        public readonly ushort UserDataLength;
        public readonly EVENT_HEADER_EXTENDED_DATA_ITEM* ExtendedData;
        public readonly IntPtr UserData;
        public readonly IntPtr UserContext;

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
        public readonly PROPERTY_FLAGS Flags;
        public readonly uint NameOffset;
        private readonly Union union;
        private readonly ushort countAndCountPropertyIndex;
        private readonly ushort lengthAndLengthPropertyIndex;
        private readonly uint Reserved;

        public TDH_IN_TYPE InType => (TDH_IN_TYPE)union.field0UInt16;
        public TDH_OUT_TYPE OutType => (TDH_OUT_TYPE)union.field1UInt16;
        public uint MapNameOffset => union.field2UInt32;
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
            public readonly ushort field0UInt16;

            [FieldOffset(2)]
            public readonly ushort field1UInt16;

            [FieldOffset(4)]
            public readonly uint field2UInt32;
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
        public readonly Guid ProviderGuid;
        public readonly Guid EventGuid;
        public readonly EVENT_DESCRIPTOR EventDescriptor;
        public readonly DECODING_SOURCE DecodingSource;
        public readonly uint ProviderNameOffset;
        public readonly uint LevelNameOffset;
        public readonly uint ChannelNameOffset;
        public readonly uint KeywordsNameOffset;
        public readonly uint TaskNameOffset;
        public readonly uint OpcodeNameOffset;
        public readonly uint EventMessageOffset;
        public readonly uint ProviderMessageOffset;
        public readonly uint BinaryXMLOffset;
        public readonly uint BinaryXMLSize;
        public readonly uint ActivityIDNameOffset;
        public readonly uint RelatedActivityIDNameOffset;
        public readonly uint PropertyCount;
        public readonly uint TopLevelPropertyCount;
        public readonly TEMPLATE_FLAGS Flags;
        private EVENT_PROPERTY_INFO EventPropertyInfoArray;

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

        public static readonly UnmanagedString Empty = new UnmanagedString();

        public unsafe bool HasValue => str != null;

        public override unsafe string ToString()
        {
            return str != null ? new string(str) : string.Empty;
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

        internal TraceEventInfoCPtr(TRACE_EVENT_INFO* pTraceEventInfo, uint cbTraceEventInfo)
        {
            this.pTraceEventInfo = pTraceEventInfo;
            this.cbTraceEventInfo = cbTraceEventInfo;
        }

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

        public UnmanagedString MessageName =>
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

    internal class ConstantColumn
    {
        public static DataColumn<T> CreateComputedColumn<T>(T constant)
        {
            return DataColumn.Create<T>(null);
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

    [StructLayout(LayoutKind.Sequential)]
    public struct Keyword
        : IEquatable<Keyword>
        , IComparable<Keyword>
    {
        public Keyword(ulong keywordValue)
        {
            KeywordValue = keywordValue;
        }

        public ulong KeywordValue { get; }

        public static Keyword Zero => new Keyword();
        public static Keyword MinValue => new Keyword(0L);
        public static Keyword MaxValue => new Keyword(ulong.MaxValue);

        public static bool operator ==(Keyword lhs, Keyword rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(Keyword lhs, Keyword rhs)
        {
            return !lhs.Equals(rhs);
        }

        public static bool operator <(Keyword lhs, Keyword rhs)
        {
            return lhs.CompareTo(rhs) < 0;
        }

        public static bool operator >(Keyword lhs, Keyword rhs)
        {
            return lhs.CompareTo(rhs) > 0;
        }

        public static bool operator <=(Keyword lhs, Keyword rhs)
        {
            return lhs.CompareTo(rhs) <= 0;
        }

        public static bool operator >=(Keyword lhs, Keyword rhs)
        {
            return lhs.CompareTo(rhs) >= 0;
        }

        public static Keyword operator &(Keyword lhs, Keyword rhs)
        {
            return new Keyword(lhs.KeywordValue & rhs.KeywordValue);
        }

        public static implicit operator Keyword(ulong keywordValue)
        {
            return new Keyword(keywordValue);
        }

        public bool Equals(Keyword other)
        {
            return KeywordValue == other.KeywordValue;
        }

        public int CompareTo(Keyword other)
        {
            return KeywordValue.CompareTo(other.KeywordValue);
        }

        public override bool Equals(object other)
        {
            return other is Keyword && Equals((Keyword)other);
        }

        public override int GetHashCode()
        {
            return KeywordValue.GetHashCode();
        }

        public override string ToString()
        {
            return KeywordValue.ToString(CultureInfo.InvariantCulture);
        }
    }

    public class CrimsonEventsViewModelSource
    {
        public class CrimsonEventsInfo
        {
            private unsafe TRACE_EVENT_INFO** traceEventInfos;
            private unsafe EVENT_RECORD** eventRecords;

            public uint ProjectProcessId(int index)
            {
                return GetEventRecord(index).EventHeader.ProcessId;
            }

            public unsafe TraceEventInfoCPtr GetTraceEventInfo(int index)
            {
                return new TraceEventInfoCPtr(traceEventInfos[index], 0);
            }

            public unsafe EventRecordCPtr GetEventRecord(int index)
            {
                return new EventRecordCPtr(eventRecords[index]);
            }

            public ushort ProjectId(int index)
            {
                return GetEventRecord(index).EventHeader.EventDescriptor.Id;
            }

            public byte ProjectVersion(int index)
            {
                return GetEventRecord(index).EventHeader.EventDescriptor.Version;
            }

            public byte ProjectChannel(int index)
            {
                return GetEventRecord(index).EventHeader.EventDescriptor.Channel;
            }

            public UnmanagedString ProjectChannelName(int index)
            {
                return GetTraceEventInfo(index).GetChannelName();
            }

            public byte ProjectLevel(int index)
            {
                return GetEventRecord(index).EventHeader.EventDescriptor.Level;
            }

            public UnmanagedString ProjectLevelName(int index)
            {
                return GetTraceEventInfo(index).GetLevelName();
            }

            public ushort ProjectTask(int index)
            {
                return GetEventRecord(index).EventHeader.EventDescriptor.Task;
            }

            public UnmanagedString ProjectTaskName(int index)
            {
                return GetTraceEventInfo(index).GetTaskName();
            }

            public byte ProjectOpCode(int index)
            {
                return GetEventRecord(index).EventHeader.EventDescriptor.Opcode;
            }

            public UnmanagedString ProjectOpCodeName(int index)
            {
                //var eventRecord = GetEventRecord(index);
                //if (eventRecord.IsTraceLoggingEvent()) {
                //    int opcode = eventRecord.EventHeader.EventDescriptor.Opcode;
                //    return this.winmetaOpcodeService.GetOpcodeName(opcode);
                //}
                return GetTraceEventInfo(index).GetOpcodeName();
            }

            public ulong ProjectKeyword(int index)
            {
                return GetEventRecord(index).EventHeader.EventDescriptor.Keyword;
            }

            public Guid ProjectProviderGuid(int index)
            {
                return GetEventRecord(index).EventHeader.ProviderId;
            }

            public string ProjectMessage(int index)
            {
                throw new NotImplementedException();
            }

            public string ProjectProviderName(int index)
            {
                TraceEventInfoCPtr eventInfo = GetTraceEventInfo(index);
                if (eventInfo.HasValue)
                    return eventInfo.ProviderName.ToString();

                return ProjectProviderGuid(index).ToString();
            }

            public Guid ProjectActivityId(int index)
            {
                return GetEventRecord(index).EventHeader.ActivityId;
            }

            private static readonly Guid ActivityIdSentinel =
                new Guid("D733D8B0-7D18-4AEB-A3FC-8C4613BC2A40");

            public unsafe Guid ProjectRelatedActivityId(int index)
            {
                var item = GetEventRecord(index).FindExtendedData(EVENT_HEADER_EXT_TYPE.RELATED_ACTIVITYID);
                if (item == null)
                    return ActivityIdSentinel;
                return item->RelatedActivityId;
            }

            public unsafe string ProjectUserSecurityIdentifier(int index)
            {
                var item = GetEventRecord(index).FindExtendedData(EVENT_HEADER_EXT_TYPE.SID);
                if (item == null)
                    return string.Empty;

                SecurityIdentifier sid = item->UserSecurityIdentifier;
                return sid?.ToString() ?? string.Empty;
            }

            public int ProjectThreadId(int index)
            {
                return (int)GetEventRecord(index).EventHeader.ThreadId;
            }

            public TimePoint ProjectTimePoint(int index)
            {
                return TimePoint.Zero;
            }
        }

        public interface ISession
        {
        }

        public DataTableGenerator CreateTable(ISession session)
        {
            var info = new CrimsonEventsInfo();
            var generator = new DataTableGenerator();

            HdvColumnViewModelPreset providerNamePreset = null;
            HdvColumnViewModelPreset taskNamePreset = null;
            HdvColumnViewModelPreset versionPreset = null;
            HdvColumnViewModelPreset idPreset = null;
            HdvColumnViewModelPreset channelPreset = null;
            HdvColumnViewModelPreset levelPreset = null;
            HdvColumnViewModelPreset opcodePreset = null;
            HdvColumnViewModelPreset taskPreset = null;
            HdvColumnViewModelPreset keywordPreset = null;
            HdvColumnViewModelPreset opcodeNamePreset = null;
            HdvColumnViewModelPreset messagePreset = null;
            HdvColumnViewModelPreset providerIdPreset = null;
            HdvColumnViewModelPreset threadIdPreset = null;
            HdvColumnViewModelPreset processIdPreset = null;

            generator.AddColumn(
                providerNamePreset,
                DataColumn.Create(info.ProjectProviderName),
                false);

            generator.AddColumn(
                taskNamePreset,
                DataColumn.Create(info.ProjectTaskName),
                false);

            generator.AddColumn(
                versionPreset,
                DataColumn.Create(info.ProjectVersion),
                false);

            generator.AddColumn(
                idPreset,
                DataColumn.Create(info.ProjectId),
                false);

            generator.AddColumn(
                channelPreset,
                DataColumn.Create(info.ProjectChannel),
                false);

            generator.AddColumn(
                levelPreset,
                DataColumn.Create(info.ProjectLevel),
                false);

            generator.AddColumn(
                opcodePreset,
                DataColumn.Create(info.ProjectOpCode),
                false, null,
                ConstantColumn.CreateComputedColumn("Opcode"));

            generator.AddColumn(
                taskPreset,
                DataColumn.Create(info.ProjectTask),
                false);

            generator.AddColumn(
                keywordPreset,
                DataColumn.Create(info.ProjectKeyword),
                false);

            generator.AddColumn(
                opcodeNamePreset,
                DataColumn.Create(info.ProjectOpCodeName),
                false);

            generator.AddColumn(
                messagePreset,
                DataColumn.Create(info.ProjectMessage),
                false);

            generator.AddColumn(
                providerIdPreset,
                DataColumn.Create(info.ProjectProviderGuid),
                false);

            generator.AddColumn(
                threadIdPreset,
                DataColumn.Create(info.ProjectThreadId),
                false);

            //generator.AddColumn(
            //    activityIdPreset,
            //    DataColumn.Create(info.ProjectActivityId),
            //    false, new ActivityIdFormatter(sentinelActivityId), null, false,
            //    false);

            //generator.AddColumn(
            //    relatedActivityIdPreset,
            //    DataColumn.Create(info.ProjectRelatedActivityId),
            //    false, new ActivityIdFormatter(sentinelActivityId), null,
            //    false, false);

            //generator.AddColumn(
            //    userSecurityIdentifierPreset,
            //    DataColumn.Create(info.ProjectUserSecurityIdentifier),
            //    false);

            generator.AddColumn(
                processIdPreset,
                DataColumn.Create(info.ProjectProcessId),
                false);

            //const int MaxEventDataDescriptors = 128;
            //for (uint i = 0; i < MaxEventDataDescriptors; ++i) {
            //    DataColumn<string> column39 = DataColumn.Create<string>(new TopLevelPropertyGenerator(info, i));
            //    DataColumn<string> header = DataColumn.Create<string>(new TopLevelPropertyHeaderGenerator(info, i));
            //    var data = StringWithLogicalComparisonColumn.CreateComputedColumn(column39);
            //    string columnName = "UserData" + (i + 1);
            //    generator.AddColumn(
            //        GuidUtils.GenerateGuidFromName(columnName),
            //        columnName,
            //        data,
            //        false,
            //        true,
            //        80,
            //        SortOrder.Unspecified, -1, 0, null, null, header, false, true);
            //}

            return generator;
        }
    }
}