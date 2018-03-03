namespace EventManifestFramework.Schema
{
    using System;
    using EventManifestFramework.Support;

    public enum MapKind
    {
        None = 0,
        BitMap = 1,
        ValueMap = 2,
    }

    public interface IMap
    {
        MapKind Kind { get; }
        LocatedRef<string> Name { get; }
        LocatedRef<string> Symbol { get; }
        MapItemCollection<IMapItem> Items { get; }
    }

    public abstract class Map : ProviderItem, IMap
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

        public MapItemCollection<IMapItem> Items { get; } =
            new MapItemCollection<IMapItem>();

        public override string ToString()
        {
            return Name;
        }
    }
}
