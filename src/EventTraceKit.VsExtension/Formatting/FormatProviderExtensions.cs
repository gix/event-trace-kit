namespace EventTraceKit.VsExtension.Formatting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public static class FormatProviderExtensions
    {
        public static string DefaultFormat(
            this IFormatProvider formatProvider)
        {
            return formatProvider?.GetType()
                .GetCustomAttribute<DefaultFormatAttribute>(true)?.DefaultFormat;
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
                .Select(x => x.GetSupportedFormat())
                .ToArray();
        }
    }
}
