namespace EventTraceKit.EventTracing.Compilation.ResGen
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;
    using EventTraceKit.EventTracing.Compilation.BinXml;
    using EventTraceKit.EventTracing.Compilation.ResGen.Crimson;
    using EventTraceKit.EventTracing.Compilation.Support;
    using EventTraceKit.EventTracing.Internal.Extensions;
    using EventTraceKit.EventTracing.Schema;
    using EventTraceKit.EventTracing.Support;
    using PatternMapItem = EventTraceKit.EventTracing.Compilation.ResGen.Crimson.PatternMapItem;
    using PropertyFlags = EventTraceKit.EventTracing.Compilation.ResGen.Crimson.PropertyFlags;

    internal class EventTemplateWriter : IDisposable
    {
        private readonly XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
        private readonly MemoryMappedViewWriter writer;
        private readonly Dictionary<object, long> offsetMap = new Dictionary<object, long>();

        public EventTemplateWriter(FileStream output)
        {
            nsmgr.AddNamespace("win", WinEventSchema.Namespace.NamespaceName);
            writer = new MemoryMappedViewWriter(output);
        }

        public bool EnableMessageCompilerCompat { get; set; } = true;
        public bool UseLegacyTemplateIds { get; set; }

        public void Dispose()
        {
            writer.Dispose();
        }

        public void Write(IList<Provider> providers)
        {
            WriteCrimBlock(providers);
            // FIXME: Why are there two zero-bytes at the end?
            writer.WriteUInt16(0);
        }

        private void WriteCrimBlock(IList<Provider> providers)
        {
            long startPos = writer.Position;
            writer.Position += Marshal.SizeOf<FileHeader>();
            long offsetsOffset = writer.Position;

            writer.Position += providers.Count * Marshal.SizeOf<ProviderEntry>();
            var offsets = new long[providers.Count];
            for (int i = 0; i < providers.Count; ++i) {
                offsets[i] = writer.Position;
                WriteWevtBlock(providers[i]);
            }

            for (int i = 0; i < providers.Count; ++i) {
                ProviderEntry o;
                o.Guid = providers[i].Id;
                o.Offset = (uint)offsets[i];
                writer.WriteResource(ref offsetsOffset, ref o);
            }

            FileHeader b;
            b.Magic = CrimsonTags.CRIM;
            b.Length = (uint)(writer.Position - startPos);
            b.Major = 3;
            b.Minor = 1;
            b.NumProviders = (uint)providers.Count;
            writer.WriteResource(ref startPos, ref b);
        }

        private void WriteWevtBlock(Provider provider)
        {
            long startPos = writer.Position;

            var types = new List<EventFieldKind>();
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

            var offsets = new long[11];
            long offset = startPos + Marshal.SizeOf<ProviderBlock>();
            long dataOffset = offset + (offsets.Length * Marshal.SizeOf<ProviderListOffset>());

            writer.Position = dataOffset;

            foreach (var type in types) {
                offsets[(int)type] = writer.Position;
                switch (type) {
                    case EventFieldKind.Level:
                        WriteLevels(provider.Levels);
                        break;
                    case EventFieldKind.Task:
                        WriteTasks(provider.Tasks);
                        break;
                    case EventFieldKind.Opcode:
                        WriteOpcodes(provider.GetAllOpcodes());
                        break;
                    case EventFieldKind.Keyword:
                        WriteKeywords(provider.Keywords);
                        break;
                    case EventFieldKind.Event:
                        writer.FillAlignment(8);
                        offsets[(int)type] = writer.Position;
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

            foreach (var type in types) {
                var o = new ProviderListOffset();
                o.Type = type;
                o.Offset = (uint)offsets[(int)type];
                writer.WriteResource(ref offset, ref o);
            }
            for (int i = types.Count; i < offsets.Length; ++i) {
                var o = new ProviderListOffset();
                writer.WriteResource(ref offset, ref o);
            }

            ProviderBlock b;
            b.Magic = CrimsonTags.WEVT;
            b.Length = (uint)(writer.Position - startPos);
            b.MessageId = GetMessageId(provider.Message);
            b.NumOffsets = (uint)types.Count;
            writer.WriteResource(ref startPos, ref b);
        }

        private void WriteChannels(IList<Channel> channels)
        {
            long startPos = writer.Position;
            writer.Position += Marshal.SizeOf<ListBlock>();
            long offset = writer.Position + (channels.Count * Marshal.SizeOf<ChannelEntry>());

            foreach (var channel in channels) {
                MarkObjectOffset(channel, writer.Position);

                ChannelEntry c;
                c.Flags = channel.Imported ? ChannelFlags.Imported : ChannelFlags.None;
                c.NameOffset = (uint)offset;
                c.Value = channel.Value.GetValueOrDefault();
                c.MessageId = GetMessageId(channel.Message);

                writer.WriteResource(ref c);
                writer.WriteString(ref offset, channel.Name);
            }

            writer.Position = offset;

            var b = new ListBlock();
            b.Magic = CrimsonTags.CHAN;
            b.Length = (uint)(writer.Position - startPos);
            b.NumEntries = (uint)channels.Count;
            writer.WriteResource(ref startPos, ref b);
        }

        private void WriteOpcodes(IEnumerable<Opcode> opcodesRange)
        {
            var opcodes = opcodesRange.ToList().StableSortBy(i => (i.Value.Value << 16) | (i.Task?.Value.Value ?? 0));

            long startPos = writer.Position;
            writer.Position += Marshal.SizeOf<ListBlock>();
            long offset = writer.Position + (opcodes.Count * Marshal.SizeOf<OpcodeEntry>());

            foreach (var opcode in opcodes) {
                MarkObjectOffset(opcode, writer.Position);

                var op = new OpcodeEntry();
                op.TaskId = opcode.Task?.Value.Value ?? 0;
                op.Value = opcode.Value;
                op.MessageId = GetMessageId(opcode.Message);
                op.NameOffset = (uint)offset;

                writer.WriteResource(ref op);
                writer.WriteString(ref offset, opcode.Name.Value.ToPrefixedString());
            }

            writer.Position = offset;

            var b = new ListBlock();
            b.Magic = CrimsonTags.OPCO;
            b.Length = opcodes.Count > 0 ? (uint)(writer.Position - startPos) : 0;
            b.NumEntries = (uint)opcodes.Count;
            writer.WriteResource(ref startPos, ref b);
        }

        private void WriteLevels(IList<Level> levels)
        {
            levels = levels.ToList().StableSortBy(i => i.Value.Value);

            long startPos = writer.Position;
            writer.Position += Marshal.SizeOf<ListBlock>();
            long offset = writer.Position + (levels.Count * Marshal.SizeOf<LevelEntry>());

            foreach (var level in levels) {
                MarkObjectOffset(level, writer.Position);

                var l = new LevelEntry();
                l.Value = level.Value;
                l.MessageId = GetMessageId(level.Message);
                l.NameOffset = (uint)offset;

                writer.WriteResource(ref l);
                writer.WriteString(ref offset, level.Name.Value.ToPrefixedString());
            }

            writer.Position = offset;

            var b = new ListBlock();
            b.Magic = CrimsonTags.LEVL;
            b.Length = levels.Count > 0 ? (uint)(writer.Position - startPos) : 0;
            b.NumEntries = (uint)levels.Count;
            writer.WriteResource(ref startPos, ref b);
        }

        private void WriteTasks(IList<Task> tasks)
        {
            long startPos = writer.Position;
            writer.Position += Marshal.SizeOf<ListBlock>();
            long offset = writer.Position + (tasks.Count * Marshal.SizeOf<TaskEntry>());

            foreach (var task in tasks) {
                MarkObjectOffset(task, writer.Position);

                var t = new TaskEntry();
                t.Value = task.Value;
                t.MessageId = GetMessageId(task.Message);
                t.EventGuid = task.Guid.GetValueOrDefault();
                t.NameOffset = (uint)offset;

                writer.WriteResource(ref t);
                writer.WriteString(ref offset, task.Name.Value.ToPrefixedString());
            }

            writer.Position = offset;

            var b = new ListBlock();
            b.Magic = CrimsonTags.TASK;
            b.Length = tasks.Count > 0 ? (uint)(writer.Position - startPos) : 0;
            b.NumEntries = (uint)tasks.Count;
            writer.WriteResource(ref startPos, ref b);
        }

        private void WriteKeywords(IList<Keyword> keywords)
        {
            keywords = keywords.ToList().StableSortBy(i => i.Mask);

            long startPos = writer.Position;
            writer.Position += Marshal.SizeOf<ListBlock>();
            long offset = writer.Position + (keywords.Count * Marshal.SizeOf<KeywordEntry>());

            foreach (var keyword in keywords) {
                MarkObjectOffset(keyword, writer.Position);

                var kw = new KeywordEntry();
                kw.Mask = keyword.Mask;
                kw.MessageId = GetMessageId(keyword.Message);
                kw.NameOffset = (uint)offset;

                writer.WriteResource(ref kw);
                writer.WriteString(ref offset, keyword.Name.Value.ToPrefixedString());
            }

            writer.Position = offset;

            var b = new ListBlock();
            b.Magic = CrimsonTags.KEYW;
            b.Length = keywords.Count > 0 ? (uint)(writer.Position - startPos) : 0;
            b.NumEntries = (uint)keywords.Count;
            writer.WriteResource(ref startPos, ref b);
        }

        private void WriteMaps(IList<Map> maps)
        {
            var sortedMaps = new List<Map>(maps);
            sortedMaps.StableSortBy(m => m.Name);

            long startPos = writer.Position;
            writer.Position += Marshal.SizeOf<ListBlock>();

            long offsetsOffset = writer.Position;
            writer.Position += maps.Count * Marshal.SizeOf<uint>();

            long stringOffset = writer.Position;
            foreach (var map in maps)
                stringOffset += CalcMapSize(map);

            var offsets = new SortedDictionary<string, long>();
            foreach (var map in sortedMaps) {
                offsets[map.Name] = stringOffset;
                writer.WriteString(ref stringOffset, map.Name);
            }

            foreach (var map in maps) {
                long offset = writer.Position;
                WriteMap(map, (uint)offsets[map.Name]);
                offsets[map.Name] = offset;
            }

            foreach (var offset in offsets.Values)
                writer.WriteUInt32(ref offsetsOffset, (uint)offset);
            writer.Position = stringOffset;

            var b = new ListBlock();
            b.Magic = CrimsonTags.MAPS;
            b.Length = (uint)(writer.Position - startPos);
            b.NumEntries = (uint)maps.Count;
            writer.WriteResource(ref startPos, ref b);
        }

        private static int CalcMapSize(Map map)
        {
            return Marshal.SizeOf<MapEntry>() +
                   (map.Items.Count * Marshal.SizeOf<MapItemEntry>());
        }

        private void WriteMap(Map map, uint nameOffset)
        {
            var items = map.Items.ToList().StableSortBy(i => i.Value.Value);

            long startPos = writer.Position;
            MarkObjectOffset(map, startPos);
            writer.Position += Marshal.SizeOf<MapEntry>();

            foreach (var item in items) {
                MapItemEntry i;
                i.Value = item.Value;
                i.MessageId = GetMessageId(item.Message);
                writer.WriteResource(ref i);
            }

            MapEntry m;
            switch (map.Kind) {
                case MapKind.ValueMap:
                    m.Magic = CrimsonTags.VMAP;
                    m.Flags = MapFlags.None;
                    break;
                case MapKind.BitMap:
                    m.Magic = CrimsonTags.BMAP;
                    m.Flags = MapFlags.Bitmap;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            m.Length = (uint)(writer.Position - startPos);
            m.NameOffset = nameOffset;
            m.NumItems = (uint)items.Count;
            writer.WriteResource(ref startPos, ref m);
        }

        private void WriteTemplates(IList<Template> templates)
        {
            long startPos = writer.Position;
            writer.Position += Marshal.SizeOf<ListBlock>();
            foreach (var template in templates)
                WriteTemplate(template);

            var b = new ListBlock();
            b.Magic = CrimsonTags.TTBL;
            b.Length = (uint)(writer.Position - startPos);
            b.NumEntries = (uint)templates.Count;
            writer.WriteResource(ref startPos, ref b);
        }

        private void WriteTemplate(Template template)
        {
            var properties = template.Properties;
            int totalPropertyCount =
                properties.Count +
                properties
                    .Where(p => p.Kind == PropertyKind.Struct)
                    .Cast<StructProperty>()
                    .Sum(p => p.Properties.Count);

            uint flags = 1;
            if (template.UserData != null)
                flags = 2;

            var propTypes = new byte[properties.Count];
            for (int i = 0; i < properties.Count; ++i)
                propTypes[i] = (byte)properties[i].BinXmlType;

            XDocument eventXml = CreateXmlTemplate(template);
            Guid templateId = CreateTemplateId(eventXml, propTypes);
            byte[] binXml = ConvertToBinXml(eventXml, propTypes);

            long startPos = writer.Position;
            MarkObjectOffset(template, startPos);
            writer.Position += Marshal.SizeOf<TemplateEntry>() + binXml.Length;

            long propOffset = writer.Position;
            long nameOffset = writer.Position + (totalPropertyCount * Marshal.SizeOf<PropertyEntry>());

            int structStartIndex = properties.Count;
            foreach (var property in properties)
                WriteProperty(property, ref nameOffset, ref structStartIndex);

            int indexRefOffset = properties.Count;
            foreach (var property in properties.OfType<StructProperty>()) {
                int dummy = 0;
                foreach (var prop in property.Properties)
                    WriteProperty(prop, ref nameOffset, ref dummy, indexRefOffset);
                indexRefOffset += property.Properties.Count;
            }

            writer.Position = nameOffset;

            TemplateEntry t;
            t.Magic = CrimsonTags.TEMP;
            t.Length = (uint)(writer.Position - startPos);
            t.NumParams = (uint)properties.Count;
            t.NumProperties = (uint)totalPropertyCount;
            t.PropertyOffset = (uint)propOffset;
            t.Flags = flags;
            t.TemplateId = templateId;

            writer.WriteResource(ref startPos, ref t);
            writer.WriteArray(ref startPos, binXml);
        }

        private static byte[] ConvertToBinXml(XDocument doc, byte[] propTypes)
        {
            using (var buffer = new MemoryStream()) {
                BinXmlWriter.Write(buffer, doc, propTypes);
                return buffer.ToArray();
            }
        }

        private void WriteProperty(
            Property property, ref long nameOffset, ref int structStartIndex, int indexRefOffset = 0)
        {
            var flags = property.GetFlags();

            ushort count = property.Count.Value.GetValueOrDefault();
            ushort length = property.Length.Value.GetValueOrDefault();
            if (property.Count.IsVariable)
                count = (ushort)(property.Count.DataPropertyIndex + indexRefOffset);
            if (property.Length.IsVariable)
                length = (ushort)(property.Length.DataPropertyIndex + indexRefOffset);

            var p = new PropertyEntry();
            p.Flags = (PropertyFlags)flags;
            if (property.Kind == PropertyKind.Struct) {
                var sp = (StructProperty)property;
                p.structType.StructStartIndex = (ushort)structStartIndex;
                p.structType.NumStructMembers = (ushort)sp.Properties.Count;
                structStartIndex += sp.Properties.Count;
            } else {
                var dp = (DataProperty)property;
                p.nonStructType.InputType = (InTypeKind)dp.InType.Value;
                p.nonStructType.OutputType = (OutTypeKind)dp.OutType.Value;
                p.nonStructType.MapOffset = GetObjectOffset(dp.Map);
            }
            p.Count = count;
            p.Length = length;
            p.NameOffset = (uint)nameOffset;

            writer.WriteResource(ref p);
            writer.WriteString(ref nameOffset, property.Name);
        }

        private void WriteNamedQueries(IList<PatternMap> maps)
        {
            long startPos = writer.Position;
            writer.Position += Marshal.SizeOf<ListBlock>();
            long offset = writer.Position;

            long mapOffset = offset + (maps.Count * Marshal.SizeOf<int>());
            long dataOffset = mapOffset + (maps.Count * Marshal.SizeOf<PatternMapEntry>());

            for (int i = 0; i < maps.Count; ++i)
                dataOffset += maps[i].Items.Count * Marshal.SizeOf<PatternMapItem>();

            writer.Position = mapOffset;

            var mapOffsets = new uint[maps.Count];
            for (int i = 0; i < maps.Count; ++i) {
                mapOffsets[i] = (uint)writer.Position;
                WritePatternMap(maps[i], ref dataOffset);
            }

            writer.WriteArray(ref offset, mapOffsets);

            var b = new ListBlock();
            b.Magic = CrimsonTags.QTAB;
            b.Length = (uint)(dataOffset - startPos);
            b.NumEntries = (uint)maps.Count;
            writer.WriteResource(ref startPos, ref b);
            writer.Position = dataOffset;
        }

        private void WritePatternMap(PatternMap map, ref long dataOffset)
        {
            long startPos = writer.Position;
            MarkObjectOffset(map, writer.Position);
            long offset = writer.Position + Marshal.SizeOf<PatternMapEntry>();

            long nameOffset = dataOffset;
            writer.WriteString(ref dataOffset, map.Name);
            long formatOffset = dataOffset;
            writer.WriteString(ref dataOffset, map.Format);

            foreach (var item in map.Items) {
                PatternMapItem i;
                i.NameOffset = (uint)dataOffset;
                writer.WriteString(ref dataOffset, item.Name);
                i.ValueOffset = (uint)dataOffset;
                writer.WriteString(ref dataOffset, item.Value);
                writer.WriteResource(ref offset, ref i);
            }

            writer.Position = offset;

            PatternMapEntry m;
            m.Magic = CrimsonTags.QUER;
            // Length seems to span until the end of the strings, even though
            // other maps interleave this. Bug in MC?
            m.Length = (uint)(dataOffset - startPos);
            m.NameOffset = (uint)nameOffset;
            m.FormatOffset = (uint)formatOffset;
            m.NumItems = (uint)map.Items.Count;
            writer.WriteResource(ref startPos, ref m);
        }

        private void WriteFilters(IList<Filter> filters)
        {
            long startPos = writer.Position;
            writer.Position += Marshal.SizeOf<FilterBlock>();
            long offset = writer.Position + (filters.Count * Marshal.SizeOf<FilterEntry>());

            foreach (var filter in filters) {
                MarkObjectOffset(filter, writer.Position);

                var f = new FilterEntry();
                f.Value = filter.Value;
                f.Version = filter.Version;
                f.MessageId = GetMessageId(filter.Message);
                f.NameOffset = (uint)offset;
                f.TemplateOffset = GetObjectOffset(filter.Template);

                writer.WriteResource(ref f);
                writer.WriteString(ref offset, filter.Name.Value.ToPrefixedString());
            }
            writer.Position = offset;

            FilterBlock b;
            b.Magic = CrimsonTags.FLTR;
            b.Length = (uint)(offset - startPos);
            b.NumFilters = (uint)filters.Count;
            // This field is most likely just garbage. MC seems to reuse the
            // local struct to write the block header.
            b.Junk = GetObjectOffset(filters[filters.Count - 1].Template);
            writer.WriteResource(ref startPos, ref b);
        }

        private Guid CreateTemplateId(XDocument doc, byte[] types)
        {
            var xml = CreateTemplateXml(doc);
            if (UseLegacyTemplateIds)
                return CreateMD5TemplateId(xml, types);
            return CreateSha256TemplateId(xml, types);
        }

        private string CreateTemplateXml(XDocument doc)
        {
            var xml = doc.ToString(SaveOptions.DisableFormatting);

            // "<foo />" -> "<foo/>"
            if (EnableMessageCompilerCompat && doc.Root != null && doc.Root.IsEmpty && xml.EndsWith(" />"))
                xml = xml.Substring(0, xml.Length - 3) + "/>";

            return xml;
        }

        private static Guid CreateMD5TemplateId(string templateXml, byte[] types)
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

        private static Guid CreateSha256TemplateId(string templateXml, byte[] types)
        {
            var xmlBytes = Encoding.Unicode.GetBytes(templateXml);

            var magic = Encoding.ASCII.GetBytes("MS-WEVT\0");
            var bytes = new byte[xmlBytes.Length + (types.Length * 4)];
            Buffer.BlockCopy(xmlBytes, 0, bytes, 0, xmlBytes.Length);
            for (int i = 0; i < types.Length; ++i)
                BitConverterEx.FromUInt32(types[i], bytes, xmlBytes.Length + i * 4);

            var sha = SHA256.Create();
            sha.TransformBlock(magic, 0, magic.Length, null, 0);
            sha.TransformFinalBlock(bytes, 0, bytes.Length);

            var finalHash = new byte[16];
            Buffer.BlockCopy(sha.Hash, 0, finalHash, 0, finalHash.Length);
            finalHash[7] &= 0xF;
            finalHash[7] |= 0x50;

            return new Guid(finalHash);
        }

        private XDocument CreateXmlTemplate(Template template)
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

        private void WriteEvents(IList<Event> events)
        {
            events = events.ToList().StableSortBy(i => i.Value);

            long startPos = writer.Position;
            writer.Position += Marshal.SizeOf<EventBlock>();
            long offset = writer.Position + (events.Count * Marshal.SizeOf<EventEntry>());

            var keywordOffsets = new long[events.Count];
            for (int i = 0; i < events.Count; ++i) {
                if (events[i].Keywords.Count == 0)
                    continue;

                keywordOffsets[i] = offset;
                foreach (Keyword keyword in events[i].Keywords.OrderBy(x => x.Mask))
                    writer.WriteUInt32(ref offset, GetObjectOffset(keyword));
            }

            for (int i = 0; i < events.Count; ++i) {
                var evt = events[i];

                EventEntry e;
                e.Descriptor.Id = (ushort)evt.Value;
                e.Descriptor.Version = evt.Version;
                e.Descriptor.Channel = evt.ChannelValue;
                e.Descriptor.Level = evt.LevelValue;
                e.Descriptor.Opcode = evt.OpcodeValue;
                e.Descriptor.Task = evt.TaskValue;
                e.Descriptor.Keyword = evt.KeywordMask;
                e.MessageId = GetMessageId(evt.Message);
                e.TemplateOffset = GetObjectOffset(evt.Template);
                e.OpcodeOffset = GetObjectOffset(evt.Opcode);
                e.LevelOffset = GetObjectOffset(evt.Level);
                e.TaskOffset = GetObjectOffset(evt.Task);
                e.NumKeywords = (uint)evt.Keywords.Count;
                e.KeywordsOffset = (uint)keywordOffsets[i];
                e.ChannelOffset = GetObjectOffset(evt.Channel);

                writer.WriteResource(ref e);
            }
            writer.Position = offset;

            EventBlock b;
            b.Magic = CrimsonTags.EVNT;
            b.Length = (uint)(writer.Position - startPos);
            b.NumEvents = (uint)events.Count;
            b.Unknown = 0;
            writer.WriteResource(ref startPos, ref b);
        }

        private void MarkObjectOffset(object obj, long offset)
        {
            offsetMap.Add(obj, offset);
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
    }
}
