namespace InstrManifestCompiler.EventManifestSchema.Base
{
    using System.Xml;
    using System.Xml.Linq;
    using InstrManifestCompiler.Extensions;

    internal sealed class OutType
    {
        public OutType(QName xmlType, bool isDefault)
        {
            XmlType = xmlType;
            IsDefault = isDefault;
        }

        public QName XmlType { get; set; }
        public bool IsDefault { get; set; }

        public static OutType Create(XElement elem, IXmlNamespaceResolver resolver)
        {
            QName xmlType = elem.GetQName("xmlType");
            bool isDefault = elem.GetOptionalBool("default", false);

            return new OutType(xmlType, isDefault);
        }
    }
}
