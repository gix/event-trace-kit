namespace InstrManifestCompiler.EventManifestSchema
{
    using System;
    using InstrManifestCompiler.Support;

    public enum MapKind
    {
        None = 0,
        BitMap = 1,
        ValueMap = 2,
    }

    public interface IMap
    {
        MapKind Kind { get; }
        RefValue<string> Name { get; }
        RefValue<string> Symbol { get; }
        MapItemCollection<IMapItem> Items { get; }
    }

    public abstract class Map : ProviderItem, IMap
    {
        protected Map(RefValue<string> name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        protected Map(RefValue<string> name, RefValue<string> symbol)
            : this(name)
        {
            Symbol = symbol;
        }

        public abstract MapKind Kind { get; }
        public RefValue<string> Name { get; }
        public RefValue<string> Symbol { get; }

        public MapItemCollection<IMapItem> Items { get; } =
            new MapItemCollection<IMapItem>();

        public override string ToString()
        {
            return Name;
        }
    }
}
