namespace InstrManifestCompiler.EventManifestSchema
{
    using System;
    using System.Diagnostics;
    using System.Xml.Linq;
    using InstrManifestCompiler.Support;

    [DebuggerDisplay("{" + nameof(Id) + "}")]
    public sealed class Template : ProviderItem
    {
        public Template(RefValue<string> id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            Id = id;
            Properties = new PropertyCollection();
        }

        public RefValue<string> Name { get; set; }
        public RefValue<string> Id { get; }
        public PropertyCollection Properties { get; }
        public XElement UserData { get; set; }

        public override string ToString()
        {
            return Id;
        }

        public override void Accept(IProviderItemVisitor visitor)
        {
            visitor.VisitTemplate(this);
        }
    }
}
