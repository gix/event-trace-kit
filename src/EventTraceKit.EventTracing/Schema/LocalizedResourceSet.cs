namespace EventTraceKit.EventTracing.Schema
{
    using System.Globalization;
    using EventTraceKit.EventTracing.Support;

    public sealed class LocalizedResourceSet : SourceItem
    {
        public LocalizedResourceSet(CultureInfo culture)
        {
            Culture = culture;
        }

        public CultureInfo Culture { get; }
        public LocalizedStringCollection Strings { get; } =
            new LocalizedStringCollection();

        public bool ContainsName(string name)
        {
            return Strings.GetByName(name) != null;
        }
    }
}
