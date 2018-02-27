namespace InstrManifestCompiler.EventManifestSchema
{
    using System;
    using System.Diagnostics.Contracts;
    using InstrManifestCompiler.EventManifestSchema.Base;
    using InstrManifestCompiler.Support;

    public sealed class DataProperty : Property
    {
        public DataProperty(RefValue<string> name, InType inType)
            : base(name)
        {
            Contract.Requires<ArgumentNullException>(inType != null);
            InType = inType;
        }

        public override PropertyKind Kind
        {
            get { return PropertyKind.Data; }
        }

        public InType InType { get; private set; }
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
