namespace EventTraceKit.VsExtension
{
    using System;

    public interface IMessageFormatter
    {
        string GetMessageForEvent(
            EventInfo eventInfo,
            ParseTdhContext parseTdhContext,
            IFormatProvider formatProvider);
    }
}
