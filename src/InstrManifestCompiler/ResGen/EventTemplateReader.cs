namespace InstrManifestCompiler.ResGen
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;
    using InstrManifestCompiler.EventManifestSchema;
    using InstrManifestCompiler.EventManifestSchema.Base;
    using InstrManifestCompiler.EventManifestSchema.BinXml;
    using InstrManifestCompiler.Extensions;
    using InstrManifestCompiler.Native;
    using InstrManifestCompiler.Support;

    internal sealed class EventTemplateReader
    {
        private readonly IXmlNamespaceResolver nsr;
        private readonly Dictionary<long, object> objectMap = new Dictionary<long, object>();
        private readonly Dictionary<uint, LocalizedString> strings =
            new Dictionary<uint, LocalizedString>();

        private readonly IDiagnostics diags;
        private readonly IEventManifestMetadata metadata;

        private TextWriter logWriter = Console.Out;
        private bool log;

        private Dictionary<uint, Message> messageMap;

        public EventTemplateReader(IDiagnostics diags, IEventManifestMetadata metadata = null)
        {
            Contract.Requires<ArgumentNullException>(diags != null);

            this.diags = diags;
            this.metadata = metadata;

            var nsmgr = new XmlNamespaceManager(new NameTable());
            nsmgr.AddNamespace("win", WinEventSchema.Namespace);
            nsr = nsmgr;
        }

        public TextWriter LogWriter
        {
            get { return logWriter; }
            set { logWriter = value ?? Console.Out; }
        }

        public void DumpMessageTable(string filename)
        {
            using (var input = File.OpenRead(filename))
                DumpMessageTable(input);
        }

        public void DumpMessageTable(Stream input)
        {
            log = true;
            try {
                using (var reader = IO.CreateBinaryReader(input))
                    ReadMessageTable(reader);
            } finally {
                log = false;
            }
        }

        public IEnumerable<Message> ReadMessageTable(Stream input)
        {
            using (var reader = IO.CreateBinaryReader(input))
                return ReadMessageTable(reader);
        }

        public void DumpWevtTemplate(string filename)
        {
            using (var input = File.OpenRead(filename))
                DumpWevtTemplate(input);
        }

        public void DumpWevtTemplate(Stream input)
        {
            log = true;
            try {
                using (var reader = new BinaryReader(input))
                    ReadCrimBlock(reader);
            } finally {
                log = false;
            }
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
            LogMessage("MESSAGE_RESOURCE_DATA ({0} blocks)", blockCount);

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
                LogMessage("  [{0}] MESSAGE_RESOURCE_BLOCK (0x{1:X}-0x{2:X})", i, block.LowId, block.HighId);
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

                    LogMessage("    0x{0:X8}: {1}", id, text);
                    messages.Add(new Message(id, text));
                }
            }

            return messages;
        }

        private void LogMessage(object value)
        {
            if (log)
                logWriter.WriteLine(value);
        }

        private void LogMessage(string format, params object[] args)
        {
            if (log)
                logWriter.WriteLine(format, args);
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
                LogMessage("ProviderGuid: {0}", offset.Item1);
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

            var offsets = new List<Tuple<uint, uint>>();
            for (uint i = 0; i < count; ++i) {
                uint type = r.ReadUInt32();
                uint offset = r.ReadUInt32();
                offsets.Add(Tuple.Create(type, offset));
            }

            var provider = new Provider(name, Value.Create(providerId), name);
            provider.Message = ResolveMessage(messageId);

            foreach (var pair in offsets) {
                uint type = pair.Item1;
                uint offset = pair.Item2;
                r.BaseStream.Position = offset;
                switch (type) {
                    case EventFieldKind.Level:
                        provider.Levels.AddRange(ReadLevels(r));
                        break;
                    case EventFieldKind.Task:
                        provider.Tasks.AddRange(ReadTasks(r));
                        break;
                    case EventFieldKind.Opcode:
                        provider.Opcodes.AddRange(ReadOpcodes(r));
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
                    default:
                        LogMessage("Unknown item type {0} at offset {1}.", type, offset);
                        break;
                }
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

                LogMessage(
                    "Event({0}, Msg=0x{1:X} T=0x{2:X} O=0x{3:X} L=0x{4:X} T=0x{5:X} K={6}@0x{7:X}[{8}] C=0x{9:X}",
                    desc, messageId, templateOffset, opcodeOffset, levelOffset,
                    taskOffset, keywordCount, keywordsOffset,
                    string.Join(", ", keywordOffsets),
                    channelOffset);

                var @event = new Event(Value.Create((uint)desc.EventId), Value.Create(desc.Version));
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

                var channel = new Channel(name, Value.CreateStruct(ChannelType.Analytic));
                if (value != 0)
                    channel.Value = (byte)value;
                channel.Message = ResolveMessage(messageId);
                MarkObject(offset, channel);
                channels.Add(channel);
            }

            foreach (var channel in channelEntries)
                LogMessage(channel);

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

                var level = new Level(QName.Parse(name, nsr), Value.Create((byte)value));
                level.Message = ResolveMessage(messageId);
                MarkObject(offset, level);
                levels.Add(level);
            }

            foreach (var level in levelEntries)
                LogMessage(level);

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

                var task = new Task(QName.Parse(name, nsr), Value.Create((ushort)value));
                task.Guid = guid == Guid.Empty ? null : (Guid?)guid;
                task.Message = ResolveMessage(messageId);
                MarkObject(offset, task);
                tasks.Add(task);
            }

            foreach (var task in taskEntries)
                LogMessage(task);

            return tasks;
        }

        private List<Opcode> ReadOpcodes(BinaryReader r)
        {
            var opcodes = new List<Opcode>();
            var opcodeEntries = new List<OpcodeEntry>();

            ReadMagic(r, CrimsonTags.OPCO);
            var length = r.ReadUInt32();
            var count = r.ReadUInt32();

            for (uint i = 0; i < count; ++i) {
                long offset = r.BaseStream.Position;
                var unk1 = r.ReadUInt16();
                var value = r.ReadUInt16();
                var messageId = r.ReadUInt32();
                var nameOffset = r.ReadUInt32();
                string name = ReadStringAt(r, nameOffset);
                opcodeEntries.Add(
                    new OpcodeEntry {
                        Unk1 = unk1,
                        Value = value,
                        MessageId = messageId,
                        Name = name,
                    });

                var opcode = new Opcode(QName.Parse(name, nsr), Value.Create((byte)value));
                opcode.Message = ResolveMessage(messageId);
                MarkObject(offset, opcode);
                opcodes.Add(opcode);
            }

            foreach (var opcode in opcodeEntries)
                LogMessage(opcode);

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

                var keyword = new Keyword(QName.Parse(name, nsr), Value.Create(mask));
                keyword.Message = ResolveMessage(messageId);
                MarkObject(offset, keyword);
                keywords.Add(keyword);
            }

            foreach (var keyword in keywordEntries)
                LogMessage(keyword);

            return keywords;
        }

        private List<IMap> ReadMaps(BinaryReader r)
        {
            var maps = new List<IMap>();
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

            foreach (var m in mapEntries)
                LogMessage(m);

            return maps;
        }

        private Tuple<IMap, MapEntry> ReadMap(BinaryReader r)
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
            var map = magic == CrimsonTags.VMAP ? new ValueMap(name) : (IMap)new BitMap(name);
            foreach (var itemEntry in itemEntries) {
                var value = Value.Create(itemEntry.Value);
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

            LogMessage(doc);
            LogMessage("Template({0}-{1}, {2}, Flags={3})", paramCount, dataCount, guid, flags);

            var structRanges = new List<Tuple<StructProperty, ushort, ushort>>();
            for (uint i = 0; i < paramCount; ++i)
                template.Properties.Add(ReadProperty(r, structRanges, true));

            ResolvePropertyRefs(template.Properties);

            var structProperties = new List<Property>();
            for (uint i = paramCount; i < dataCount; ++i)
                structProperties.Add(ReadProperty(r, structRanges, false));

            ResolvePropertyRefs(structProperties);

            foreach (var range in structRanges) {
                var @struct = range.Item1;
                int firstIdx = range.Item2;
                int count = range.Item3;
                @struct.Properties.AddRange(structProperties.Skip(firstIdx).Take(count));
            }

            r.BaseStream.Position = offset + length;

            MarkObject(offset, template);
            return template;
        }

        private void ResolvePropertyRefs(IReadOnlyList<Property> properties)
        {
            foreach (var property in properties) {
                ResolvePropertyRef(property.Length, properties);
                ResolvePropertyRef(property.Count, properties);
            }
        }

        private void ResolvePropertyRef(IPropertyNumber number, IReadOnlyList<Property> properties)
        {
            if (number.DataPropertyIndex == -1)
                return;

            int index = number.DataPropertyIndex;
            if (index < 0 || index >= properties.Count || !(properties[index] is DataProperty)) {
                diags.Report(
                    DiagnosticSeverity.Warning,
                    "Property references invalid property {0}. Template has only {1} properties.",
                    number.DataPropertyIndex, properties.Count);
                return;
            }

            var propertyRef = (DataProperty)properties[index];
            number.SetVariable(index, propertyRef.Name, propertyRef);
        }

        private Property ReadProperty(
            BinaryReader r, List<Tuple<StructProperty, ushort, ushort>> structRanges, bool print)
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

                if (print) {
                    LogMessage(
                        "  Struct({0}, flags={6} (0x{6:X}), props={1}@{2}, count={3}, length={4}, map=0x{5:X})",
                        name,
                        propertyCount,
                        firstPropertyIndex,
                        count,
                        length,
                        mapOffset,
                        flags);
                }

                var property = new StructProperty(name);
                structRanges.Add(Tuple.Create(property, firstPropertyIndex, propertyCount));

                if ((flags & PropertyFlags.VarLength) != 0)
                    property.Length.SetVariable(refPropertyIndex: length);
                if ((flags & PropertyFlags.VarCount) != 0)
                    property.Count.SetVariable(refPropertyIndex: length);

                if ((flags & PropertyFlags.FixedLength) != 0)
                    property.Length.SetFixed(length);
                if ((flags & PropertyFlags.FixedCount) != 0)
                    property.Count.SetFixed(length);

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

                if (print) {
                    LogMessage(
                        "  Data({0}, flags={6} (0x{6:X}), in={1:D} ({1}), out={2:D} ({2}), count={3}, length={4}, map=0x{5:X})",
                        name,
                        (InTypeKind)inputType,
                        (OutTypeKind)outputType,
                        count,
                        length,
                        mapOffset,
                        flags);
                }

                DataProperty property;
                if (metadata != null) {
                    property = new DataProperty(name, metadata.GetInType(inputType));
                    property.OutType = metadata.GetXmlType(outputType);
                } else {
                    property = new DataProperty(name, new InType(new QName("Dummy"), 0, null));
                }

                property.Map = GetObject<IMap>(mapOffset);

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
                    QName.Parse(name, nsr), Value.Create(value), Value.Create(version));
                filter.Message = ResolveMessage(messageId);
                MarkObject(offset, filter);
                filters.Add(filter);
            }

            foreach (var filter in filterEntries)
                LogMessage(filter);

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

            LocalizedString ls;
            if (strings.TryGetValue(messageId, out ls))
                return ls;

            string name = string.Format(CultureInfo.InvariantCulture, "str{0:X8}", messageId);
            string text = LookupMessage(messageId) ?? "{unresolved}";

            ls = new LocalizedString(name, text, messageId);
            strings[messageId] = ls;
            return ls;
        }

        private string LookupMessage(uint messageId)
        {
            Message message;
            if (messageMap != null && messageMap.TryGetValue(messageId, out message))
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

            object obj;
            if (!objectMap.TryGetValue(offset, out obj))
                throw new InternalException(
                    "Unread object for offset '{0}' requested.", offset);

            var value = obj as T;
            if (value == null)
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

        enum ChannelFlags : uint
        {
            None = 0,
            Imported = 1,
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
            public ushort Unk1;
            public ushort Value;
            public uint MessageId;
            public string Name;

            public override string ToString()
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Opcode({0}, Value={1}, Message=0x{2:X}, Unk1={3})",
                    Name, Value, MessageId, Unk1);
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

        private enum MapFlags : uint
        {
            None = 0,
            Bitmap = 1,
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

    enum InTypeKind
    {
        Unknown = 0,
        UnicodeString = 1,
        AnsiString = 2,
        Int8 = 3,
        UInt8 = 4,
        Int16 = 5,
        UInt16 = 6,
        Int32 = 7,
        UInt32 = 8,
        Int64 = 9,
        UInt64 = 10,
        Float = 11,
        Double = 12,
        Boolean = 13,
        Binary = 14,
        GUID = 15,
        Pointer = 16,
        FILETIME = 17,
        SYSTEMTIME = 18,
        SID = 19,
        HexInt32 = 20,
        HexInt64 = 21,
    }

    enum OutTypeKind
    {
        Unknown = 0,
        String = 1,
        DateTime = 2,
        Byte = 3,
        UnsignedByte = 4,
        Short = 5,
        UnsignedShort = 6,
        Int = 7,
        UnsignedInt = 8,
        Long = 9,
        UnsignedLong = 10,
        Float = 11,
        Double = 12,
        Boolean = 13,
        GUID = 14,
        HexBinary = 15,
        HexInt8 = 16,
        HexInt16 = 17,
        HexInt32 = 18,
        HexInt64 = 19,
        PID = 20,
        TID = 21,
        Port = 22,
        IPv4 = 23,
        IPv6 = 24,
        SocketAddress = 25,
        CIMDateTime = 26,
        ETWTIME = 27,
        Xml = 28,
        ErrorCode = 29,
        Win32Error = 30,
        NTSTATUS = 31,
        HResult = 32,
        DateTimeCultureInsensitive = 33,
    }
}
