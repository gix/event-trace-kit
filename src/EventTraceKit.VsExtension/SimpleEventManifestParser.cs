namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using EventTraceKit.VsExtension.Views;
    using Extensions;

    public class SimpleEventManifestParser : IDisposable
    {
        public static readonly XNamespace EventManifestSchemaNamespace =
            "http://schemas.microsoft.com/win/2004/08/events";

        public static readonly XNamespace EventSchemaNamespace =
            "http://schemas.microsoft.com/win/2004/08/events/event";

        public static readonly XNamespace WinEventSchemaNamespace =
            "http://manifests.microsoft.com/win/2004/08/windows/events";

        private readonly string manifestFilePath;
        private readonly XmlReader reader;
        private readonly XDocument document;
        private readonly XmlNamespaceManager xnsMgr;

        public SimpleEventManifestParser(string manifestFilePath)
        {
            this.manifestFilePath = manifestFilePath;
            reader = XmlReader.Create(manifestFilePath);
            document = XDocument.Load(reader);

            xnsMgr = new XmlNamespaceManager(reader.NameTable ?? new NameTable());
            xnsMgr.AddNamespace("e", EventManifestSchemaNamespace.NamespaceName);
            xnsMgr.AddNamespace("w", WinEventSchemaNamespace.NamespaceName);
        }

        public void Dispose()
        {
            reader.Dispose();
        }

        public IEnumerable<EventProviderViewModel> ReadProviders()
        {
            const string providerXPath = "e:instrumentationManifest/e:instrumentation/e:events/e:provider";
            foreach (var providerElem in document.XPathSelectElements(providerXPath, xnsMgr)) {
                var provider = ReadProvider(providerElem);
                if (provider != null)
                    yield return provider;
            }
        }

        public EventProviderViewModel ReadProvider(XElement providerElem)
        {
            string name = providerElem.Attribute("name").AsString();
            string symbol = providerElem.Attribute("symbol").AsString();
            Guid? id = providerElem.Attribute("guid").AsGuid();
            if (id == null)
                return null;

            var provider = new EventProviderViewModel(
                id.Value, name ?? symbol ?? $"<id:{id.Value}>", manifestFilePath);

            return provider;
        }
    }
}
