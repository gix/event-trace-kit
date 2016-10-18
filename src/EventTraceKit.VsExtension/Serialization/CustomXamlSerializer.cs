namespace EventTraceKit.VsExtension.Serialization
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Windows.Markup;
    using System.Xml;
    using Extensions;

    internal interface ISerializationExclusion
    {
        bool ExcludeFromSerialization { get; }
    }

    public interface ICustomXmlSerializationInit
    {
        void BeginInitSerialization();
        void EndInitSerialization();
    }

    public interface ICustomXmlSerializer
    {
        string SerializedName { get; }
        object Content { get; }
        bool ExcludeLocalizable { get; set; }

        IEnumerable<KeyValuePair<string, object>> GetNonContentPropertyValues();
        void WriteXmlAttributes(XmlWriter writer);
    }

    public interface ICustomXmlSerializable
    {
        ICustomXmlSerializer CreateSerializer();
        Type GetSerializedType();
    }

    public class CustomXamlSerializer
    {
        private readonly Dictionary<Type, Type> serializedTypeMap = new Dictionary<Type, Type>();
        private readonly Dictionary<string, string> namespacePrefix = new Dictionary<string, string>();
        private readonly Dictionary<string, string> namespaceAssembly = new Dictionary<string, string>();
        private readonly Dictionary<Type, Func<object, ICustomXmlSerializer>> typeToSurrogateCustomSerializerFactoryMap =
            new Dictionary<Type, Func<object, ICustomXmlSerializer>>();

        public void Serialize(object element, Stream stream)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            if (IsSequenceType(element.GetType()))
                throw new ArgumentException(
                    "Root serialized element must not be a sequence type", nameof(element));

            var settings = new XmlWriterSettings {
                CheckCharacters = false,
                CloseOutput = false,
                Indent = true,
                NewLineOnAttributes = false,
                OmitXmlDeclaration = true,
                Encoding = Encoding.UTF8
            };

            using (var writer = XmlWriter.Create(stream, settings))
                SerializeInternal(element, writer, true);
        }

        private void SerializeInternal(object element, XmlWriter writer, bool isRootElement)
        {
            if (element == null)
                return;

            var exclusion = element as ISerializationExclusion;
            if (exclusion != null && exclusion.ExcludeFromSerialization)
                return;

            //DependencyObject target = element as DependencyObject;
            //if (target != null & isRootElement)
            //    Versioning.UpdateSavedWith(target);

            Type serializedType = GetSerializedType(element);
            var serializer = TryGetSerializer(serializedType, element);

            var init = element as ICustomXmlSerializationInit;
            init?.BeginInitSerialization();

            try {
                if (IsSequenceType(serializedType))
                    SerializeSequence(element as IEnumerable, writer);
                else
                    SerializeComplex(element, writer, isRootElement, serializedType, serializer);
            } catch (Exception) {
                init = null;
                throw;
            } finally {
                init?.EndInitSerialization();
            }
        }

        private static bool IsContentProperty(
            PropertyInfo property, ContentPropertyAttribute attribute)
        {
            if (attribute == null)
                return false;
            return attribute.Name == property.Name;
        }

        private ICustomXmlSerializer TryGetSerializer(Type serializedType, object element)
        {
            ICustomXmlSerializable serializable = element as ICustomXmlSerializable;

            ICustomXmlSerializer serializer = null;

            Func<object, ICustomXmlSerializer> func;
            if (serializable == null && typeToSurrogateCustomSerializerFactoryMap.TryGetValue(serializedType, out func)) {
                serializer = func(element);
                if (serializer == null)
                    return null;

                //serializer.ExcludeLocalizable = this.ExcludeLocalizable;
                return serializer;
            }

            if (serializable != null) {
                serializer = serializable.CreateSerializer();
                if (serializer == null)
                    return null;

                //serializer.ExcludeLocalizable = this.ExcludeLocalizable;
            }

            return serializer;
        }

        private bool IsSequenceType(Type type)
        {
            return typeof(IList).IsAssignableFrom(type);
        }

        protected void WriteNamespaceDeclarations(XmlWriter writer)
        {
            writer.WriteAttributeString("xmlns", "x", null, "http://schemas.microsoft.com/winfx/2006/xaml");
            foreach (string str in namespacePrefix.Keys)
                writer.WriteAttributeString(
                    "xmlns", namespacePrefix[str], null,
                    GetClrNamespace(str, namespaceAssembly[str]));
        }

        private void SerializeSequence(IEnumerable sequence, XmlWriter writer)
        {
            foreach (object item in sequence)
                SerializeInternal(item, writer, false);
        }

        private string GetClrNamespace(Type type)
        {
            string name;
            if (namespaceAssembly.TryGetValue(type.Namespace, out name))
                return GetClrNamespace(type.Namespace, name);

            return GetClrNamespace(type.Namespace, type.Assembly.GetName().Name);
        }

        private static string GetClrNamespace(string namespaceName, string assemblyName)
        {
            return $"clr-namespace:{namespaceName};assembly={assemblyName}";
        }

        private void SerializeComplex(
            object element, XmlWriter writer, bool isRootElement, Type type,
            ICustomXmlSerializer serializer)
        {
            string prefix = GetPrefix(type);
            string ns = GetClrNamespace(type);

            if (prefix != null)
                writer.WriteStartElement(prefix, type.Name, ns);
            else
                writer.WriteStartElement(type.Name, ns);

            if (isRootElement)
                WriteNamespaceDeclarations(writer);

            object contentPropertyValue = null;
            IEnumerable<KeyValuePair<string, object>> nonContentPropertyValues;
            if (serializer != null) {
                serializer.WriteXmlAttributes(writer);
                nonContentPropertyValues = serializer.GetNonContentPropertyValues();
                contentPropertyValue = serializer.Content;
            } else {
                nonContentPropertyValues = GetChildPropertiesAndContent(
                    element, writer, type, ref contentPropertyValue);
            }

            foreach (var pair in nonContentPropertyValues) {
                string localName = type.Name + "." + pair.Key;
                if (prefix != null)
                    writer.WriteStartElement(prefix, localName, ns);
                else
                    writer.WriteStartElement(localName, ns);

                SerializeInternal(pair.Value, writer, false);
                writer.WriteEndElement();
            }

            if (contentPropertyValue != null)
                SerializeInternal(contentPropertyValue, writer, false);

            writer.WriteEndElement();
        }

        private static bool IsDefaultValue(PropertyInfo property, object value)
        {
            var attribute = property.GetCustomAttribute<DefaultValueAttribute>(true);
            if (attribute == null)
                return false;
            return value == attribute.Value || (value != null && value.Equals(attribute.Value));
        }

        private Dictionary<string, object> GetChildPropertiesAndContent(
            object element, XmlWriter writer, Type type,
            ref object contentPropertyValue)
        {
            var contentPropertyAttribute = type.GetCustomAttribute<ContentPropertyAttribute>(true);
            var properties = new Dictionary<string, object>();
            var bindingFlags = BindingFlags.Public | BindingFlags.Instance;

            foreach (PropertyInfo info in type.GetProperties(bindingFlags)) {
                if (!IsPropertySerializable(info))
                    continue;

                object value = info.GetValue(element, null);
                if (IsDefaultValue(info, value))
                    continue;

                if (IsContentProperty(info, contentPropertyAttribute)) {
                    contentPropertyValue = value;
                    continue;
                }

                var typeConverter = TypeDescriptor.GetConverter(value?.GetType() ?? info.PropertyType);
                if (typeConverter.CanConvertTo(typeof(string)) && typeConverter.CanConvertFrom(typeof(string))) {
                    writer.WriteStartAttribute(info.Name);
                    WriteAttributeValue(writer, typeConverter, value);
                    writer.WriteEndAttribute();
                } else {
                    properties[info.Name] = value;
                }
            }

            return properties;
        }

        private static void WriteAttributeValue(XmlWriter writer, TypeConverter converter, object value)
        {
            string text = value != null ? converter.ConvertToInvariantString(value) : null;
            if (text == null) {
                writer.WriteString("{");
                writer.WriteQualifiedName("Null", "http://schemas.microsoft.com/winfx/2006/xaml");
                writer.WriteString("}");
                return;
            }

            if (text.StartsWith("{", StringComparison.Ordinal))
                text = "{}" + text;
            writer.WriteString(text);
        }

        private bool IsPropertySerializable(PropertyInfo property)
        {
            var dsvAttrib = property.GetCustomAttribute<DesignerSerializationVisibilityAttribute>(true);
            if (dsvAttrib != null && dsvAttrib.Visibility == DesignerSerializationVisibility.Hidden)
                return false;

            //if (this.ExcludeLocalizable) {
            //    var localizable = property.TryGetAttribute<LocalizableAttribute>(true);
            //    if (localizable != null && localizable.IsLocalizable)
            //        return false;
            //}

            if (!property.CanRead)
                return false;
            if (!property.CanWrite)
                return IsSequenceType(property.PropertyType);

            return true;
        }

        private string GetPrefix(Type type)
        {
            string str;
            namespacePrefix.TryGetValue(type.Namespace, out str);
            return str;
        }

        private Type GetSerializedType(object element)
        {
            var serializable = element as ICustomXmlSerializable;
            if (serializable != null)
                return serializable.GetSerializedType();

            Type sourceType = element.GetType();

            return serializedTypeMap.GetOrAdd(
                sourceType,
                t => t.GetCustomAttribute<SerializedShapeAttribute>(true)?
                         .Shape ?? sourceType);
        }
    }
}
