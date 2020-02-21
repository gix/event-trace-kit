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
                return format switch
                {
                    "G" => time.ToString(@"hh\:mm\:ss\.fffffff", formatProvider),
                    "g" => time.ToString(@"mm\:ss\.fffffff", formatProvider),
                    "s" => time.TotalSeconds.ToString("F7", formatProvider),
                    "m" => time.TotalMilliseconds.ToString("F4", formatProvider),
                    "u" => (time.Ticks / 10.0).ToString("F1", formatProvider),
                    "t" => time.Ticks.ToString(formatProvider),
                    _ => time.ToString(format, formatProvider),
                };
            }

            return arg?.ToString() ?? string.Empty;
        }
    }
}
