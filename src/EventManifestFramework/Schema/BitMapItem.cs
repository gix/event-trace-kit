namespace EventManifestFramework.Schema
{
    using System;
    using EventManifestFramework.Support;

    public sealed class BitMapItem : MapItem
    {
        private readonly BitMap map;

        public BitMapItem(BitMap map, LocatedVal<uint> value, LocalizedString message)
            : base(value, message)
        {
            this.map = map ?? throw new ArgumentNullException(nameof(map));
        }

        public BitMapItem(
            BitMap map, LocatedVal<uint> value, LocatedRef<string> symbol,
            LocalizedString message)
            : base(value, message, symbol)
        {
            this.map = map ?? throw new ArgumentNullException(nameof(map));
        }

        public override MapKind Kind => MapKind.BitMap;
        public override Map Map => map;
    }
}
