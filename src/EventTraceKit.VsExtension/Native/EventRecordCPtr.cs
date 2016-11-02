namespace EventTraceKit.VsExtension.Native
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct EventRecordCPtr
    {
        private readonly EVENT_RECORD* pEventRecord;

        public EventRecordCPtr(IntPtr pEventRecord)
        {
            this.pEventRecord = (EVENT_RECORD*)pEventRecord;
        }

        public EventRecordCPtr(EVENT_RECORD* pEventRecord)
        {
            this.pEventRecord = pEventRecord;
        }

        public EVENT_RECORD* Ptr => pEventRecord;
        public bool HasData => pEventRecord != null;

        public TimePoint TimePoint => HasData ? pEventRecord->EventHeader.TimeStamp : TimePoint.Zero;
        public IntPtr UserData => HasData ? pEventRecord->UserData : IntPtr.Zero;
        public ushort UserDataLength => HasData ? pEventRecord->UserDataLength : (ushort)0;
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

        public Guid? TryGetRelatedActivityId()
        {
            var item = FindExtendedData(EVENT_HEADER_EXT_TYPE.RELATED_ACTIVITYID);
            if (item != null)
                return item->RelatedActivityId;
            return null;
        }

        public bool IsClassicEvent()
        {
            if (!HasData)
                return false;

            return EventHeader.HasData && EventHeader.Pointer->ClassicHeader;
        }

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

        public bool IsWppEvent()
        {
            if (!HasData)
                return false;

            // The provider used TraceMessage or TraceMessageVa to log the event.
            // Most providers do not use these functions to write events, so
            // this flag typically indicates that the event was written by WPP.
            return EventHeader.HasData && EventHeader.Pointer->IsTraceMessage;
        }
    }
}
