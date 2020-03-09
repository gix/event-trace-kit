namespace EventTraceKit.EventTracing.Schema
{
    using System;
    using System.Diagnostics;
    using EventTraceKit.EventTracing.Support;

    [DebuggerDisplay("{Name}({Id,h}) = '{Value}'")]
    public sealed class LocalizedString : SourceItem
    {
        public const uint UnusedId = uint.MaxValue;

        public LocalizedString(LocatedRef<string> name, LocatedRef<string> value)
            : this(name, value, UnusedId)
        {
        }

        public LocalizedString(LocatedRef<string> name, string value, uint id)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Id = id;
        }

        public LocatedRef<string> Name { get; }
        public LocatedRef<string> Value { get; }
        public uint Id { get; set; }
        public LocatedRef<string> Symbol { get; set; }

        public bool Imported { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
