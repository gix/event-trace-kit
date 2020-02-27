namespace EventTraceKit.EventTracing.Schema
{
    using System.Diagnostics;
    using EventTraceKit.EventTracing.Support;

    [DebuggerDisplay("{Source}")]
    public sealed class EventAttribute : SourceItem
    {
        public EventAttribute(string source)
        {
            Source = source ?? throw new System.ArgumentNullException(nameof(source));

            int sep = source.IndexOf('=');
            if (sep == -1)
                throw new System.ArgumentException(nameof(source));

            string name = source.Substring(0, sep);
            string value = source.Substring(sep + 1);

            if (value.StartsWith("\""))
                value = value.Substring(1, value.Length - 2).Replace("\"\"", "\"");

            Name = name;
            Value = value;
        }

        public string Source { get; }
        public string Name { get; }
        public string Value { get; }
    }
}
