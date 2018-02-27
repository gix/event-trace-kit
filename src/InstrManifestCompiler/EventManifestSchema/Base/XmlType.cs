namespace InstrManifestCompiler.EventManifestSchema.Base
{
    using System.Xml;
    using System.Xml.Linq;
    using InstrManifestCompiler.Extensions;

    public sealed class XmlType : BaseType
    {
        public XmlType(QName name, uint value, string symbol)
            : base(name, value, symbol)
        {
        }

        public static XmlType Create(XElement elem, IXmlNamespaceResolver resolver)
        {
            QName name = elem.GetQName("name");
            uint value = elem.GetUInt32("value");
            string symbol = elem.GetCSymbol("symbol");

            return new XmlType(name, value, symbol);
        }
    }
}
