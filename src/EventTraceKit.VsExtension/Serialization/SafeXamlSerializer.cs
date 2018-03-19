namespace EventTraceKit.VsExtension.Serialization
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Xaml;
    using System.Xml;

    public interface IUntypedXamlSerializer
    {
        void Save(object element, XmlWriter writer);
        void Save(IEnumerable<object> elements, XmlWriter writer);
        object Load(XmlReader reader);
        IReadOnlyList<object> LoadMultiple(XmlReader reader);
    }

    public class SafeXamlSerializer : IUntypedXamlSerializer
    {
        private readonly SafeXamlSchemaContext schemaContext;

        public SafeXamlSerializer()
            : this(Enumerable.Empty<Assembly>())
        {
        }

        public SafeXamlSerializer(params Assembly[] safeAssemblies)
            : this((IEnumerable<Assembly>)safeAssemblies)
        {
        }

        public SafeXamlSerializer(IEnumerable<Assembly> safeAssemblies)
        {
            schemaContext = new SafeXamlSchemaContext(safeAssemblies);
        }

        public void AddKnownType(Type type)
        {
            schemaContext.AddKnownType(type);
        }

        public object Load(Stream inputStream)
        {
            if (inputStream == null)
                throw new ArgumentNullException(nameof(inputStream));

            var settings = new XmlReaderSettings {
                CheckCharacters = false,
                CloseInput = false,
                ConformanceLevel = ConformanceLevel.Document,
            };

            using (var reader = XmlReader.Create(inputStream, settings))
                return LoadObject(reader);
        }

        public object Load(XmlReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            return LoadObject(reader);
        }

        public IReadOnlyList<object> LoadMultiple(Stream inputStream)
        {
            if (inputStream == null)
                throw new ArgumentNullException(nameof(inputStream));

            var settings = new XmlReaderSettings {
                CheckCharacters = false,
                CloseInput = false,
                ConformanceLevel = ConformanceLevel.Fragment,
            };

            using (var reader = XmlReader.Create(inputStream, settings))
                return LoadObjects(reader);
        }

        public IReadOnlyList<object> LoadMultiple(XmlReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            return LoadObjects(reader);
        }

        public void Save(object element, Stream outputStream)
        {
            var settings = new XmlWriterSettings {
                CheckCharacters = false,
                CloseOutput = false,
                Indent = true,
                NewLineOnAttributes = false,
                OmitXmlDeclaration = true,
                Encoding = Encoding.UTF8,
                ConformanceLevel = ConformanceLevel.Document,
            };

            using (var writer = XmlWriter.Create(outputStream, settings))
                SaveObject(element, writer);
        }

        public void Save(object element, XmlWriter writer)
        {
            SaveObject(element, writer);
        }

        public void Save(IEnumerable<object> elements, Stream outputStream)
        {
            var settings = new XmlWriterSettings {
                CheckCharacters = false,
                CloseOutput = false,
                Indent = true,
                NewLineOnAttributes = false,
                OmitXmlDeclaration = true,
                Encoding = Encoding.UTF8,
                ConformanceLevel = ConformanceLevel.Fragment,
            };

            using (var writer = XmlWriter.Create(outputStream, settings))
                SaveObjects(elements, writer);
        }

        public void Save(IEnumerable<object> elements, XmlWriter writer)
        {
            SaveObjects(elements, writer);
        }

        private object LoadObject(XmlReader reader)
        {
            using (var xamlReader = new XamlXmlReader(reader, schemaContext))
                return XamlServices.Load(xamlReader);
        }

        private IReadOnlyList<object> LoadObjects(XmlReader reader)
        {
            var values = new List<object>();
            while (!reader.EOF && reader.Read() && SkipWhitespace(reader)
                   && reader.NodeType == XmlNodeType.Element) {
                values.Add(LoadObject(reader.ReadSubtree()));
            }

            return values;
        }

        private static bool SkipWhitespace(XmlReader reader)
        {
            while (reader.NodeType == XmlNodeType.Whitespace) {
                if (!reader.Read())
                    return false;
            }

            return true;
        }

        private void SaveObject(object element, XmlWriter writer)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            if (IsSequenceType(element.GetType()))
                throw new ArgumentException(
                    "Root serialized element must not be a sequence type", nameof(element));

            var xamlWriter = new XamlXmlWriter(writer, schemaContext);
            XamlServices.Transform(
                new XamlObjectReader(element, xamlWriter.SchemaContext),
                xamlWriter, false);
            // Do not dispose XamlXmlWriter because it unconditionally closes the
            // base XmlWriter.
        }

        private void SaveObjects(IEnumerable<object> elements, XmlWriter writer)
        {
            if (elements == null)
                throw new ArgumentNullException(nameof(elements));

            foreach (var element in elements)
                SaveObject(element, writer);
        }

        private bool IsSequenceType(Type type)
        {
            return typeof(IList).IsAssignableFrom(type);
        }
    }

    public static class SafeXamlSerializerExtensions
    {
        public static T Load<T>(this SafeXamlSerializer serializer, Stream inputStream)
        {
            return (T)serializer.Load(inputStream);
        }

        public static T Load<T>(this SafeXamlSerializer serializer, XmlReader reader)
        {
            return (T)serializer.Load(reader);
        }

        public static IEnumerable<T> LoadMultiple<T>(
            this SafeXamlSerializer serializer, Stream inputStream)
        {
            return serializer.LoadMultiple(inputStream).Cast<T>();
        }

        public static IEnumerable<T> LoadMultiple<T>(this SafeXamlSerializer serializer, XmlReader reader)
        {
            return serializer.LoadMultiple(reader).Cast<T>();
        }
    }
}
