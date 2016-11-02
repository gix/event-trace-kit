namespace EventTraceKit.VsExtension.Formatting
{
    using System;
    using System.Linq;
    using Utilities;

    public sealed class DefaultFormatProviderSource : IFormatProviderSource
    {
        private readonly UnboundedCache<Type, IFormatProvider> formatProviderCache =
            new UnboundedCache<Type, IFormatProvider>(CreateFormatProvider);

        public IFormatProvider GetFormatProvider(Type type)
        {
            return formatProviderCache[type];
        }

        public string GetFormat(IFormatProvider formatProvider, string format)
        {
            if (formatProvider == null)
                return format;

            if (format != null && formatProvider.SupportedFormats().Any(x => x.Format == format))
                return format;

            var formatAttribute = (DefaultFormatAttribute)Attribute.GetCustomAttribute(
                formatProvider.GetType(), typeof(DefaultFormatAttribute));
            return formatAttribute?.DefaultFormat;
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
}