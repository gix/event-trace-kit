namespace EventTraceKit.EventTracing.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Xml;
    using System.Xml.Linq;

    internal sealed class XElementNamespaceResolver : IXmlNamespaceResolver
    {
        private readonly XElement element;

        public XElementNamespaceResolver(XElement element)
        {
            this.element = element;
        }

        public IDictionary<string, string> GetNamespacesInScope(XmlNamespaceScope scope)
        {
            throw new NotSupportedException();
        }

        public string LookupNamespace(string prefix)
        {
            var ns = element.GetNamespaceOfPrefix(prefix);
            return ns != null ? ns.NamespaceName : null;
        }

        public string LookupPrefix(string namespaceName)
        {
            return element.GetPrefixOfNamespace(namespaceName);
        }
    }
}
