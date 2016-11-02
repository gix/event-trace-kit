namespace EventTraceKit.VsExtension.Formatting
{
    using System;
    using System.Globalization;
    using Extensions;

    [DefaultFormat("N0")]
    [SupportedFormat(1, "N0", "Number")]
    [SupportedFormat(2, "D0", "Decimal")]
    [SupportedFormat(3, "X", "Hexadecimal")]
    [SupportedFormat(4, "Xs", "Hexadecimal (w/o prefix)")]
    public class NumericalFormatProvider : IFormatProvider, ICustomFormatter
    {
        public object GetFormat(Type formatType)
        {
            return this;
        }

        public virtual string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (arg == null)
                return string.Empty;

            if (arg is Keyword)
                arg = ((Keyword)arg).KeywordValue;

            if (format == "X") {
                if (arg is byte || arg is sbyte)
                    return string.Format(CultureInfo.CurrentCulture, "0x{0:X2}", arg);
                if (arg is ushort || arg is short)
                    return string.Format(CultureInfo.CurrentCulture, "0x{0:X4}", arg);
                if (arg is uint || arg is int)
                    return string.Format(CultureInfo.CurrentCulture, "0x{0:X8}", arg);
                if (arg is ulong || arg is long)
                    return string.Format(CultureInfo.CurrentCulture, "0x{0:X16}", arg);

                if (arg is decimal)
                    return string.Format(CultureInfo.CurrentCulture, "{0:N0}", arg);

                if (arg is double) {
                    ulong value = DoubleUtils.ClampToUInt64((double)arg);
                    return string.Format(CultureInfo.CurrentCulture, "0x{0:X16}", value);
                }

                return string.Format(CultureInfo.CurrentCulture, "0x{0:X}", arg);
            }

            if (format == "Xs") {
                if (arg is byte || arg is sbyte)
                    return string.Format(CultureInfo.CurrentCulture, "{0:X2}", arg);
                if (arg is ushort || arg is short)
                    return string.Format(CultureInfo.CurrentCulture, "{0:X4}", arg);
                if (arg is uint || arg is int)
                    return string.Format(CultureInfo.CurrentCulture, "{0:X8}", arg);
                if (arg is ulong || arg is long)
                    return string.Format(CultureInfo.CurrentCulture, "{0:X16}", arg);

                if (arg is decimal)
                    return string.Format(CultureInfo.CurrentCulture, "{0:N0}", arg);

                if (arg is double) {
                    ulong value = DoubleUtils.ClampToUInt64((double)arg);
                    return string.Format(CultureInfo.CurrentCulture, "{0:X16}", value);
                }

                return string.Format(CultureInfo.CurrentCulture, "{0:X}", arg);
            }

            if (format == "D0")
                return string.Format(CultureInfo.CurrentCulture, "{0:D0}", arg);
            if (format == "N0")
                return string.Format(CultureInfo.CurrentCulture, "{0:N0}", arg);
            if (format == "F0")
                return string.Format(CultureInfo.CurrentCulture, "{0:F0}", arg);

            throw new InvalidOperationException($"Unsupported format '{format}'.");
        }
    }
}