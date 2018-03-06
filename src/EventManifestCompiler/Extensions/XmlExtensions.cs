namespace EventManifestCompiler.Extensions
{
    using System.Xml;
    using System.Xml.Linq;

    internal static class XmlExtensions
    {
        public static void AddNamespace(
            this XmlNamespaceManager nsmgr, string prefix, XNamespace ns)
        {
            nsmgr.AddNamespace(prefix, ns.NamespaceName);
        }
    }
}
