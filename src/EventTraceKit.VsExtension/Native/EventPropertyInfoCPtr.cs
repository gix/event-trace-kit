namespace EventTraceKit.VsExtension.Native
{
    using System;
    using System.Runtime.InteropServices;

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
}
