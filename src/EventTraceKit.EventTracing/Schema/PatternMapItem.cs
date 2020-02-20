namespace EventTraceKit.EventTracing.Schema
{
    using System;
    using EventTraceKit.EventTracing.Support;

    public sealed class PatternMapItem : SourceItem
    {
        public PatternMapItem(PatternMap map, LocatedRef<string> name, LocatedRef<string> value)
        {
            Map = map ?? throw new ArgumentNullException(nameof(map));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value;
        }

        public LocatedRef<string> Name { get; }
        public LocatedRef<string> Value { get; }
        public PatternMap Map { get; }
    }
}
