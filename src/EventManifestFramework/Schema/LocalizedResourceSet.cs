namespace EventManifestFramework.Schema
{
    using System.Globalization;
    using EventManifestFramework.Support;

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
