namespace EventManifestFramework.Schema.Base
{
    using System.Collections.Generic;
    using System.Xml;
    using System.Xml.Linq;
    using EventManifestFramework.Internal.Extensions;

    public sealed class InType : BaseType
    {
        internal const uint Binary = 14;
        internal const uint ArrayFlag = 0x80;

        public InType(QName name, uint value, string symbol)
            : base(name, value, symbol)
        {
        }

        public XmlType DefaultOutType { get; set; }
        public IList<XmlType> OutTypes { get; } = new List<XmlType>();

        public static InType Create(XElement elem, IXmlNamespaceResolver resolver)
        {
            QName name = elem.GetQName("name");
            uint value = elem.GetUInt32("value");
            string symbol = elem.GetCSymbol("symbol");

            return new InType(name, value, symbol);
        }
    }
}
