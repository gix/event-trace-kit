namespace EventTraceKit.EventTracing.Schema
{
    using EventTraceKit.EventTracing.Schema.Base;
    using EventTraceKit.EventTracing.Support;

    public sealed class StructProperty : Property
    {
        public StructProperty(LocatedRef<string> name)
            : base(name)
        {
        }

        public PropertyCollection Properties { get; } = new PropertyCollection();

        public override PropertyKind Kind => PropertyKind.Struct;

        public override uint BinXmlType
        {
            get
            {
                var type = InType.Binary;
                if (Count.IsSpecified)
                    type |= InType.ArrayFlag;
                return type;
            }
        }

        public override PropertyFlags GetFlags()
        {
            return base.GetFlags() | PropertyFlags.Struct;
        }
    }
}
