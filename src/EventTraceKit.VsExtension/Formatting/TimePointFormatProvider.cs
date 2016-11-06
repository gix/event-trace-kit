namespace EventTraceKit.VsExtension.Formatting
{
    using System;

    [DefaultFormat("sN")]
    [SupportedFormat(1, "sN", "Seconds", "s")]
    [SupportedFormat(2, "mN", "Milliseconds", "ms")]
    [SupportedFormat(3, "uN", "Microseconds", "\x00b5s")]
    [SupportedFormat(4, "nN", "Nanoseconds", "ns")]
    [SupportedFormat(5, "tN", "Ticks")]
    internal class TimePointFormatProvider : IFormatProvider, ICustomFormatter
    {
        public object GetFormat(Type formatType) { return this; }

        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (arg == null)
                return string.Empty;

            if (arg is TimePoint) {
                var timePoint = (TimePoint)arg;
                return TimePointFormatter.ToString(timePoint.Ticks, format, formatProvider);
            }

            return arg.ToString();
        }
    }
}
