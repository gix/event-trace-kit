namespace EventTraceKit.EventTracing.Schema
{
    using EventTraceKit.EventTracing.Support;

    public sealed class BitMap : Map
    {
        public BitMap(LocatedRef<string> name)
            : base(name)
        {
        }

        public BitMap(LocatedRef<string> name, LocatedRef<string> symbol)
            : base(name, symbol)
        {
        }

        public override MapKind Kind => MapKind.BitMap;

        public override void Accept(IProviderItemVisitor visitor)
        {
            visitor.VisitBitMap(this);
        }
    }
}
