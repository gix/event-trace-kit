namespace InstrManifestCompiler.EventManifestSchema
{
    using System;
    using System.Diagnostics.Contracts;
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
            Contract.Requires<ArgumentNullException>(name != null);
            Name = name;
            Items = new MapItemCollection<IMapItem>();
        }

        protected Map(RefValue<string> name, RefValue<string> symbol)
            : this(name)
        {
            Symbol = symbol;
        }

        public abstract MapKind Kind { get; }
        public RefValue<string> Name { get; private set; }
        public RefValue<string> Symbol { get; set; }

        public MapItemCollection<IMapItem> Items { get; private set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
