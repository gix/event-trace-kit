namespace EventTraceKit.VsExtension.Formatting
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Extensions;

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DefaultFormatAttribute : Attribute
    {
        public DefaultFormatAttribute(string defaultFormat)
        {
            DefaultFormat = defaultFormat;
        }

        public string DefaultFormat { get; }
    }

    public struct SupportedFormat
    {
        private readonly string description;

        public SupportedFormat(string format, string description)
        {
            Format = format;
            this.description = description;
            Unit = null;
        }

        public SupportedFormat(string format, string description, string unit)
        {
            Format = format;
            this.description = description;
            Unit = unit;
        }

        public bool HasValue => Format != null;
        public string Format { get; }
        public string Unit { get; }

        public string Description
        {
            get
            {
                if (Unit == null)
                    return description;

                return string.Format(
                    CultureInfo.CurrentCulture, "{0} ({1})", description, Unit);
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class SupportedFormatAttribute : Attribute
    {
        public SupportedFormatAttribute(int ordinal, string format, string description)
        {
            Ordinal = ordinal;
            SupportedFormat = new SupportedFormat(format, description);
        }

        public SupportedFormatAttribute(int ordinal, string format, string description, string units)
        {
            Ordinal = ordinal;
            SupportedFormat = new SupportedFormat(format, description, units);
        }

        public int Ordinal { get; }
        public SupportedFormat SupportedFormat { get; }
    }

    public static class FormatProviderExtensions
    {
        public static string DefaultFormat(
            this IFormatProvider formatProvider)
        {
            return (formatProvider?.GetType()).GetCustomAttribute<DefaultFormatAttribute>(true)?.DefaultFormat;
        }

        public static SupportedFormat DefaultSupportedFormat(
            this IFormatProvider formatProvider)
        {
            if (formatProvider == null)
                return new SupportedFormat();

            var formats = formatProvider.SupportedFormats();
            if (!formats.Any())
                return new SupportedFormat();

            string defaultFormat = formatProvider.DefaultFormat();
            if (defaultFormat == null)
                return new SupportedFormat();

            return formats.Single(x => x.Format == defaultFormat);
        }

        public static IReadOnlyList<SupportedFormat> SupportedFormats(
            this IFormatProvider formatProvider)
        {
            if (formatProvider == null)
                return new SupportedFormat[0];

            return formatProvider.GetType()
                .GetCustomAttributes<SupportedFormatAttribute>()
                .OrderBy(x => x.Ordinal)
                .Select(x => x.SupportedFormat)
                .ToArray();
        }
    }

    public interface IFormatProviderSource
    {
        IFormatProvider GetFormatProvider(Type dataType);
    }

    public class Cache<TKey, TValue>
    {
        private readonly ConcurrentDictionary<TKey, TValue> cache =
            new ConcurrentDictionary<TKey, TValue>();
        private readonly Func<TKey, TValue> constructor;

        public Cache(Func<TKey, TValue> constructor)
        {
            this.constructor = constructor;
        }

        public TValue this[TKey key] => cache.GetOrAdd(key, constructor);
    }

    public sealed class DefaultFormatProviderSource : IFormatProviderSource
    {
        private readonly Cache<Type, IFormatProvider> formatProviderCache =
            new Cache<Type, IFormatProvider>(CreateFormatProvider);

        public IFormatProvider GetFormatProvider(Type type)
        {
            return formatProviderCache[type];
        }

        private static IFormatProvider CreateFormatProvider(Type type)
        {
            if (type == typeof(byte) ||
                type == typeof(ushort) ||
                type == typeof(uint) ||
                type == typeof(ulong) ||
                type == typeof(sbyte) ||
                type == typeof(short) ||
                type == typeof(int) ||
                type == typeof(long) ||
                type == typeof(decimal) ||
                type == typeof(double))
                return new NumericalFormatProvider();

            if (type == typeof(TimePoint))
                return new TimePointFormatProvider();

            return null;
        }
    }

    [DefaultFormat("N0")]
    [SupportedFormat(1, "N0", "Decimal")]
    [SupportedFormat(2, "x", "Hexadecimal")]
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

            if (format == "x") {
                if (arg is byte)
                    return string.Format(CultureInfo.CurrentCulture, "0x{0:X2}", arg);
                if (arg is ushort)
                    return string.Format(CultureInfo.CurrentCulture, "0x{0:X4}", arg);
                if (arg is uint)
                    return string.Format(CultureInfo.CurrentCulture, "0x{0:X8}", arg);
                if (arg is ulong)
                    return string.Format(CultureInfo.CurrentCulture, "0x{0:X16}", arg);

                if (arg is sbyte)
                    return string.Format(CultureInfo.CurrentCulture, "0x{0:X2}", arg);
                if (arg is short)
                    return string.Format(CultureInfo.CurrentCulture, "0x{0:X4}", arg);
                if (arg is int)
                    return string.Format(CultureInfo.CurrentCulture, "0x{0:X8}", arg);
                if (arg is long)
                    return string.Format(CultureInfo.CurrentCulture, "0x{0:X16}", arg);

                if (arg is decimal)
                    return string.Format(CultureInfo.CurrentCulture, "{0:N0}", arg);

                if (arg is double) {
                    ulong value = DoubleUtils.ClampToUInt64((double)arg);
                    return string.Format(CultureInfo.CurrentCulture, "0x{0:X16}", value);
                }

                return string.Format(CultureInfo.CurrentCulture, "0x{0:x}", arg);
            }

            if (format == "N0")
                return string.Format(CultureInfo.CurrentCulture, "{0:N0}", arg);
            if (format == "F0")
                return string.Format(CultureInfo.CurrentCulture, "{0:F0}", arg);

            throw new InvalidOperationException($"Unsupported format '{format}'.");
        }
    }

    [DefaultFormat("sN")]
    [SupportedFormat(1, "sN", "Seconds", "s")]
    [SupportedFormat(2, "mN", "Milliseconds", "ms")]
    [SupportedFormat(3, "uN", "Microseconds", "\x00b5s")]
    [SupportedFormat(4, "nN", "Nanoseconds", "ns")]
    internal class TimePointFormatProvider : IFormatProvider, ICustomFormatter
    {
        public object GetFormat(Type formatType) { return this; }

        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (arg == null)
                return string.Empty;

            if (arg is TimePoint) {
                var timePoint = (TimePoint)arg;
                return TimePointFormatter.ToString(timePoint.ToNanoseconds, format, formatProvider);
            }

            return arg.ToString();
        }
    }
}
