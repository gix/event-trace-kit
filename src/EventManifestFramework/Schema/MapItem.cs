namespace EventManifestFramework.Schema
{
    using System;
    using System.Diagnostics;
    using EventManifestFramework.Support;

    public interface IMapItem : ISourceItem
    {
        IMap Map { get; }
        MapKind Kind { get; }
        LocatedRef<string> Symbol { get; }
        LocatedVal<uint> Value { get; }
        LocalizedString Message { get; }
    }

    [DebuggerDisplay("{Symbol}: {Value}")]
    public abstract class MapItem : SourceItem, IMapItem
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

        public abstract IMap Map { get; }
        public abstract MapKind Kind { get; }

        public LocatedVal<uint> Value { get; }
        public LocalizedString Message { get; }
        public LocatedRef<string> Symbol { get; }
    }
}
