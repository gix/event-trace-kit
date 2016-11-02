namespace EventTraceKit.VsExtension.Native
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security.Principal;

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

    [Flags]
    public enum EVENT_HEADER_FLAG
    {
        EXTENDED_INFO = 0x0001,
        PRIVATE_SESSION = 0x0002,
        STRING_ONLY = 0x0004,
        TRACE_MESSAGE = 0x0008,
        NO_CPUTIME = 0x0010,
        HAS_32_BIT_HEADER = 0x0020,
        HAS_64_BIT_HEADER = 0x0040,
        CLASSIC_HEADER = 0x0100,
        PROCESSOR_INDEX = 0x0200
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

        public bool HasExtendedInfo => HasFlag(EVENT_HEADER_FLAG.EXTENDED_INFO);
        public bool IsPrivateSession => HasFlag(EVENT_HEADER_FLAG.PRIVATE_SESSION);
        public bool IsStringOnly => HasFlag(EVENT_HEADER_FLAG.STRING_ONLY);
        public bool IsTraceMessage => HasFlag(EVENT_HEADER_FLAG.TRACE_MESSAGE);
        public bool NoCpuTime => HasFlag(EVENT_HEADER_FLAG.NO_CPUTIME);
        public bool HasFlag32BitHeader => HasFlag(EVENT_HEADER_FLAG.HAS_32_BIT_HEADER);
        public bool HasFlag64BitHeader => HasFlag(EVENT_HEADER_FLAG.HAS_64_BIT_HEADER);
        public bool ClassicHeader => HasFlag(EVENT_HEADER_FLAG.CLASSIC_HEADER);
        public bool HasFlagProcessorIndex => HasFlag(EVENT_HEADER_FLAG.PROCESSOR_INDEX);

        public uint KernelTime => TimeUnion.KernelTime;
        public uint UserTime => TimeUnion.UserTime;
        public ulong ProcessorTime => TimeUnion.ProcessorTime;

        private bool HasFlag(EVENT_HEADER_FLAG flag)
        {
            return (Flags & (int)flag) != 0;
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
            const int primeFactor = 397;
            unchecked {
                int hash = Id.GetHashCode();
                hash = (hash * primeFactor) ^ Version.GetHashCode();
                hash = (hash * primeFactor) ^ Channel.GetHashCode();
                hash = (hash * primeFactor) ^ Level.GetHashCode();
                hash = (hash * primeFactor) ^ Opcode.GetHashCode();
                hash = (hash * primeFactor) ^ Task.GetHashCode();
                hash = (hash * primeFactor) ^ Keyword.GetHashCode();
                return hash;
            }
        }
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
}
