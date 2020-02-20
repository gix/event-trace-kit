namespace EventTraceKit.EventTracing.Schema
{
    using System;
    using EventTraceKit.EventTracing.Schema.Base;
    using EventTraceKit.EventTracing.Support;

    public sealed class DataProperty : Property
    {
        public DataProperty(LocatedRef<string> name, InType inType)
            : base(name)
        {
            InType = inType ?? throw new ArgumentNullException(nameof(inType));
        }

        public override PropertyKind Kind => PropertyKind.Data;

        public InType InType { get; }
        public XmlType OutType { get; set; }
        public Map Map { get; set; }

        public override uint BinXmlType
        {
            get
            {
                uint type = InType.Value;
                if (Count.IsSpecified)
                    type |= InType.ArrayFlag;
                return type;
            }
        }

        public override PropertyFlags GetFlags()
        {
            PropertyFlags flags = base.GetFlags();
            if (InType.Name == WinEventSchema.SecurityId)
                flags &= ~(PropertyFlags.FixedLength);
            return flags;
        }
    }
}
