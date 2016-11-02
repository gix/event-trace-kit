namespace EventTraceKit.VsExtension.Native
{
    using System;
    using System.Runtime.InteropServices;

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

        public TraceEventInfoCPtr(IntPtr pTraceEventInfo, UIntPtr cbTraceEventInfo)
        {
            this.pTraceEventInfo = (TRACE_EVENT_INFO*)pTraceEventInfo;
            this.cbTraceEventInfo = cbTraceEventInfo.ToUInt32();
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

        public DecodingSource DecodingSource => HasValue ? pTraceEventInfo->DecodingSource : 0L;

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
}
