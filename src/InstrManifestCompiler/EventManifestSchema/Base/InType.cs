namespace InstrManifestCompiler.EventManifestSchema.Base
{
    using System.Collections.Generic;
    using System.Xml;
    using System.Xml.Linq;

    public sealed class InType : BaseType
    {
        public InType(QName name, uint value, string symbol)
            : base(name, value, symbol)
        {
            OutTypes = new List<XmlType>();
        }

        public XmlType DefaultOutType { get; set; }
        public IList<XmlType> OutTypes { get; private set; }

        public static InType Create(XElement elem, IXmlNamespaceResolver resolver)
        {
            QName name = elem.GetQName("name");
            uint value = elem.GetUInt32("value");
            string symbol = elem.GetCSymbol("symbol");

            return new InType(name, value, symbol);
        }
    }
}
