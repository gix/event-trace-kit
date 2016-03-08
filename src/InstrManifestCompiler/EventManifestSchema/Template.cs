namespace InstrManifestCompiler.EventManifestSchema
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Xml.Linq;
    using InstrManifestCompiler.Support;

    [DebuggerDisplay("{Id}")]
    public sealed class Template : ProviderItem
    {
        public Template(RefValue<string> id)
        {
            Contract.Requires<ArgumentNullException>(id != null);
            Id = id;
            Properties = new PropertyCollection();
        }

        public RefValue<string> Name { get; set; }
        public RefValue<string> Id { get; private set; }
        public PropertyCollection Properties { get; private set; }
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
