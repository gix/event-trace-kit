namespace EventTraceKit.EventTracing.Schema
{
    using System.Diagnostics;
    using EventTraceKit.EventTracing.Support;

    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public sealed class ValueMap : Map
    {
        public ValueMap(LocatedRef<string> name)
            : base(name)
        {
        }

        public ValueMap(LocatedRef<string> name, LocatedRef<string> symbol)
            : base(name, symbol)
        {
        }

        public override MapKind Kind => MapKind.ValueMap;

        public override void Accept(IProviderItemVisitor visitor)
        {
            visitor.VisitValueMap(this);
        }
    }
}
