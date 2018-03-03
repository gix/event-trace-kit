namespace EventManifestFramework.Schema
{
    using System;
    using System.Diagnostics;
    using EventManifestFramework.Support;

    [DebuggerDisplay("{Symbol}: {Value}")]
    public abstract class MapItem : SourceItem
    {
        protected MapItem(LocatedVal<uint> value, LocalizedString message)
        {
            Value = value;
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        protected MapItem(LocatedVal<uint> value, LocalizedString message,
                          LocatedRef<string> symbol)
            : this(value, message)
        {
            Symbol = symbol;
        }

        public abstract Map Map { get; }
        public abstract MapKind Kind { get; }

        public LocatedVal<uint> Value { get; }
        public LocalizedString Message { get; }
        public LocatedRef<string> Symbol { get; }
    }
}
