namespace EventTraceKit.EventTracing.Schema
{
    using System;
    using EventTraceKit.EventTracing.Support;

    public enum MapKind
    {
        None = 0,
        BitMap = 1,
        ValueMap = 2,
    }

    public abstract class Map : ProviderItem
    {
        protected Map(LocatedRef<string> name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        protected Map(LocatedRef<string> name, LocatedRef<string> symbol)
            : this(name)
        {
            Symbol = symbol;
        }

        public abstract MapKind Kind { get; }
        public LocatedRef<string> Name { get; }
        public LocatedRef<string> Symbol { get; }

        public MapItemCollection<MapItem> Items { get; } =
            new MapItemCollection<MapItem>();

        public override string ToString()
        {
            return Name;
        }
    }
}
