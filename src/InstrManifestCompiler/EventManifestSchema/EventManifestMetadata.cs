namespace InstrManifestCompiler.EventManifestSchema
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using InstrManifestCompiler.EventManifestSchema.Base;
    using InstrManifestCompiler.Support;

    internal sealed class EventManifestMetadata : IEventManifestMetadata
    {
        private readonly Dictionary<QName, XmlType> xmlTypeMap = new Dictionary<QName, XmlType>();
        private readonly Dictionary<QName, InType> inTypeMap = new Dictionary<QName, InType>();

        public EventManifestMetadata()
        {
            XmlTypes = new List<XmlType>();
            InTypes = new List<InType>();
            OutTypes = new List<OutType>();

            Channels = new ChannelCollection();
            Levels = new LevelCollection();
            Opcodes = new OpcodeCollection();
            Tasks = new TaskCollection();
            Keywords = new KeywordCollection();
            NamedQueries = new PatternMapCollection();

            Strings = new LocalizedStringCollection();
        }

        public List<XmlType> XmlTypes { get; private set; }
        public List<InType> InTypes { get; private set; }
        public List<OutType> OutTypes { get; private set; }

        public ChannelCollection Channels { get; private set; }
        public LevelCollection Levels { get; private set; }
        public OpcodeCollection Opcodes { get; private set; }
        public TaskCollection Tasks { get; private set; }
        public KeywordCollection Keywords { get; private set; }
        public PatternMapCollection NamedQueries { get; private set; }
        public LocalizedStringCollection Strings { get; private set; }

        public LocalizedString GetString(string stringRef)
        {
            if (stringRef == null)
                throw new ArgumentNullException(nameof(stringRef));

            if (IsStringTableRef(stringRef)) {
                string name = stringRef.Substring(9, stringRef.Length - 10);
                return Strings.GetByName(name);
            }

            if (IsMessageRef(stringRef)) {
                // FIXME
                string symbolId = stringRef.Substring(5, stringRef.Length - 6);
                throw new NotImplementedException("$(mc.symbolid) references are not implemented yet.");
            }

            throw new ArgumentException("Invalid message ref.");
        }

        public XmlType GetXmlType(QName name)
        {
            if (!xmlTypeMap.TryGetValue(name, out var type))
                throw new InternalException("Unknown XmlType '{0}'", name);
            return type;
        }

        public XmlType GetXmlType(byte value)
        {
            XmlType type = xmlTypeMap.FirstOrDefault(p => p.Value.Value == value).Value;
            if (type == null)
                throw new InternalException("No XmlType with value '{0}' available.", value);
            return type;
        }

        public bool TryGetXmlType(QName name, out XmlType type)
        {
            return xmlTypeMap.TryGetValue(name, out type);
        }

        public InType GetInType(QName name)
        {
            if (!inTypeMap.TryGetValue(name, out var type))
                throw new InternalException("Unknown InType '{0}'", name);
            return type;
        }

        public InType GetInType(byte value)
        {
            InType type = inTypeMap.FirstOrDefault(p => p.Value.Value == value).Value;
            if (type == null)
                throw new InternalException("No InType with value '{0}' available.", value);
            return type;
        }

        public void AddXmlType(XmlType type)
        {
            xmlTypeMap.Add(type.Name, type);
            XmlTypes.Add(type);
        }

        public void AddInType(InType inType)
        {
            inTypeMap.Add(inType.Name, inType);
            InTypes.Add(inType);
        }

        private static bool IsStringTableRef(string stringRef)
        {
            return stringRef.StartsWith("$(string.") && stringRef.EndsWith(")");
        }

        private static bool IsMessageRef(string stringRef)
        {
            return stringRef.StartsWith("$(mc.") && stringRef.EndsWith(")");
        }
    }
}
