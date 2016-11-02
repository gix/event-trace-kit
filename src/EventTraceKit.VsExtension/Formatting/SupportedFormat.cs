namespace EventTraceKit.VsExtension.Formatting
{
    using System.Globalization;

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
}
