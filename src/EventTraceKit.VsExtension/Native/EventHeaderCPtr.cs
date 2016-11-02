namespace EventTraceKit.VsExtension.Native
{
    using System;
    using System.Runtime.InteropServices;

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
}
