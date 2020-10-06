namespace EventTraceKit.EventTracing.Tests.Schema
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;
    using EventTraceKit.EventTracing.Schema;

    public abstract class ValidationTest
    {
        internal readonly DiagnosticsCollector diags;
        internal readonly EventManifestParser parser;

        protected ValidationTest()
        {
            diags = new DiagnosticsCollector();
            parser = EventManifestParser.CreateWithWinmeta(diags);
        }

        public static IEnumerable<object[]> ValidQNames
        {
            get
            {
                yield return new object[] { "Name1" };
                yield return new object[] { "test:Name1" };
                yield return new object[] { "a" };
                yield return new object[] { "A" };
                yield return new object[] { "_Name_1" };
                yield return new object[] { "Ab0123456789_" };
            }
        }

        public static IEnumerable<object[]> InvalidQNames
        {
            get
            {
                yield return new object[] { "" };
                yield return new object[] { " " };
                yield return new object[] { "1" };
                yield return new object[] { "undef:Name" };
            }
        }

        public static IEnumerable<object[]> ValidSymbolNames
        {
            get
            {
                yield return new object[] { "" };
                yield return new object[] { "Sym_1" };
                yield return new object[] { "_Sym_1" };
                yield return new object[] { "a" };
                yield return new object[] { "Ab0123456789" };
            }
        }

        public static IEnumerable<object[]> InvalidSymbolNames
        {
            get
            {
                yield return new object[] { " " };
                yield return new object[] { " A " };
                yield return new object[] { "1" };
                yield return new object[] { "1A" };
            }
        }

        public static IEnumerable<object[]> ValidMessageRefs
        {
            get
            {
                yield return new object[] { "$(string.msg.1)" };
            }
        }

        public static IEnumerable<object[]> InvalidMessageRefs
        {
            get
            {
                yield return new object[] { " " };
                yield return new object[] { "text" };
                yield return new object[] { "$(string.1" };
                yield return new object[] { "$(string.undefined)" };
            }
        }

        protected MemoryStream CreateInput(string type, ref XElement elem1)
        {
            var provider = CreateProvider();
            var typeElem = provider.Element(type);
            if (typeElem == null)
                provider.Add(E(type, elem1));
            else
                typeElem.AddFirst(elem1);

            var ns = EventManifestSchema.Namespace;
            var stream = CreateInputStream(provider.Document);
            var doc = LoadUnchecked(stream);
            elem1 = doc
                .Element(ns + "instrumentationManifest")
                .Element(ns + "instrumentation")
                .Element(ns + "events")
                .Element(ns + "provider")
                .Element(ns + type)
                .Elements().First();

            stream.Position = 0;
            return stream;
        }

        protected MemoryStream CreateInput(ref XElement provider)
        {
            var ns = EventManifestSchema.Namespace;
            var stream = CreateInputStream(provider.Document);
            var doc = LoadUnchecked(stream);
            provider = doc
                .Element(ns + "instrumentationManifest")
                .Element(ns + "instrumentation")
                .Element(ns + "events")
                .Element(ns + "provider");

            stream.Position = 0;
            return stream;
        }

        protected MemoryStream CreateInput(string type, ref XElement elem1, ref XElement elem2)
        {
            var provider = CreateProvider();
            var typeElem = provider.Element(type);
            if (typeElem == null)
                provider.Add(E(type, elem1, elem2));
            else
                typeElem.AddFirst(elem1, elem2);

            var ns = EventManifestSchema.Namespace;
            var stream = CreateInputStream(provider.Document);
            var doc = LoadUnchecked(stream);
            var elems = doc
                .Element(ns + "instrumentationManifest")
                .Element(ns + "instrumentation")
                .Element(ns + "events")
                .Element(ns + "provider")
                .Element(ns + type)
                .Elements().ToArray();
            elem1 = elems[0];
            elem2 = elems[1];

            stream.Position = 0;
            return stream;
        }

        protected XDocument CreateManifest()
        {
            XNamespace ns = EventManifestSchema.Namespace;

            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", "yes"),
                new XElement(ns + "instrumentationManifest",
                    new XAttribute(XNamespace.Xmlns + "win", WinEventSchema.Namespace),
                    new XAttribute(XNamespace.Xmlns + "test", "urn:uuid:23787556-8924-4834-80C0-45C883D0501B"),
                    new XElement(ns + "instrumentation",
                        new XElement(ns + "events")),
                    new XElement(ns + "localization",
                        new XElement(ns + "resources",
                            new XAttribute("culture", "en-US"),
                            new XElement(ns + "stringTable")))));

            return doc;
        }

        protected XElement CreateProvider(params object[] content)
        {
            XNamespace ns = EventManifestSchema.Namespace;

            var provider = new XElement(ns + "provider",
                new XAttribute("name", "P01"),
                new XAttribute("guid", Guid.Empty.ToString("B")),
                new XAttribute("symbol", "P01"),
                new XAttribute("resourceFileName", "f.dll"),
                new XAttribute("messageFileName", "f.dll"),
                new XElement(ns + "templates",
                    new XElement(ns + "template",
                        new XAttribute("tid", "TestTemplate1"),
                        new XElement(ns + "data",
                            new XAttribute("name", "Field1"),
                            new XAttribute("inType", "win:UInt8"))),
                    new XElement(ns + "template",
                        new XAttribute("tid", "TestTemplate2"),
                        new XElement(ns + "data",
                            new XAttribute("name", "Field1"),
                            new XAttribute("inType", "win:UInt8")))));

            provider.Add(content);

            var doc = CreateManifest();

            doc.Element(ns + "instrumentationManifest")
                .Element(ns + "instrumentation")
                .Element(ns + "events")
                .Add(provider);

            doc.Element(ns + "instrumentationManifest")
                .Element(ns + "localization")
                .Element(ns + "resources")
                .Element(ns + "stringTable")
                .Add(new XElement(ns + "string",
                    new XAttribute("id", "msg.1"),
                    new XAttribute("value", "text1")));

            return provider;
        }

        protected static XElement E(string name, params object[] content)
        {
            return new XElement(EventManifestSchema.Namespace + name, content);
        }

        protected static XAttribute A(XName name, object value)
        {
            return new XAttribute(name, value);
        }

        protected static MemoryStream CreateInputStream(XDocument doc)
        {
            var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false, true), 0x400, true))
                WriteUnchecked(doc, writer);
            stream.Position = 0;
            return stream;
        }

        private static void WriteUnchecked(XDocument doc, TextWriter writer)
        {
            var settings = new XmlWriterSettings {
                CheckCharacters = false,
                Indent = true
            };
            using var xmlWriter = XmlWriter.Create(writer, settings);
            doc.WriteTo(xmlWriter);
        }

        private static XDocument LoadUnchecked(Stream stream)
        {
            var settings = new XmlReaderSettings {
                CheckCharacters = false
            };
            using var reader = XmlReader.Create(stream, settings, "<stdin>");
            return XDocument.Load(reader, LoadOptions.SetBaseUri | LoadOptions.SetLineInfo);
        }
    }
}
