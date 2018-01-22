namespace InstrManifestCompiler.ResGen
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;
    using InstrManifestCompiler.EventManifestSchema;
    using InstrManifestCompiler.EventManifestSchema.Base;
    using InstrManifestCompiler.EventManifestSchema.BinXml;
    using InstrManifestCompiler.Extensions;
    using InstrManifestCompiler.Support;

    internal class EventTemplateWriterOld : IDisposable
    {
        private readonly BinaryWriter writer;
        private readonly XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
        private readonly Dictionary<object, long> offsetMap = new Dictionary<object, long>();

        public EventTemplateWriterOld(Stream output)
        {
            writer = new BinaryWriter(output, Encoding.ASCII, true);
            nsmgr.AddNamespace("win", WinEventSchema.Namespace);
        }

        public void Dispose()
        {
            writer.Dispose();
        }

        public void Write(IList<Provider> providers)
        {
            WriteCrimBlock(providers);
            // FIXME: Why are there too zero-bytes at the end?
            writer.WriteUInt16(0);
        }

        /// struct CRIM_BLOCK {
        ///   ulittle32_t Magic; // 'CRIM'
        ///   ulittle32_t Length;
        ///   ulittle16_t Major;
        ///   ulittle16_t Minor;
        ///   ulittle32_t NumProviders;
        ///   ulittle32_t ProviderOffsets[];
        /// };
        private void WriteCrimBlock(IList<Provider> providers)
        {
            long startPos = writer.BaseStream.Position;

            ushort major = 3;
            ushort minor = 1;

            writer.WriteUInt32(CrimsonTags.CRIM);
            writer.WriteUInt32(0);
            writer.WriteUInt16(major);
            writer.WriteUInt16(minor);
            writer.WriteUInt32((uint)providers.Count);

            var offsets = new ReservedUInt32[providers.Count];
            for (int i = 0; i < providers.Count; ++i) {
                writer.WriteGuid(providers[i].Id);
                offsets[i] = writer.ReserveUInt32();
            }

            for (int i = 0; i < providers.Count; ++i) {
                offsets[i].UpdateToCurrent();
                WriteWevtBlock(providers[i]);
            }

            WriteLengthAt(startPos);
        }

        /// struct WEVT_BLOCK {
        ///   ulittle32_t Magic; // 'WEVT'
        ///   ulittle32_t Length;
        ///   ulittle32_t MessageId;
        ///   ulittle32_t NumOffsets;
        ///   WEVT_OFFSET Offsets[11];
        /// };
        /// struct WEVT_OFFSET {
        ///   ulittle32_t Type;
        ///   ulittle32_t Offset;
        /// };
        private void WriteWevtBlock(Provider provider)
        {
            long startPos = writer.BaseStream.Position;

            var types = new List<int>();
            if (provider.Channels.Count > 0)
                types.Add(EventFieldKind.Channel);
            if (provider.Maps.Count > 0)
                types.Add(EventFieldKind.Maps);
            if (provider.NamedQueries.Count > 0)
                types.Add(EventFieldKind.NamedQueries);
            if (provider.Templates.Count > 0)
                types.Add(EventFieldKind.Template);
            types.Add(EventFieldKind.Opcode);
            types.Add(EventFieldKind.Level);
            types.Add(EventFieldKind.Task);
            types.Add(EventFieldKind.Keyword);
            if (provider.Events.Count > 0)
                types.Add(EventFieldKind.Event);
            if (provider.Filters.Count > 0)
                types.Add(EventFieldKind.Filter);

            writer.WriteUInt32(CrimsonTags.WEVT);
            writer.WriteUInt32(0);
            writer.WriteUInt32(GetMessageId(provider.Message));
            writer.WriteUInt32((uint)types.Count);

            var offsets = new ReservedUInt32[11];
            foreach (var type in types) {
                writer.WriteUInt32((uint)type);
                offsets[type] = writer.ReserveUInt32();
            }
            for (int i = types.Count; i < offsets.Length; ++i)
                writer.WriteUInt64(0);

            foreach (int type in types) {
                offsets[type].UpdateToCurrent();
                switch (type) {
                    case EventFieldKind.Level:
                        WriteLevels(provider.Levels);
                        break;
                    case EventFieldKind.Task:
                        WriteTasks(provider.Tasks);
                        break;
                    case EventFieldKind.Opcode:
                        WriteOpcodes(provider.Opcodes);
                        break;
                    case EventFieldKind.Keyword:
                        WriteKeywords(provider.Keywords);
                        break;
                    case EventFieldKind.Event:
                        writer.FillAlignment(8);
                        offsets[type].UpdateToCurrent();
                        WriteEvents(provider.Events);
                        break;
                    case EventFieldKind.Channel:
                        WriteChannels(provider.Channels);
                        break;
                    case EventFieldKind.Maps:
                        WriteMaps(provider.Maps);
                        break;
                    case EventFieldKind.Template:
                        WriteTemplates(provider.Templates);
                        break;
                    case EventFieldKind.NamedQueries:
                        WriteNamedQueries(provider.NamedQueries);
                        break;
                    case EventFieldKind.Filter:
                        WriteFilters(provider.Filters);
                        break;
                    default:
                        throw new InternalException("Unknown item type {0}.", type);
                }
            }

            WriteLengthAt(startPos);
        }

        /// struct ETW_CHANNEL_BLOCK {
        ///   ulittle32_t Magic; // 'CHAN'
        ///   ulittle32_t Length;
        ///   ulittle32_t NumChannels;
        ///   ETW_CHANNEL Channels[];
        /// };
        /// struct ETW_CHANNEL {
        ///   ulittle32_t Unknown1;
        ///   ulittle32_t NameOffset;
        ///   ulittle32_t Value;
        ///   ulittle32_t MessageId;
        /// };
        private void WriteChannels(IList<Channel> channels)
        {
            long startPos = writer.BaseStream.Position;

            writer.WriteUInt32(CrimsonTags.CHAN);
            writer.WriteUInt32(0);
            writer.WriteUInt32((uint)channels.Count);

            long nameOffset = writer.BaseStream.Position;
            nameOffset += channels.Count * 16;

            foreach (var channel in channels) {
                MarkObjectOffset(channel);
                writer.WriteUInt32(channel.Imported ? 1u : 0);
                writer.WriteUInt32((uint)nameOffset);
                writer.WriteUInt32(channel.Value.GetValueOrDefault());
                writer.WriteUInt32(GetMessageId(channel.Message));
                nameOffset += GetByteCount(channel.Name);
            }

            foreach (var channel in channels)
                writer.WriteBytes(EncodeName(channel.Name));

            WriteLengthAt(startPos);
        }

        /// struct ETW_OPCODE_BLOCK {
        ///   ulittle32_t Magic; // 'OPCO'
        ///   ulittle32_t Length;
        ///   ulittle32_t NumOpcodes;
        ///   ETW_OPCODE Opcodes[];
        /// };
        /// struct ETW_OPCODE {
        ///   ulittle16_t Unknown1;
        ///   ulittle16_t Value;
        ///   ulittle32_t MessageId;
        ///   ulittle32_t NameOffset;
        /// };
        private void WriteOpcodes(IList<Opcode> opcodes)
        {
            opcodes = opcodes.ToList().StableSortBy(i => i.Value);

            long startPos = writer.BaseStream.Position;

            writer.WriteUInt32(CrimsonTags.OPCO);
            writer.WriteUInt32(0);
            writer.WriteUInt32((uint)opcodes.Count);
            if (opcodes.Count == 0)
                return;

            long dataOffset = writer.BaseStream.Position;
            dataOffset += opcodes.Count * 12;

            foreach (var opcode in opcodes) {
                MarkObjectOffset(opcode);
                writer.WriteUInt16(0);
                writer.WriteUInt16(opcode.Value);
                writer.WriteUInt32(GetMessageId(opcode.Message));
                writer.WriteUInt32((uint)dataOffset);
                //dataOffset += WriteAt(dataOffset, EncodeName(opcode.Name));
                dataOffset += GetByteCount(opcode.Name);
            }

            foreach (var opcode in opcodes)
                writer.WriteBytes(EncodeName(opcode.Name.Value.ToPrefixedString()));

            WriteLengthAt(startPos);
            //writer.BaseStream.Position = dataOffset;
            //WriteBlockHeader(startPos, CrimsonTags.OPCO);
        }

        /// struct ETW_LEVEL_BLOCK {
        ///   ulittle32_t Magic; // 'LEVL'
        ///   ulittle32_t Length;
        ///   ulittle32_t NumLevels;
        ///   ETW_LEVEL Levels[];
        /// };
        /// struct ETW_LEVEL {
        ///   ulittle32_t Value;
        ///   ulittle32_t MessageId;
        ///   ulittle32_t NameOffset;
        /// };
        private void WriteLevels(IList<Level> levels)
        {
            levels = levels.ToList().StableSortBy(i => i.Value);

            long startPos = writer.BaseStream.Position;

            writer.WriteUInt32(CrimsonTags.LEVL);
            writer.WriteUInt32(0);
            writer.WriteUInt32((uint)levels.Count);
            if (levels.Count == 0)
                return;

            long nameOffset = writer.BaseStream.Position;
            nameOffset += levels.Count * 12;

            foreach (var level in levels) {
                MarkObjectOffset(level);
                writer.WriteUInt32(level.Value);
                writer.WriteUInt32(GetMessageId(level.Message));
                writer.WriteUInt32((uint)nameOffset);
                nameOffset += GetByteCount(level.Name);
            }

            foreach (var level in levels)
                writer.WriteBytes(EncodeName(level.Name.Value.ToPrefixedString()));

            WriteLengthAt(startPos);
        }

        /// struct ETW_TASK_BLOCK {
        ///   ulittle32_t Magic; // 'TASK'
        ///   ulittle32_t Length;
        ///   ulittle32_t NumTasks;
        ///   ETW_TASK Tasks[];
        /// };
        /// struct ETW_TASK {
        ///   ulittle32_t Value;
        ///   ulittle32_t MessageId;
        ///   guid_le_t EventGuid;
        ///   ulittle32_t NameOffset;
        /// };
        private void WriteTasks(IList<Task> tasks)
        {
            long startPos = writer.BaseStream.Position;

            writer.WriteUInt32(CrimsonTags.TASK);
            writer.WriteUInt32(0);
            writer.WriteUInt32((uint)tasks.Count);
            if (tasks.Count == 0)
                return;

            long nameOffset = writer.BaseStream.Position;
            nameOffset += tasks.Count * 28;

            foreach (var task in tasks) {
                MarkObjectOffset(task);
                writer.WriteUInt32(task.Value);
                writer.WriteUInt32(GetMessageId(task.Message));
                writer.WriteGuid(task.Guid.GetValueOrDefault());
                writer.WriteUInt32((uint)nameOffset);
                nameOffset += GetByteCount(task.Name);
            }

            foreach (var task in tasks)
                writer.WriteBytes(EncodeName(task.Name.Value.ToPrefixedString()));

            WriteLengthAt(startPos);
        }

        /// struct ETW_KEYWORD_BLOCK {
        ///   ulittle32_t Magic; // 'KEYW'
        ///   ulittle32_t Length;
        ///   ulittle32_t NumKeywords;
        ///   ETW_KEYWORD Keywords[];
        /// };
        /// struct ETW_KEYWORD {
        ///   ulittle64_t Mask;
        ///   ulittle32_t MessageId;
        ///   ulittle32_t NameOffset;
        /// };
        private void WriteKeywords(IList<Keyword> keywords)
        {
            keywords = keywords.ToList().StableSortBy(i => i.Mask);

            long startPos = writer.BaseStream.Position;

            writer.WriteUInt32(CrimsonTags.KEYW);
            writer.WriteUInt32(0);
            writer.WriteUInt32((uint)keywords.Count);
            if (keywords.Count == 0)
                return;

            long nameOffset = writer.BaseStream.Position;
            nameOffset += keywords.Count * 16;

            foreach (var keyword in keywords) {
                MarkObjectOffset(keyword);
                writer.WriteUInt64(keyword.Mask);
                writer.WriteUInt32(GetMessageId(keyword.Message));
                writer.WriteUInt32((uint)nameOffset);
                nameOffset += GetByteCount(keyword.Name);
            }

            foreach (var keyword in keywords)
                writer.WriteBytes(EncodeName(keyword.Name.Value.ToPrefixedString()));

            WriteLengthAt(startPos);
        }

        /// struct ETW_MAPS_BLOCK {
        ///   ulittle32_t Magic; // 'MAPS'
        ///   ulittle32_t Length;
        ///   ulittle32_t NumMapOffsets;
        ///   ulittle32_t MapOffset[];
        /// };
        /// struct ETW_MAP {
        ///   ulittle32_t Magic; // 'VMAP', 'BMAP'
        ///   ulittle32_t Length;
        ///   ulittle32_t NameOffset;
        ///   ulittle32_t Unknown;
        ///   ulittle32_t NumItems;
        ///   ETW_MAP_ITEM Items[];
        /// };
        /// struct ETW_MAP_ITEM {
        ///   ulittle32_t Value;
        ///   ulittle32_t MessageId;
        /// };
        private void WriteMaps(IList<IMap> maps)
        {
            var sortedMaps = new List<IMap>(maps);
            sortedMaps.StableSortBy(m => m.Name);

            long startPos = writer.BaseStream.Position;

            writer.WriteUInt32(CrimsonTags.MAPS);
            writer.WriteUInt32(0);
            writer.WriteUInt32((uint)maps.Count);

            var mapOffsets = new Dictionary<string, uint>(maps.Count);
            var nameOffsets = new Dictionary<string, uint>(maps.Count);

            long offset = writer.BaseStream.Position + (maps.Count * 4);

            foreach (var map in maps) {
                mapOffsets.Add(map.Name, (uint)offset);
                offset += 20 + (map.Items.Count * 8);
            }

            foreach (var map in sortedMaps) {
                nameOffsets.Add(map.Name, (uint)offset);
                offset += GetByteCount(map.Name);
            }

            foreach (var map in sortedMaps)
                writer.WriteUInt32(mapOffsets[map.Name]);

            foreach (var map in maps)
                WriteMap(map, nameOffsets[map.Name]);

            foreach (var map in sortedMaps)
                writer.WriteBytes(EncodeName(map.Name));

            WriteLengthAt(startPos);
        }

        private void WriteMap(IMap map, uint nameOffset)
        {
            var items = map.Items.ToList().StableSortBy(i => i.Value);

            MarkObjectOffset(map);
            long startPos = writer.BaseStream.Position;

            writer.WriteUInt32(map.Kind == MapKind.BitMap ? CrimsonTags.BMAP : CrimsonTags.VMAP);
            writer.WriteUInt32(0);

            writer.WriteUInt32(nameOffset);
            writer.WriteUInt32(map.Kind == MapKind.BitMap ? 1u : 0);
            writer.WriteUInt32((uint)items.Count);

            foreach (var item in items) {
                writer.WriteUInt32(item.Value);
                writer.WriteUInt32(GetMessageId(item.Message));
            }

            WriteLengthAt(startPos);
        }

        /// struct ETW_TEMPLATE_BLOCK {
        ///   ulittle32_t Magic; // 'TTBL'
        ///   ulittle32_t Length;
        ///   ulittle32_t NumTemplates;
        ///   ETW_TEMPLATE Templates[];
        /// };
        private void WriteTemplates(IList<Template> templates)
        {
            long startPos = writer.BaseStream.Position;

            writer.WriteUInt32(CrimsonTags.TTBL);
            writer.WriteUInt32(0);
            writer.WriteUInt32((uint)templates.Count);

            foreach (var template in templates)
                WriteTemplate(template);

            WriteLengthAt(startPos);
        }

        /// struct ETW_TEMPLATE {
        ///   ulittle32_t Magic; // 'TEMP'
        ///   ulittle32_t Length;
        ///   ulittle32_t NumParams;
        ///   ulittle32_t NumProperties; (?)
        ///   ulittle32_t PropertyOffset;
        ///   ulittle32_t Unknown1; // Flags?
        ///   guid_le_t TemplateId;
        ///   char BinXml[];
        ///   ETW_TEMPLATE_PROPERTY Properties[];
        /// };
        private void WriteTemplate(Template template)
        {
            MarkObjectOffset(template);
            long startPos = writer.BaseStream.Position;

            var properties = template.Properties;

            int paramCount = properties.Count;
            int propertyCount = properties.Count +
                                properties
                                    .Where(p => p.Kind == PropertyKind.Struct)
                                    .Cast<StructProperty>()
                                    .Sum(p => p.Properties.Count);

            uint flags = 1;
            if (template.UserData != null)
                flags = 2;

            writer.WriteUInt32(CrimsonTags.TEMP);
            writer.WriteUInt32(0);

            writer.WriteUInt32((uint)paramCount);
            writer.WriteUInt32((uint)propertyCount);
            var propertyOffset = writer.ReserveUInt32();
            writer.WriteUInt32(flags);

            var propTypes = new byte[properties.Count];
            for (int i = 0; i < properties.Count; i++)
                propTypes[i] = (byte)properties[i].BinXmlType;

            var doc = CreateTemplateXml(template);
            string templateXml = doc.ToString(SaveOptions.DisableFormatting);

            var templateId = CreateTemplateId(templateXml, propTypes);
            writer.WriteGuid(templateId);

            var binXmlWriter = new BinXmlWriter(writer, propTypes);
            binXmlWriter.WriteFragment(doc);
            writer.FillAlignment(4);

            long nameOffset = writer.BaseStream.Position;
            foreach (var property in properties) {
                nameOffset += 20;
                if (property.Kind == PropertyKind.Struct)
                    nameOffset += 20 * ((StructProperty)property).Properties.Count;
            }

            propertyOffset.UpdateToCurrent();
            int propIndex = properties.Count;
            foreach (var property in properties)
                WriteProperty(property, ref nameOffset, ref propIndex);
            foreach (var property in properties) {
                if (property.Kind == PropertyKind.Struct)
                    foreach (var prop in ((StructProperty)property).Properties)
                        WriteProperty(prop, ref nameOffset, ref propIndex);
            }

            foreach (var property in properties)
                writer.WriteBytes(EncodeName(property.Name));
            foreach (var property in properties) {
                if (property.Kind == PropertyKind.Struct)
                    foreach (var data in ((StructProperty)property).Properties)
                        writer.WriteBytes(EncodeName(data.Name));
            }

            WriteLengthAt(startPos);
        }

        /// struct ETW_TEMPLATE_PROPERTY {
        ///   ulittle32_t Flags;
        ///   union {
        ///     struct {
        ///       ulittle8_t InputType;
        ///       ulittle8_t OutputType;
        ///       ulittle16_t Padding; (?)
        ///       ulittle32_t MapOffset;
        ///     } nonStructType;
        ///     struct {
        ///       ulittle16_t StructStartIndex;
        ///       ulittle16_t NumStructMembers;
        ///       ulittle32_t Padding; (?)
        ///     } structType;
        ///   } u;
        ///   ulittle16_t Count;
        ///   ulittle16_t Length;
        ///   ulittle32_t NameOffset;
        /// };
        private void WriteProperty(Property property, ref long nameOffset, ref int propIndex)
        {
            PropertyFlags flags = property.GetFlags();

            ushort count = property.Count.Value.GetValueOrDefault();
            ushort length = property.Length.Value.GetValueOrDefault();
            if (property.Count.IsVariable)
                count = (ushort)property.Count.DataPropertyIndex;
            if (property.Length.IsVariable)
                length = (ushort)property.Length.DataPropertyIndex;

            writer.WriteUInt32((uint)flags);
            if (property.Kind == PropertyKind.Struct) {
                var structProp = (StructProperty)property;
                var propCount = (ushort)structProp.Properties.Count;
                writer.WriteUInt16((ushort)propIndex);
                writer.WriteUInt16(propCount);
                writer.WriteUInt32(0);
                propIndex += propCount;
            } else {
                var data = (DataProperty)property;
                var inputType = (byte)data.InType.Value;
                var outputType = (byte)data.OutType.Value;
                writer.WriteUInt8(inputType);
                writer.WriteUInt8(outputType);
                writer.WriteUInt16(0);
                writer.WriteUInt32(GetObjectOffset(data.Map));
            }
            writer.WriteUInt16(count);
            writer.WriteUInt16(length);
            writer.WriteUInt32((uint)nameOffset);
            nameOffset += GetByteCount(property.Name);
        }

        private void WriteNamedQueries(IList<PatternMap> maps)
        {
            long startPos = writer.BaseStream.Position;

            writer.WriteUInt32(CrimsonTags.QTAB);
            writer.WriteUInt32(0);
            writer.WriteUInt32((uint)maps.Count);

            var offsets = new ReservedUInt32[maps.Count];
            for (int i = 0; i < maps.Count; i++)
                offsets[i] = writer.ReserveUInt32();

            long nameOffset = writer.BaseStream.Position;
            nameOffset += maps.Count * 20;
            foreach (var map in maps)
                nameOffset += map.Items.Count * 8;

            for (int i = 0; i < maps.Count; i++) {
                offsets[i].UpdateToCurrent();
                WritePatternMap(maps[i], ref nameOffset);
            }

            foreach (var map in maps) {
                writer.WriteBytes(EncodeName(map.Name));
                writer.WriteBytes(EncodeName(map.Format));
                foreach (var item in map.Items) {
                    writer.WriteBytes(EncodeName(item.Name));
                    writer.WriteBytes(EncodeName(item.Value));
                }
            }

            WriteLengthAt(startPos);
        }

        private void WritePatternMap(PatternMap map, ref long nameOffset)
        {
            MarkObjectOffset(map);

            writer.WriteUInt32(CrimsonTags.QUER);
            var relOffset = writer.ReserveUInt32();

            writer.WriteUInt32((uint)nameOffset);
            nameOffset += GetByteCount(map.Name);
            writer.WriteUInt32((uint)nameOffset);
            nameOffset += GetByteCount(map.Format);

            writer.WriteUInt32((uint)map.Items.Count);

            foreach (var item in map.Items) {
                writer.WriteUInt32((uint)nameOffset);
                nameOffset += GetByteCount(item.Name);
                writer.WriteUInt32((uint)nameOffset);
                nameOffset += GetByteCount(item.Value);
            }

            relOffset.Update((uint)(nameOffset - writer.BaseStream.Position + 20 + map.Items.Count * 8));
        }

        /// struct ETW_FILTER_BLOCK {
        ///   ulittle32_t Magic; // 'FLTR'
        ///   ulittle32_t Length;
        ///   ulittle32_t NumFilters;
        ///   ulittle32_t Junk;
        ///   ETW_FILTER Filter[];
        /// };
        /// struct ETW_FILTER {
        ///   ulittle8_t Value;
        ///   ulittle8_t Version;
        ///   ulittle16_t Padding;
        ///   ulittle32_t MessageId;
        ///   ulittle32_t NameOffset;
        ///   ulittle32_t TemplateOffset;
        /// };
        private void WriteFilters(IList<Filter> filters)
        {
            long startPos = writer.BaseStream.Position;

            writer.WriteUInt32(CrimsonTags.FLTR);
            writer.WriteUInt32(0);
            writer.WriteUInt32((uint)filters.Count);

            // This field is most likely just garbage. MC seems to reuse the
            // local struct to write the block header.
            writer.WriteUInt32(GetObjectOffset(filters[filters.Count - 1].Template));

            long nameOffset = writer.BaseStream.Position;
            nameOffset += filters.Count * 16;

            foreach (var filter in filters) {
                MarkObjectOffset(filter);
                writer.WriteUInt8(filter.Value);
                writer.WriteUInt8(filter.Version);
                writer.WriteUInt16(0); // padding
                writer.WriteUInt32(GetMessageId(filter.Message));
                writer.WriteUInt32((uint)nameOffset);
                writer.WriteUInt32(GetObjectOffset(filter.Template));
                nameOffset += GetByteCount(filter.Name);
            }

            foreach (var filter in filters)
                writer.WriteBytes(EncodeName(filter.Name.Value.ToPrefixedString()));

            WriteLengthAt(startPos);
        }

        private static Guid CreateTemplateId(string templateXml, byte[] types)
        {
            var xmlBytes = Encoding.Unicode.GetBytes(templateXml);
            var typeBytes = new byte[types.Length * 4];
            for (int i = 0; i < types.Length; ++i)
                BitConverterEx.FromUInt32(types[i], typeBytes, i * 4);

            var md5 = new MD5Managed();
            md5.TransformFinalBlock(xmlBytes, 0, xmlBytes.Length);
            md5.Initialize(md5.Hash);
            md5.TransformFinalBlock(typeBytes, 0, typeBytes.Length);

            return new Guid(md5.Hash);
        }

        private XDocument CreateTemplateXml(Template template)
        {
            if (template.UserData != null)
                return new XDocument(template.UserData);

            var eventData = new XElement("EventData");
            if (template.Name != null)
                eventData.Add(new XAttribute("Name", template.Name));
            foreach (var property in template.Properties) {
                string localName = (property.Kind == PropertyKind.Struct ? "ComplexData" : "Data");
                var elem = new XElement(
                    localName,
                    new XAttribute("Name", property.Name),
                    "%" + (property.Index + 1));
                eventData.Add(elem);
            }
            return new XDocument(eventData);
        }

        /// struct ETW_EVENT_BLOCK {
        ///   ulittle32_t Magic; // 'EVNT'
        ///   ulittle32_t Length;
        ///   ulittle32_t NumEvents;
        ///   ulittle32_t Unknown;
        ///   ETW_EVENT Events[];
        /// };
        /// struct ETW_EVENT {
        ///   EVENT_DESCRIPTOR Descriptor;
        ///   ulittle32_t MessageId;
        ///   ulittle32_t TemplateOffset;
        ///   ulittle32_t OpcodeOffset;
        ///   ulittle32_t LevelOffset;
        ///   ulittle32_t TaskOffset;
        ///   ulittle32_t NumKeywords;
        ///   ulittle32_t KeywordsOffset;
        ///   ulittle32_t ChannelOffset;
        /// };
        /// struct ETW_KEYWORD_LIST {
        ///   ulittle32_t KeywordOffsets[];
        /// };
        private void WriteEvents(IList<Event> events)
        {
            events = events.ToList().StableSortBy(i => i.Value);

            long startPos = writer.BaseStream.Position;

            writer.WriteUInt32(CrimsonTags.EVNT);
            writer.WriteUInt32(0);

            writer.WriteUInt32((uint)events.Count);
            writer.WriteUInt32(0);

            var keywordOffsets = new ReservedUInt32[events.Count];
            for (int i = 0; i < events.Count; ++i) {
                var evt = events[i];

                WriteEventDescriptor(evt);
                writer.WriteUInt32(GetMessageId(evt.Message));
                writer.WriteUInt32(GetObjectOffset(evt.Template));
                writer.WriteUInt32(GetObjectOffset(evt.Opcode));
                writer.WriteUInt32(GetObjectOffset(evt.Level));
                writer.WriteUInt32(GetObjectOffset(evt.Task));
                writer.WriteUInt32((uint)evt.Keywords.Count);
                keywordOffsets[i] = writer.ReserveUInt32();
                writer.WriteUInt32(GetObjectOffset(evt.Channel));
            }

            for (int i = 0; i < events.Count; ++i) {
                if (events[i].Keywords.Count == 0)
                    continue;
                keywordOffsets[i].UpdateToCurrent();
                foreach (Keyword keyword in events[i].Keywords)
                    writer.WriteUInt32(GetObjectOffset(keyword));
            }

            WriteLengthAt(startPos);
        }

        private void WriteEventDescriptor(Event evt)
        {
            writer.WriteUInt16((ushort)evt.Value);
            writer.WriteUInt8(evt.Version);
            writer.WriteUInt8(evt.ChannelValue);
            writer.WriteUInt8(evt.LevelValue);
            writer.WriteUInt8(evt.OpcodeValue);
            writer.WriteUInt16(evt.TaskValue);
            writer.WriteUInt64(evt.KeywordMask);
        }

        private void MarkObjectOffset(object obj)
        {
            offsetMap.Add(obj, writer.BaseStream.Position);
        }

        private uint GetObjectOffset(object obj)
        {
            if (obj == null)
                return 0;

            if (!offsetMap.TryGetValue(obj, out var offset))
                throw new InternalException(
                    "Offset for unwritten object '{0}' requested.", obj);

            return (uint)offset;
        }

        private static uint GetMessageId(LocalizedString message)
        {
            return message?.Id ?? Message.UnusedId;
        }

        private byte[] EncodeName(string name)
        {
            int count = GetByteCount(name);
            var bytes = new byte[count];
            byte[] countBytes = BitConverter.GetBytes((uint)count);
            Buffer.BlockCopy(countBytes, 0, bytes, 0, 4);
            Encoding.Unicode.GetBytes(name, 0, name.Length, bytes, 4);
            return bytes;
        }

        private int GetByteCount(QName name)
        {
            return GetByteCount(name.LocalName);
        }

        private int GetByteCount(string name)
        {
            // Count includes NUL and 4-byte length.
            int count =  Encoding.Unicode.GetByteCount(name) + 2 + 4;
            return (count + 3) & ~3;
        }

        private void WriteLengthAt(long blockOffset)
        {
            uint length = (uint)(writer.BaseStream.Position - blockOffset);
            writer.WriteUInt32At(blockOffset + 4, length);
        }
    }
}
