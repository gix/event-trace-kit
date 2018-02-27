namespace EventTraceKit.VsExtension.Formatting
{
    using System;
    using System.Globalization;
    using Extensions;

    [DefaultFormat("N0")]
    [SupportedFormat(1, "N0", "Number", HelpText = "Decimal number with digit grouping\nExample: 10.000")]
    [SupportedFormat(2, "D0", "Decimal", HelpText = "Decimal number\nExample: 10000")]
    [SupportedFormat(3, "X", "Hexadecimal", HelpText = "Hexadecimal number with prefix\nExample: 0xABCDE")]
    [SupportedFormat(4, "Xs", "Hexadecimal (w/o prefix)", HelpText = "Hexadecimal number without prefix\nExample: ABCDE")]
    [SupportedFormat(5, "PX", "Padded Hex", HelpText = "Hexadecimal number padded with prefix\nExample: 0x000ABCDE")]
    [SupportedFormat(6, "PXs", "Padded Hex (w/o prefix)", HelpText = "Hexadecimal number padded without prefix\nExample: 000ABCDE")]
    public class NumericalFormatProvider : IFormatProvider, ICustomFormatter
    {
        public object GetFormat(Type formatType)
        {
            return this;
        }

        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (arg == null)
                return string.Empty;

            if (arg is Keyword kw)
                arg = kw.KeywordValue;

            var baseProvider = CultureInfo.CurrentCulture;

            if (format == "X")
                return string.Format(baseProvider, "0x{0:X}", arg);
            if (format == "Xs")
                return string.Format(baseProvider, "{0:X}", arg);

            if (format == "PX") {
                switch (arg) {
                    case byte x: return "0x" + x.ToString("X2", baseProvider);
                    case sbyte x: return "0x" + x.ToString("X2", baseProvider);
                    case ushort x: return "0x" + x.ToString("X4", baseProvider);
                    case short x: return "0x" + x.ToString("X4", baseProvider);
                    case uint x: return "0x" + x.ToString("X8", baseProvider);
                    case int x: return "0x" + x.ToString("X8", baseProvider);
                    case ulong x: return "0x" + x.ToString("X16", baseProvider);
                    case long x: return "0x" + x.ToString("X16", baseProvider);

                    case decimal x: return x.ToString("N0", baseProvider);
                    case double x:
                        var clamped = DoubleUtils.ClampToUInt64(x);
                        return "0x" + clamped.ToString("X16", baseProvider);
                }

                return string.Format(baseProvider, "0x{0:X}", arg);
            }

            if (format == "PXs") {
                switch (arg) {
                    case byte x: return x.ToString("X2", baseProvider);
                    case sbyte x: return x.ToString("X2", baseProvider);
                    case ushort x: return x.ToString("X4", baseProvider);
                    case short x: return x.ToString("X4", baseProvider);
                    case uint x: return x.ToString("X8", baseProvider);
                    case int x: return x.ToString("X8", baseProvider);
                    case ulong x: return x.ToString("X16", baseProvider);
                    case long x: return x.ToString("X16", baseProvider);

                    case decimal x: return x.ToString("N0", baseProvider);
                    case double x:
                        var clamped = DoubleUtils.ClampToUInt64(x);
                        return clamped.ToString("X16", baseProvider);
                }

                return string.Format(baseProvider, "{0:X}", arg);
            }

            if (format == "D0")
                return string.Format(baseProvider, "{0:D0}", arg);
            if (format == "N0")
                return string.Format(baseProvider, "{0:N0}", arg);
            if (format == "F0")
                return string.Format(baseProvider, "{0:F0}", arg);

            if (arg is IFormattable f)
                return f.ToString(format, baseProvider);

            return arg.ToString();
        }
    }
}
