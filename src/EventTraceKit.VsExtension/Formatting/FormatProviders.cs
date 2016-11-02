namespace EventTraceKit.VsExtension.Formatting
{
    using System;

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DefaultFormatAttribute : Attribute
    {
        public DefaultFormatAttribute(string defaultFormat)
        {
            DefaultFormat = defaultFormat;
        }

        public string DefaultFormat { get; }
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
}
