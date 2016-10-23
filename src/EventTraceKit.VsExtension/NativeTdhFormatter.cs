namespace EventTraceKit.VsExtension
{
    using System;

    public class NativeTdhFormatter : IMessageFormatter
    {
        private readonly EtwMessageFormatter formatter = new EtwMessageFormatter();

        public unsafe string GetMessageForEvent(
            EventInfo eventInfo,
            ParseTdhContext parseTdhContext,
            IFormatProvider formatProvider)
        {
            return formatter.FormatEventMessage(
                (void*)eventInfo.EventRecord,
                (void*)eventInfo.TraceEventInfo,
                (uint)eventInfo.TraceEventInfoSize,
                (uint)parseTdhContext.NativePointerSize);
        }
    }
}