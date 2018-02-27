namespace InstrManifestCompiler.EventManifestSchema
{
    using InstrManifestCompiler.Support;

    public sealed class StructProperty : Property
    {
        public StructProperty(RefValue<string> name)
            : base(name)
        {
            Properties = new PropertyCollection();
        }

        public PropertyCollection Properties { get; private set; }

        public override PropertyKind Kind
        {
            get { return PropertyKind.Struct; }
        }

        public override uint BinXmlType
        {
            get
            {
                var type = (uint)BinXml.BinXmlType.Binary;
                if (Count.IsSpecified)
                    type |= (uint)BinXml.BinXmlType.ArrayFlag;
                return type;
            }
        }

        public override PropertyFlags GetFlags()
        {
            return base.GetFlags() | PropertyFlags.Struct;
        }
    }
}
