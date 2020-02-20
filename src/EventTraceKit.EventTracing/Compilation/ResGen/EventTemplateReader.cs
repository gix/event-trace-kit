namespace EventTraceKit.EventTracing.Compilation.ResGen
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;
    using EventTraceKit.EventTracing.Compilation.BinXml;
    using EventTraceKit.EventTracing.Compilation.ResGen.Crimson;
    using EventTraceKit.EventTracing.Compilation.Support;
    using EventTraceKit.EventTracing.Internal.Extensions;
    using EventTraceKit.EventTracing.Internal.Native;
    using EventTraceKit.EventTracing.Schema;
    using EventTraceKit.EventTracing.Schema.Base;
    using EventTraceKit.EventTracing.Support;
    using PropertyFlags = Crimson.PropertyFlags;

    public sealed class EventTemplateReader
    {
        private readonly IXmlNamespaceResolver nsr;
        private readonly Dictionary<long, object> objectMap = new Dictionary<long, object>();
        private readonly Dictionary<uint, LocalizedString> strings =
            new Dictionary<uint, LocalizedString>();

        private readonly IDiagnostics diags;
        private readonly IEventManifestMetadata metadata;

        private Dictionary<uint, Message> messageMap;

        public EventTemplateReader(IDiagnostics diags, IEventManifestMetadata metadata = null)
        {
            this.diags = diags ?? throw new ArgumentNullException(nameof(diags));
            this.metadata = metadata;

            var nsmgr = new XmlNamespaceManager(new NameTable());
            nsmgr.AddNamespace("win", WinEventSchema.Namespace);
            nsr = nsmgr;
        }

        public IEnumerable<Message> ReadMessageTable(Stream input)
        {
            using (var reader = IO.CreateBinaryReader(input))
                return ReadMessageTable(reader);
        }

        public EventManifest ReadWevtTemplate(Stream input, IEnumerable<Message> messages)
        {
            if (messages != null)
                messageMap = messages.ToDictionary(m => m.Id);

            try {
                using (var reader = new BinaryReader(input))
                    return ReadCrimBlock(reader);
            } finally {
                messageMap = null;
            }
        }

        private IEnumerable<Message> ReadMessageTable(BinaryReader r)
        {
            var messages = new List<Message>();

            uint blockCount = r.ReadUInt32();

            var resourceBlocks = new List<MESSAGE_RESOURCE_BLOCK>();
            for (uint i = 0; i < blockCount; ++i) {
                uint lowId = r.ReadUInt32();
                uint highId = r.ReadUInt32();
                uint offset = r.ReadUInt32();
                resourceBlocks.Add(
                    new MESSAGE_RESOURCE_BLOCK {
                        LowId = lowId,
                        HighId = highId,
                        Offset = offset
                    });
            }

            for (int i = 0; i < resourceBlocks.Count; i++) {
                var block = resourceBlocks[i];
                r.BaseStream.Position = block.Offset;

                for (uint id = block.LowId; id <= block.HighId; ++id) {
                    if (r.BaseStream.Position == r.BaseStream.Length)
                        continue;

                    ushort length = r.ReadUInt16();
                    ushort flags = r.ReadUInt16();
                    var bytes = new byte[length - 4];
                    r.Read(bytes, 0, bytes.Length);

                    Encoding encoding;
                    if ((flags & NativeMethods.MESSAGE_RESOURCE_UNICODE) != 0)
                        encoding = Encoding.Unicode;
                    else
                        encoding = Encoding.ASCII;
                    string text = encoding.GetString(bytes).TrimEnd('\r', '\n', '\0');

                    messages.Add(new Message(id, text));
                }
            }

            return messages;
        }

        private string FormatMagic(uint magic)
        {
            return Encoding.ASCII.GetString(BitConverter.GetBytes(magic)).Replace('\0', ' ');
        }

        private void ReadMagic(BinaryReader reader, uint expected)
        {
            uint magic = reader.ReadUInt32();
            if (magic != expected) {
                throw new InternalException(
                    "Invalid magic 0x{0:X8} ({1}), expected 0x{2:X8} ({3}) at byte offset 0x{4:X}.",
                    magic,
                    FormatMagic(magic),
                    CrimsonTags.CRIM,
                    FormatMagic(expected),
                    reader.BaseStream.Position - 4);
            }
        }

        private EventManifest ReadCrimBlock(BinaryReader r)
        {
            objectMap.Clear();
            strings.Clear();

            ReadMagic(r, CrimsonTags.CRIM);

            uint length = r.ReadUInt32();
            ushort major = r.ReadUInt16();
            ushort minor = r.ReadUInt16();
            uint providerCount = r.ReadUInt32();

            var providerEntries = new List<Tuple<Guid, uint>>();
            for (uint i = 0; i < providerCount; ++i) {
                var providerGuid = r.ReadGuid();
                var wevtOffset = r.ReadUInt32();
                providerEntries.Add(Tuple.Create(providerGuid, wevtOffset));
            }

            var manifest = new EventManifest();
            foreach (var offset in providerEntries) {
                r.BaseStream.Position = offset.Item2;
                var provider = ReadWevtBlock(offset.Item1, r);
                manifest.Providers.Add(provider);
            }

            var resourceSet = new LocalizedResourceSet(CultureInfo.GetCultureInfo("en-US"));
            resourceSet.Strings.AddRange(strings.Values);
            manifest.AddResourceSet(resourceSet);

            return manifest;
        }

        private Provider ReadWevtBlock(Guid providerId, BinaryReader r)
        {
            string name = string.Format(
                CultureInfo.InvariantCulture, "Provider_{0:N}", providerId);

            ReadMagic(r, CrimsonTags.WEVT);
            uint length = r.ReadUInt32();
            uint messageId = r.ReadUInt32();
            uint count = r.ReadUInt32();

            var lists = new List<ProviderListOffset>();
            for (uint i = 0; i < count; ++i) {
                var plo = new ProviderListOffset();
                plo.Type = (EventFieldKind)r.ReadUInt32();
                plo.Offset = r.ReadUInt32();
                lists.Add(plo);
            }

            var provider = new Provider(name, Located.Create(providerId), name);
            provider.Message = ResolveMessage(messageId);

            var allOpcodes = new List<Tuple<ushort, Opcode>>();

            foreach (var list in lists) {
                r.BaseStream.Position = list.Offset;
                switch (list.Type) {
                    case EventFieldKind.Level:
                        provider.Levels.AddRange(ReadLevels(r));
                        break;
                    case EventFieldKind.Task:
                        provider.Tasks.AddRange(ReadTasks(r));
                        break;
                    case EventFieldKind.Opcode:
                        allOpcodes.AddRange(ReadOpcodes(r));
                        break;
                    case EventFieldKind.Keyword:
                        provider.Keywords.AddRange(ReadKeywords(r));
                        break;
                    case EventFieldKind.Event:
                        provider.Events.AddRange(ReadEvents(r));
                        break;
                    case EventFieldKind.Channel:
                        provider.Channels.AddRange(ReadChannels(r));
                        break;
                    case EventFieldKind.Maps:
                        provider.Maps.AddRange(ReadMaps(r));
                        break;
                    case EventFieldKind.Template:
                        provider.Templates.AddRange(ReadTemplates(r));
                        break;
                    case EventFieldKind.Filter:
                        provider.Filters.AddRange(ReadFilters(r));
                        break;
                    default: {
                        }
                        break;
                }
            }

            provider.Opcodes.AddRange(allOpcodes.Where(x => x.Item1 == 0).Select(x => x.Item2));
            foreach (var taskSpecificOpcode in allOpcodes.Where(x => x.Item1 != 0)) {
                var task = provider.Tasks.First(x => x.Value == taskSpecificOpcode.Item1);
                task.Opcodes.Add(taskSpecificOpcode.Item2);
            }

            return provider;
        }

        private List<Event> ReadEvents(BinaryReader r)
        {
            var events = new List<Event>();

            ReadMagic(r, CrimsonTags.EVNT);
            uint length = r.ReadUInt32();
            uint count = r.ReadUInt32();
            uint unk1 = r.ReadUInt32();

            for (uint i = 0; i < count; ++i) {
                long offset = r.BaseStream.Position;
                var desc = ReadEventDescriptor(r);
                uint messageId = r.ReadUInt32();
                uint templateOffset = r.ReadUInt32();
                uint opcodeOffset = r.ReadUInt32();
                uint levelOffset = r.ReadUInt32();
                uint taskOffset = r.ReadUInt32();
                uint keywordCount = r.ReadUInt32();
                uint keywordsOffset = r.ReadUInt32();
                uint channelOffset = r.ReadUInt32();
                uint[] keywordOffsets = ReadUInt32At(r, keywordsOffset, keywordCount);

                var @event = new Event(Located.Create((uint)desc.EventId), Located.Create(desc.Version));
                @event.Channel = GetObject<Channel>(channelOffset);
                @event.Level = GetObject<Level>(levelOffset);
                @event.Task = GetObject<Task>(taskOffset);
                @event.Opcode = GetObject<Opcode>(opcodeOffset);
                foreach (var keywordOffset in keywordOffsets) {
                    var keyword = GetObject<Keyword>(keywordOffset);
                    if (keyword != null)
                        @event.Keywords.Add(keyword);
                }
                @event.Template = GetObject<Template>(templateOffset);
                @event.Message = ResolveMessage(messageId);
                MarkObject(offset, @event);
                events.Add(@event);
            }

            return events;
        }

        private EventDescriptor ReadEventDescriptor(BinaryReader r)
        {
            var id = r.ReadUInt16();
            var version = r.ReadByte();
            var channel = r.ReadByte();
            var level = r.ReadByte();
            var opcode = r.ReadByte();
            var task = r.ReadUInt16();
            var keywords = r.ReadUInt64();

            return new EventDescriptor {
                EventId = id,
                Version = version,
                Channel = channel,
                Level = level,
                Opcode = opcode,
                Task = task,
                Keywords = (long)keywords
            };
        }

        private List<Channel> ReadChannels(BinaryReader r)
        {
            var channels = new List<Channel>();
            var channelEntries = new List<ChannelEntry>();

            ReadMagic(r, CrimsonTags.CHAN);
            var length = r.ReadUInt32();
            var count = r.ReadUInt32();

            for (uint i = 0; i < count; ++i) {
                long offset = r.BaseStream.Position;
                var flags = r.ReadUInt32();
                var nameOffset = r.ReadUInt32();
                var value = r.ReadUInt32();
                var messageId = r.ReadUInt32();
                string name = ReadStringAt(r, nameOffset);
                channelEntries.Add(
                    new ChannelEntry {
                        Flags = (ChannelFlags)flags,
                        Name = name,
                        Value = (byte)value,
                        MessageId = messageId,
                    });

                var channel = new Channel(name, Located.CreateStruct(ChannelType.Analytic));
                if (value != 0)
                    channel.Value = (byte)value;
                channel.Message = ResolveMessage(messageId);
                MarkObject(offset, channel);
                channels.Add(channel);
            }

            foreach (var channel in channelEntries) {
            }

            return channels;
        }

        private List<Level> ReadLevels(BinaryReader r)
        {
            var levels = new List<Level>();
            var levelEntries = new List<LevelEntry>();

            ReadMagic(r, CrimsonTags.LEVL);
            var length = r.ReadUInt32();
            var count = r.ReadUInt32();

            for (uint i = 0; i < count; ++i) {
                long offset = r.BaseStream.Position;
                var value = r.ReadUInt32();
                var messageId = r.ReadUInt32();
                var nameOffset = r.ReadUInt32();
                string name = ReadStringAt(r, nameOffset);
                levelEntries.Add(
                    new LevelEntry {
                        Value = value,
                        MessageId = messageId,
                        Name = name,
                    });

                var level = new Level(QName.Parse(name, nsr), Located.Create((byte)value));
                level.Message = ResolveMessage(messageId);
                MarkObject(offset, level);
                levels.Add(level);
            }

            foreach (var level in levelEntries) {
            }

            return levels;
        }

        private List<Task> ReadTasks(BinaryReader r)
        {
            var tasks = new List<Task>();
            var taskEntries = new List<TaskEntry>();

            ReadMagic(r, CrimsonTags.TASK);
            var length = r.ReadUInt32();
            var count = r.ReadUInt32();

            for (uint i = 0; i < count; ++i) {
                long offset = r.BaseStream.Position;
                var value = r.ReadUInt32();
                var messageId = r.ReadUInt32();
                var guid = r.ReadGuid();
                var nameOffset = r.ReadUInt32();
                string name = ReadStringAt(r, nameOffset);
                taskEntries.Add(
                    new TaskEntry {
                        Value = value,
                        MessageId = messageId,
                        Guid = guid,
                        Name = name,
                    });

                var task = new Task(QName.Parse(name, nsr), Located.Create((ushort)value));
                task.Guid = guid == Guid.Empty ? null : (Guid?)guid;
                task.Message = ResolveMessage(messageId);
                MarkObject(offset, task);
                tasks.Add(task);
            }

            foreach (var task in taskEntries) {
            }

            return tasks;
        }

        private List<Tuple<ushort, Opcode>> ReadOpcodes(BinaryReader r)
        {
            var opcodes = new List<Tuple<ushort, Opcode>>();
            var opcodeEntries = new List<OpcodeEntry>();

            ReadMagic(r, CrimsonTags.OPCO);
            var length = r.ReadUInt32();
            var count = r.ReadUInt32();

            for (uint i = 0; i < count; ++i) {
                long offset = r.BaseStream.Position;
                var taskId = r.ReadUInt16();
                var value = r.ReadUInt16();
                var messageId = r.ReadUInt32();
                var nameOffset = r.ReadUInt32();
                string name = ReadStringAt(r, nameOffset);
                opcodeEntries.Add(
                    new OpcodeEntry {
                        TaskId = taskId,
                        Value = value,
                        MessageId = messageId,
                        Name = name,
                    });

                var opcode = new Opcode(QName.Parse(name, nsr), Located.Create((byte)value));
                opcode.Message = ResolveMessage(messageId);
                MarkObject(offset, opcode);
                opcodes.Add(Tuple.Create(taskId, opcode));
            }

            foreach (var opcode in opcodeEntries) {
            }

            return opcodes;
        }

        private List<Keyword> ReadKeywords(BinaryReader r)
        {
            var keywords = new List<Keyword>();
            var keywordEntries = new List<KeywordEntry>();

            ReadMagic(r, CrimsonTags.KEYW);
            var length = r.ReadUInt32();
            var count = r.ReadUInt32();

            for (uint i = 0; i < count; ++i) {
                long offset = r.BaseStream.Position;
                var mask = r.ReadUInt64();
                var messageId = r.ReadUInt32();
                var nameOffset = r.ReadUInt32();
                string name = ReadStringAt(r, nameOffset);
                keywordEntries.Add(
                    new KeywordEntry {
                        Mask = mask,
                        MessageId = messageId,
                        Name = name,
                    });

                var keyword = new Keyword(QName.Parse(name, nsr), Located.Create(mask));
                keyword.Message = ResolveMessage(messageId);
                MarkObject(offset, keyword);
                keywords.Add(keyword);
            }

            foreach (var keyword in keywordEntries) {
            }

            return keywords;
        }

        private List<Map> ReadMaps(BinaryReader r)
        {
            var maps = new List<Map>();
            var mapEntries = new List<MapEntry>();

            ReadMagic(r, CrimsonTags.MAPS);
            var length = r.ReadUInt32();
            var count = r.ReadUInt32();

            var offsets = new uint[count];
            for (uint i = 0; i < count; ++i)
                offsets[i] = r.ReadUInt32();

            foreach (var offset in offsets) {
                r.BaseStream.Position = offset;
                var tuple = ReadMap(r);
                maps.Add(tuple.Item1);
                mapEntries.Add(tuple.Item2);
            }

            foreach (var m in mapEntries) {
            }

            return maps;
        }

        private Tuple<Map, MapEntry> ReadMap(BinaryReader r)
        {
            long offset = r.BaseStream.Position;

            uint magic = r.ReadUInt32();
            if (magic != CrimsonTags.VMAP && magic != CrimsonTags.BMAP)
                throw new InternalException("Unknown map magic {0}", magic);

            uint length = r.ReadUInt32();
            uint nameOffset = r.ReadUInt32();
            uint flags = r.ReadUInt32();
            uint count = r.ReadUInt32();

            var itemEntries = new List<MapItemEntry>();
            for (uint i = 0; i < count; ++i) {
                var value = r.ReadUInt32();
                var messageId = r.ReadUInt32();
                itemEntries.Add(new MapItemEntry { Value = value, MessageId = messageId });
            }

            if (r.BaseStream.Position != offset + length)
                throw new IOException();

            string name = ReadStringAt(r, nameOffset);
            var mapEntry = new MapEntry {
                Flags = (MapFlags)flags,
                Name = name,
                Items = itemEntries
            };
            var map = magic == CrimsonTags.VMAP ? new ValueMap(name) : (Map)new BitMap(name);
            foreach (var itemEntry in itemEntries) {
                var value = Located.Create(itemEntry.Value);
                var ls = ResolveMessage(itemEntry.MessageId);

                MapItem item;
                if (magic == CrimsonTags.VMAP)
                    item = new ValueMapItem(
                        (ValueMap)map, value, ls);
                else
                    item = new BitMapItem((BitMap)map, value, ls);
                map.Items.Add(item);
            }

            MarkObject(offset, map);

            return Tuple.Create(map, mapEntry);
        }

        private List<Template> ReadTemplates(BinaryReader r)
        {
            var templates = new List<Template>();

            ReadMagic(r, CrimsonTags.TTBL);
            uint length = r.ReadUInt32();
            uint count = r.ReadUInt32();

            for (uint i = 0; i < count; ++i)
                templates.Add(ReadTemplate(r));

            return templates;
        }

        private Template ReadTemplate(BinaryReader r)
        {
            long offset = r.BaseStream.Position;

            ReadMagic(r, CrimsonTags.TEMP);
            uint length = r.ReadUInt32();
            uint paramCount = r.ReadUInt32();
            uint dataCount = r.ReadUInt32();
            uint propertyOffset = r.ReadUInt32();
            uint flags = r.ReadUInt32();
            Guid guid = r.ReadGuid();

            var types = new List<BinXmlType>();
            XDocument doc = BinXmlReader.Read(r.BaseStream, types);
            r.BaseStream.Position = propertyOffset;

            string id = string.Format(
                CultureInfo.InvariantCulture, "template{0:X16}", offset);
            var template = new Template(id);

            long structPropertyOffset = propertyOffset + paramCount * Marshal.SizeOf<PropertyEntry>();
            for (uint i = 0; i < paramCount; ++i) {
                r.BaseStream.Position = propertyOffset + i * Marshal.SizeOf<PropertyEntry>();
                template.Properties.Add(ReadProperty(r, ref structPropertyOffset, true));
            }

            ResolvePropertyRefs(in guid, template.Properties);

            r.BaseStream.Position = offset + length;

            MarkObject(offset, template);
            return template;
        }

        private void ResolvePropertyRefs(
            in Guid templateId, IReadOnlyList<Property> properties)
        {
            foreach (var property in properties) {
                ResolvePropertyRef(in templateId, property, property.Length, properties);
                ResolvePropertyRef(in templateId, property, property.Count, properties);

                if (property.Kind == PropertyKind.Struct)
                    ResolvePropertyRefs(in templateId, ((StructProperty)property).Properties);
            }
        }

        private void ResolvePropertyRef(
            in Guid templateId, Property property, IPropertyNumber number,
            IReadOnlyList<Property> properties)
        {
            if (number.DataPropertyIndex == -1)
                return;

            int index = number.DataPropertyIndex;
            if (index < 0 || index >= properties.Count) {
                diags.Report(
                    DiagnosticSeverity.Warning,
                    "Property {0}:{1} ({2}) references invalid property {3}. Template has only {4} properties.",
                    templateId, property.Index, property.Name, number.DataPropertyIndex, properties.Count);
                return;
            }
            if (!(properties[index] is DataProperty)) {
                diags.Report(
                    DiagnosticSeverity.Warning,
                    "Property {0}:{1} ({2}) references invalid property {3}. Property is not a data property.",
                    templateId, property.Index, property.Name, number.DataPropertyIndex);
                return;
            }

            var propertyRef = (DataProperty)properties[index];
            number.SetVariable(index, propertyRef.Name, propertyRef);
        }

        private Property ReadProperty(
            BinaryReader r, ref long structPropertyOffset, bool isNested)
        {
            var flags = (PropertyFlags)r.ReadUInt32();
            if ((flags & PropertyFlags.Struct) != 0) {
                ushort firstPropertyIndex = r.ReadUInt16();
                ushort propertyCount = r.ReadUInt16();

                uint mapOffset = r.ReadUInt32();
                ushort count = r.ReadUInt16();
                ushort length = r.ReadUInt16();
                uint nameOffset = r.ReadUInt32();
                string name = ReadStringAt(r, nameOffset).TrimEnd('\0');

                var property = new StructProperty(name);

                if ((flags & PropertyFlags.VarLength) != 0)
                    property.Length.SetVariable(refPropertyIndex: length);
                if ((flags & PropertyFlags.VarCount) != 0)
                    property.Count.SetVariable(refPropertyIndex: count);

                if ((flags & PropertyFlags.FixedLength) != 0)
                    property.Length.SetFixed(length);
                if ((flags & PropertyFlags.FixedCount) != 0)
                    property.Count.SetFixed(count);

                r.BaseStream.Position = structPropertyOffset;
                long dummy = -1;
                for (int i = 0; i < propertyCount; ++i)
                    property.Properties.Add(ReadProperty(r, ref dummy, true));
                structPropertyOffset = r.BaseStream.Position;

                return property;
            } else {
                byte inputType = r.ReadByte();
                byte outputType = r.ReadByte();
                uint padding = r.ReadUInt16();

                uint mapOffset = r.ReadUInt32();
                ushort count = r.ReadUInt16();
                ushort length = r.ReadUInt16();
                uint nameOffset = r.ReadUInt32();
                string name = ReadStringAt(r, nameOffset).TrimEnd('\0');

                DataProperty property;
                if (metadata != null) {
                    property = new DataProperty(name, metadata.GetInType(inputType));
                    property.OutType = metadata.GetXmlType(outputType);
                } else {
                    property = new DataProperty(name, new InType(new QName("Dummy"), 0, null));
                }

                property.Map = GetObject<Map>(mapOffset);

                if ((flags & PropertyFlags.VarLength) != 0)
                    property.Length.SetVariable(refPropertyIndex: length);
                if ((flags & PropertyFlags.VarCount) != 0)
                    property.Count.SetVariable(refPropertyIndex: length);

                if ((flags & PropertyFlags.FixedLength) != 0)
                    property.Length.SetFixed(length);
                if ((flags & PropertyFlags.FixedCount) != 0)
                    property.Count.SetFixed(length);

                return property;
            }
        }

        private List<Filter> ReadFilters(BinaryReader r)
        {
            var filters = new List<Filter>();
            var filterEntries = new List<FilterEntry>();

            ReadMagic(r, CrimsonTags.FLTR);
            uint length = r.ReadUInt32();
            uint count = r.ReadUInt32();
            uint junk = r.ReadUInt32();

            for (uint i = 0; i < count; ++i) {
                long offset = r.BaseStream.Position;
                byte value = r.ReadByte();
                byte version = r.ReadByte();
                ushort padding = r.ReadUInt16();
                uint messageId = r.ReadUInt32();
                uint nameOffset = r.ReadUInt32();
                uint templateOffset = r.ReadUInt32();
                string name = ReadStringAt(r, nameOffset);
                filterEntries.Add(
                    new FilterEntry {
                        Name = name,
                        Value = value,
                        Version = version,
                        MessageId = messageId,
                        TemplateOffset = templateOffset,
                    });

                var filter = new Filter(
                    QName.Parse(name, nsr), Located.Create(value), Located.Create(version));
                filter.Message = ResolveMessage(messageId);
                MarkObject(offset, filter);
                filters.Add(filter);
            }

            foreach (var filter in filterEntries) {
            }

            return filters;
        }

        private string ReadStringAt(BinaryReader r, long offset)
        {
            Stream stream = r.BaseStream;
            long old = stream.Position;
            try {
                stream.Position = offset;
                uint byteCount = r.ReadUInt32();
                return r.ReadPaddedString(Encoding.Unicode, byteCount - 4);
            } finally {
                stream.Position = old;
            }
        }

        private uint[] ReadUInt32At(BinaryReader r, long offset, uint count)
        {
            Stream stream = r.BaseStream;
            long old = stream.Position;
            try {
                stream.Position = offset;
                var values = new uint[count];
                for (uint i = 0; i < count; ++i)
                    values[i] = r.ReadUInt32();
                return values;
            } finally {
                stream.Position = old;
            }
        }

        private LocalizedString ResolveMessage(uint messageId)
        {
            if (messageId == LocalizedString.UnusedId)
                return null;

            if (strings.TryGetValue(messageId, out var ls))
                return ls;

            string name = string.Format(CultureInfo.InvariantCulture, "str{0:X8}", messageId);
            string text = LookupMessage(messageId) ?? "{unresolved}";

            ls = new LocalizedString(name, text, messageId);
            strings[messageId] = ls;
            return ls;
        }

        private string LookupMessage(uint messageId)
        {
            if (messageMap != null && messageMap.TryGetValue(messageId, out Message message))
                return message.Text;
            return null;
        }

        private void MarkObject<T>(long offset, T obj)
        {
            objectMap.Add(offset, obj);
        }

        private T GetObject<T>(uint offset) where T : class
        {
            if (offset == 0)
                return null;

            if (!objectMap.TryGetValue(offset, out var obj))
                throw new InternalException(
                    "Unread object for offset '{0}' requested.", offset);

            if (!(obj is T value))
                throw new InternalException("Null object in object map.");

            return value;
        }

        private class FilterEntry
        {
            public byte Value;
            public byte Version;
            public uint MessageId;
            public uint TemplateOffset;
            public string Name;

            public override string ToString()
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Filter({0}, Value={1}, Version={2}, Message=0x{3:X}, Template=0x{4:X})",
                    Name, Value, Version, MessageId, TemplateOffset);
            }
        }

        private class ChannelEntry
        {
            public ChannelFlags Flags;
            public string Name;
            public byte Value;
            public uint MessageId;

            public override string ToString()
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Channel({0}, Flags={1}, Value={2}, Message=0x{3:X})",
                    Name, Flags, Value, MessageId);
            }
        }

        private class OpcodeEntry
        {
            public ushort TaskId;
            public ushort Value;
            public uint MessageId;
            public string Name;

            public override string ToString()
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Opcode({0}, Value={1}, Message=0x{2:X}, TaskId={3})",
                    Name, Value, MessageId, TaskId);
            }
        }

        private class LevelEntry
        {
            public uint Value;
            public uint MessageId;
            public string Name;

            public override string ToString()
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Level({0}, Value={1}, Message=0x{2:X})",
                    Name, Value, MessageId);
            }
        }

        private class TaskEntry
        {

            public uint Value;
            public uint MessageId;
            public Guid Guid;
            public string Name;

            public override string ToString()
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Task({0}, Value={1}, Message=0x{2:X}, Guid={3})",
                    Name, Value, MessageId, Guid);
            }
        }

        private class KeywordEntry
        {
            public ulong Mask;
            public uint MessageId;
            public string Name;

            public override string ToString()
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Keyword({0}, Mask={1}, Message=0x{2:X})",
                    Name, Mask, MessageId);
            }
        }

        private class EventDescriptor
        {
            public ushort EventId;
            public byte Version;
            public byte Channel;
            public byte Level;
            public byte Opcode;
            public int Task;
            public long Keywords;

            public override string ToString()
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Evt({0}, V={1}, C={2}, L={3}, O={4}, T={5}, K=0x{6:X})",
                    EventId, Version, Channel, Level, Opcode, Task, Keywords);
            }
        }

        private class MapEntry
        {
            public MapFlags Flags;
            public string Name;
            public List<MapItemEntry> Items;

            public override string ToString()
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Map({0}, Flags={1}, Items={{ {2} }})",
                    Name, Flags, string.Join(", ", Items.Select(e => e.ToString())));
            }
        }

        private class MapItemEntry
        {
            public uint Value;
            public uint MessageId;

            public override string ToString()
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "({0}, Message=0x{1:X})",
                    Value, MessageId);
            }
        }
    }
}
