namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using Collections;
    using Extensions;

    public class SimpleInstrumentationManifestParser : IDisposable
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

        public SimpleInstrumentationManifestParser(string manifestFilePath)
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

        public IEnumerable<TraceProviderDescriptorViewModel> ReadProviders()
        {
            const string providerXPath = "e:instrumentationManifest/e:instrumentation/e:events/e:provider";
            foreach (var providerElem in document.XPathSelectElements(providerXPath, xnsMgr)) {
                var provider = ReadProvider(providerElem);
                if (provider != null)
                    yield return provider;
            }
        }

        public TraceProviderDescriptorViewModel ReadProvider(XElement providerElem)
        {
            string name = providerElem.Attribute("name").AsString();
            string symbol = providerElem.Attribute("symbol").AsString();
            Guid? id = providerElem.Attribute("guid").AsGuid();
            if (id == null)
                return null;

            var provider = new TraceProviderDescriptorViewModel(
                id.Value, name ?? symbol ?? $"<id:{id.Value}>");

            provider.Manifest = manifestFilePath;
            provider.Events.AddRange(ReadEvents(providerElem));

            return provider;
        }

        public IEnumerable<TraceEventDescriptorViewModel> ReadEvents(
            XElement providerElem)
        {
            foreach (var eventElem in providerElem.XPathSelectElements("e:events/e:event", xnsMgr)) {
                var eventInfo = ReadEvent(eventElem);
                if (eventInfo != null)
                    yield return eventInfo;
            }
        }

        public TraceEventDescriptorViewModel ReadEvent(
            XElement eventElem)
        {
            string symbol = eventElem.Attribute("symbol").AsString();
            ushort? id = eventElem.Attribute("value").AsUShort();
            byte version = eventElem.Attribute("version").AsByte().GetValueOrDefault(0);
            var channelToken = eventElem.Attribute("channel").AsString();
            var levelName = eventElem.Attribute("level").AsString();
            var opcodeName = eventElem.Attribute("opcode").AsString();
            var taskName = eventElem.Attribute("task").AsString();
            var keywordList = eventElem.Attribute("keywords").AsString();

            if (id == null)
                return null;

            return new TraceEventDescriptorViewModel(id.Value, version, symbol ?? $"<id:{id.Value}>") {
                Level = levelName,
                Channel = channelToken,
                Task = taskName,
                Opcode = opcodeName,
                Keywords = keywordList
            };
        }
    }
}
