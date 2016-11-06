namespace EventTraceKit.VsExtension
{
    using System;
    using System.ComponentModel;

    public static class TimePointFormatter
    {
        public const string FormatGrouped = "N";
        public const string FormatMicroseconds = "u";
        public const string FormatMicrosecondsGrouped = "uN";
        public const string FormatMilliseconds = "m";
        public const string FormatMillisecondsGrouped = "mN";
        public const string FormatNanoseconds = "n";
        public const string FormatNanosecondsGrouped = "nN";
        public const string FormatTicks = "t";
        public const string FormatTicksGrouped = "tN";
        public const string FormatSeconds = "s";
        public const string FormatSecondsGrouped = "sN";
        public const string FormatWithUnits = "U";

        public static string ToString(long ticks, string format, IFormatProvider formatProvider)
        {
            TimeUnit unit;
            bool includeUnits;
            bool includeThousandsSeparator;
            if (!TryParseFormat(format, out unit, out includeUnits, out includeThousandsSeparator))
                throw new FormatException("Invalid or unsupported format.");

            return ToString(ticks, formatProvider, unit, includeUnits, includeThousandsSeparator);
        }

        public static string ToString(
            long ticks, IFormatProvider formatProvider,
            TimeUnit timeUnits, bool includeUnits, bool includeThousandsSeparator)
        {
            decimal value;
            string unitSymbol;
            string digitsFormat;
            switch (timeUnits) {
                case TimeUnit.Seconds:
                    value = ticks / 10000000M;
                    unitSymbol = "s";
                    digitsFormat = includeThousandsSeparator ? "N9" : "F9";
                    break;

                case TimeUnit.Milliseconds:
                    value = ticks / 10000M;
                    unitSymbol = "ms";
                    digitsFormat = includeThousandsSeparator ? "N6" : "F6";
                    break;

                case TimeUnit.Microseconds:
                    value = ticks / 10M;
                    unitSymbol = "us";
                    digitsFormat = includeThousandsSeparator ? "N3" : "F3";
                    break;

                case TimeUnit.Nanoseconds:
                    value = ticks * 100;
                    unitSymbol = "ns";
                    digitsFormat = includeThousandsSeparator ? "N0" : "F0";
                    break;

                case TimeUnit.Ticks:
                    value = ticks;
                    unitSymbol = "t";
                    digitsFormat = includeThousandsSeparator ? "N0" : "F0";
                    break;

                default:
                    throw new InvalidEnumArgumentException(
                        nameof(timeUnits), (int)timeUnits, typeof(TimeUnit));
            }

            string digits = value.ToString(digitsFormat, formatProvider);
            return includeUnits ? digits + unitSymbol : digits;
        }

        private static bool TryParseFormat(
            string format, out TimeUnit unit, out bool includeUnits,
            out bool includeThousandsSeparator)
        {
            unit = TimeUnit.Nanoseconds;
            includeUnits = false;
            includeThousandsSeparator = false;

            if (format == null)
                return true;

            bool parsedUnit = false;
            for (int i = 0; i < format.Length; ++i) {
                switch (format[i]) {
                    case 'N':
                        if (includeThousandsSeparator)
                            return false;
                        includeThousandsSeparator = true;
                        continue;
                    case 'U':
                        if (includeUnits)
                            return false;
                        includeUnits = true;
                        continue;

                    case 's':
                        if (parsedUnit)
                            return false;
                        unit = TimeUnit.Seconds;
                        break;
                    case 'm':
                        if (parsedUnit)
                            return false;
                        unit = TimeUnit.Milliseconds;
                        break;
                    case 'u':
                        if (parsedUnit)
                            return false;
                        unit = TimeUnit.Microseconds;
                        break;
                    case 'n':
                        if (parsedUnit)
                            return false;
                        unit = TimeUnit.Nanoseconds;
                        break;
                    case 't':
                        if (parsedUnit)
                            return false;
                        unit = TimeUnit.Ticks;
                        break;

                    default:
                        return false;
                }

                parsedUnit = true;
            }

            return true;
        }
    }

    public enum TimeUnit
    {
        Seconds,
        Milliseconds,
        Microseconds,
        Nanoseconds,
        Ticks
    }
}
