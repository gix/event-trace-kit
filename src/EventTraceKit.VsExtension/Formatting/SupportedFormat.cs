namespace EventTraceKit.VsExtension.Formatting
{
    using System.Globalization;

    public struct SupportedFormat
    {
        public SupportedFormat(string format, string name, string unit = null, string helpText = null)
        {
            Format = format;
            Name = name;
            Unit = unit;
            HelpText = helpText;
        }

        public bool HasValue => Format != null;
        public string Format { get; }
        public string Unit { get; }
        public string Name { get; }
        public string HelpText { get; }

        public string Label
        {
            get
            {
                if (Unit == null)
                    return Name;

                return string.Format(
                    CultureInfo.CurrentCulture, "{0} ({1})", Name, Unit);
            }
        }
    }
}
