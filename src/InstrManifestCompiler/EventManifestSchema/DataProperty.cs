namespace InstrManifestCompiler.EventManifestSchema
{
    using System;
    using InstrManifestCompiler.EventManifestSchema.Base;
    using InstrManifestCompiler.Support;

    public sealed class DataProperty : Property
    {
        public DataProperty(RefValue<string> name, InType inType)
            : base(name)
        {
            InType = inType ?? throw new ArgumentNullException(nameof(inType));
        }

        public override PropertyKind Kind => PropertyKind.Data;

        public InType InType { get; }
        public XmlType OutType { get; set; }
        public IMap Map { get; set; }

        public override uint BinXmlType
        {
            get
            {
                uint type = InType.Value;
                if (Count.IsSpecified)
                    type |= (uint)BinXml.BinXmlType.ArrayFlag;
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
