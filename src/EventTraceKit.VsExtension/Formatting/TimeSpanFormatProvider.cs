namespace EventTraceKit.VsExtension.Formatting
{
    using System;

    [DefaultFormat("G")]
    [SupportedFormat(1, "G", "Timestamp")]
    [SupportedFormat(2, "g", "Short Timestamp")]
    [SupportedFormat(3, "s", "Seconds", "s")]
    [SupportedFormat(4, "m", "Milliseconds", "ms")]
    [SupportedFormat(5, "u", "Microseconds", "\x00b5s")]
    [SupportedFormat(7, "t", "Ticks", "100ns", HelpText = "Time period expressed in 100-nanosecond units.")]
    internal class TimeSpanFormatProvider : IFormatProvider, ICustomFormatter
    {
        public object GetFormat(Type formatType) { return this; }

        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (arg is TimeSpan time) {
                switch (format) {
                    case "G":
                        return time.ToString(@"hh\:mm\:ss\.fffffff", formatProvider);
                    case "g":
                        return time.ToString(@"mm\:ss\.fffffff", formatProvider);
                    case "s":
                        return time.TotalSeconds.ToString("F7", formatProvider);
                    case "m":
                        return time.TotalMilliseconds.ToString("F4", formatProvider);
                    case "u":
                        return (time.Ticks / 10.0).ToString("F1", formatProvider);
                    case "t":
                        return time.Ticks.ToString(formatProvider);
                    default:
                        return time.ToString(format, formatProvider);
                }
            }

            return arg?.ToString() ?? string.Empty;
        }
    }
}
