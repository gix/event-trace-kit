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
        public SupportedFormatAttribute(int ordinal, string format, string description, string units = null)
        {
            Ordinal = ordinal;
            Format = format;
            Description = description;
            Units = units;
        }

        public int Ordinal { get; set; }
        public string Format { get; set; }
        public string Description { get; set; }
        public string Units { get; set; }
        public string HelpText { get; set; }

        public SupportedFormat GetSupportedFormat() =>
            new SupportedFormat(Format, Description, unit: Units, helpText: HelpText);
    }
}
