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

    public class SafeXamlSerializer
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
                CloseInput = false
            };

            using (var reader = XmlReader.Create(inputStream, settings))
                return LoadImpl(reader);
        }

        public object Load(XmlReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            return LoadImpl(reader);
        }

        public void Save(object element, Stream outputStream)
        {
            var settings = new XmlWriterSettings {
                CheckCharacters = false,
                CloseOutput = false,
                Indent = true,
                NewLineOnAttributes = false,
                OmitXmlDeclaration = true,
                Encoding = Encoding.UTF8
            };

            using (var writer = XmlWriter.Create(outputStream, settings))
                SaveImpl(element, writer);
        }

        public void Save(object element, XmlWriter writer)
        {
            SaveImpl(element, writer);
        }

        private object LoadImpl(XmlReader reader)
        {
            var xamlReader = new XamlXmlReader(reader, schemaContext);
            return XamlServices.Load(xamlReader);
        }

        private void SaveImpl(object element, XmlWriter writer)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            if (IsSequenceType(element.GetType()))
                throw new ArgumentException(
                    "Root serialized element must not be a sequence type", nameof(element));

            var xamlWriter = new XamlXmlWriter(writer, schemaContext);
            XamlServices.Save(xamlWriter, element);
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
    }
}
