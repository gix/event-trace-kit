namespace EventTraceKit.EventTracing.Internal.Extensions
{
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.Schema;

    internal static class XmlExtensions
    {
        public static void Add(
            this XmlSchemaSet set, XNamespace targetNamespace, string schemaUri)
        {
            set.Add(targetNamespace.NamespaceName, schemaUri);
        }

        public static void AddNamespace(
            this XmlNamespaceManager nsmgr, string prefix, XNamespace ns)
        {
            nsmgr.AddNamespace(prefix, ns.NamespaceName);
        }
    }
}
