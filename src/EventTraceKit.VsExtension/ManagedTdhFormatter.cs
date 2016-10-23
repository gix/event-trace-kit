namespace EventTraceKit.VsExtension
{
    using System;
    using Native;

    public class ManagedTdhFormatter : IMessageFormatter
    {
        public unsafe string GetMessageForEvent(
            EventInfo eventInfo,
            ParseTdhContext parseTdhContext,
            IFormatProvider formatProvider)
        {
            var eventRecord = new EventRecordCPtr(
                (EVENT_RECORD*)eventInfo.EventRecord);

            var traceEventInfo = new TraceEventInfoCPtr(
                (TRACE_EVENT_INFO*)eventInfo.TraceEventInfo,
                (uint)eventInfo.TraceEventInfoSize);

            return TdhHelper.GetMessageForEventRecord(
                eventRecord, traceEventInfo, parseTdhContext, formatProvider);
        }
    }
}