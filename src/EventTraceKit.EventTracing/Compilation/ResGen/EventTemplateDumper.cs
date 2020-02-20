namespace EventTraceKit.EventTracing.Compilation.ResGen
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Xml.Linq;
    using EventTraceKit.EventTracing.Compilation.BinXml;
    using EventTraceKit.EventTracing.Compilation.ResGen.Crimson;
    using EventTraceKit.EventTracing.Compilation.Support;
    using EventTraceKit.EventTracing.Internal.Extensions;
    using EventTraceKit.EventTracing.Internal.Native;
    using EventTraceKit.EventTracing.Schema;
    using EventTraceKit.EventTracing.Support;
    using EventDescriptor = Crimson.EventDescriptor;
    using PropertyFlags = Crimson.PropertyFlags;

    public sealed class EventTemplateDumper
    {
        private readonly ScopedWriter writer;

        private Dictionary<uint, Message> messageMap;
        private bool terse = true;

        public EventTemplateDumper(TextWriter writer)
        {
            this.writer = new ScopedWriter(writer);
        }

        public void DumpMessageTable(string inputFile)
        {
            using (var input = File.OpenRead(inputFile))
                DumpMessageTable(input);
        }

        public void DumpMessageTable(Stream input)
        {
            using (var reader = IO.CreateBinaryReader(input))
                DumpMessageTable(reader);
        }

        public void DumpMessageTableResource(string moduleFile)
        {
            var module = SafeModuleHandle.LoadImageResource(moduleFile);
            if (module.IsInvalid)
                throw new Win32Exception();

            const int RT_MESSAGETABLE = 11;
            using (module)
            using (var stream = module.OpenResource((IntPtr)RT_MESSAGETABLE, 1))
                DumpMessageTable(stream);
        }

        public void DumpWevtTemplate(string inputFile)
        {
            using (var input = File.OpenRead(inputFile))
                DumpWevtTemplate(input);
        }

        public void DumpWevtTemplate(Stream input)
        {
            using (var reader = new BinaryReader(input))
                DumpCrimBlock(reader);
        }

        public void DumpWevtTemplate(Stream input, IEnumerable<Message> messages)
        {
            if (messages != null)
                messageMap = messages.ToDictionary(m => m.Id);

            try {
                using (var reader = new BinaryReader(input))
                    DumpCrimBlock(reader);
            } finally {
                messageMap = null;
            }
        }

        public void DumpWevtTemplateResource(string moduleFile)
        {
            var module = SafeModuleHandle.LoadImageResource(moduleFile);
            if (module.IsInvalid)
                throw new Win32Exception();

            using (module)
            using (var stream = module.OpenResource("WEVT_TEMPLATE", 1))
                DumpWevtTemplate(stream);
        }

        public void DumpWevtTemplateResource(IntPtr moduleHandle)
        {
            if (moduleHandle == IntPtr.Zero)
                throw new ArgumentNullException(nameof(moduleHandle));

            using (var module = new SafeModuleHandle(moduleHandle, false))
            using (var stream = module.OpenResource("WEVT_TEMPLATE", 1))
                DumpWevtTemplate(stream);
        }

        private void DumpMessageTable(BinaryReader r)
        {
            writer.PushListScope("MESSAGE_RESOURCE_DATA");

            uint blockCount = r.ReadUInt32();

            writer.WriteNumber("NumberOfBlocks", blockCount);

            var resourceBlocks = new List<MESSAGE_RESOURCE_BLOCK>();
            for (uint i = 0; i < blockCount; ++i) {
                uint lowId = r.ReadUInt32();
                uint highId = r.ReadUInt32();
                uint offset = r.ReadUInt32();
                var block = new MESSAGE_RESOURCE_BLOCK {
                    LowId = lowId,
                    HighId = highId,
                    Offset = offset
                };
                resourceBlocks.Add(block);

                writer.PushDictScope("MESSAGE_RESOURCE_BLOCK");
                writer.WriteHex("LowId", block.LowId);
                writer.WriteHex("HighId", block.HighId);
                writer.WriteNumber("Offset", block.Offset);
                writer.PopScope();
            }

            resourceBlocks.Sort((x, y) => x.Offset.CompareTo(y.Offset));

            for (int i = 0; i < resourceBlocks.Count; ++i) {
                var block = resourceBlocks[i];
                r.BaseStream.Position = block.Offset;

                writer.PushListScope($"Block {i} (0x{block.LowId:X}-0x{block.HighId:X})");
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

                    writer.PushDictScope("MESSAGE_RESOURCE_ENTRY");
                    writer.WriteNumber("Length", length);
                    writer.WriteNumber("Flags", flags);
                    writer.WriteString("Text", text);
                    writer.PopScope();
                }

                writer.PopScope();
            }

            writer.PopScope();
        }

        private string FormatMagic(uint magic)
        {
            return Encoding.ASCII.GetString(BitConverter.GetBytes(magic)).Replace('\0', ' ');
        }

        private void VerifyMagic(long offset, uint magic, uint expected)
        {
            if (magic != expected) {
                throw ParseError(
                    offset,
                    "Invalid magic 0x{0:X8} ({1}), expected 0x{2:X8} ({3})",
                    magic,
                    FormatMagic(magic),
                    CrimsonTags.CRIM,
                    FormatMagic(expected));
            }
        }

        private Exception ParseError(long offset, string format, params object[] args)
        {
            string message = string.Format(format, args);
            return new InvalidOperationException($"At 0x{offset:X}: {message}");
        }

        private void DumpCrimBlock(BinaryReader r)
        {
            var header = r.ReadStruct<FileHeader>();
            VerifyMagic(0, header.Magic, CrimsonTags.CRIM);

            writer.PushDictScope("CRIM");
            writer.WriteHex("Magic", header.Magic);
            writer.WriteNumber("Length", header.Length);
            writer.WriteNumber("Major", header.Major);
            writer.WriteNumber("Minor", header.Minor);
            writer.WriteNumber("NumProviders", header.NumProviders);

            var providerEntries = new List<ProviderEntry>();
            for (uint i = 0; i < header.NumProviders; ++i) {
                var provider = r.ReadStruct<ProviderEntry>();
                providerEntries.Add(provider);
                writer.WriteLine("Guid={0}, Offset=0x{1:X}", provider.Guid, provider.Offset);
            }

            foreach (var entry in providerEntries) {
                r.BaseStream.Position = entry.Offset;
                DumpWevtBlock(r, entry.Guid);
            }

            writer.PopScope();
        }

        private void DumpWevtBlock(BinaryReader r, Guid providerId)
        {
            var block = r.ReadStruct<ProviderBlock>();
            VerifyMagic(0, block.Magic, CrimsonTags.WEVT);

            writer.PushDictScope($"WEVT({providerId})");
            writer.WriteHex("Magic", block.Magic);
            writer.WriteNumber("Length", block.Length);
            writer.WriteNumber("MessageId", block.MessageId);
            writer.WriteNumber("NumOffsets", block.NumOffsets);

            var lists = new List<ProviderListOffset>();
            for (uint i = 0; i < block.NumOffsets; ++i) {
                var plo = new ProviderListOffset();
                plo.Type = (EventFieldKind)r.ReadUInt32();
                plo.Offset = r.ReadUInt32();
                lists.Add(plo);
            }

            foreach (var list in lists) {
                r.BaseStream.Position = list.Offset;
                switch (list.Type) {
                    case EventFieldKind.Level:
                        DumpLevels(r);
                        break;
                    case EventFieldKind.Task:
                        DumpTasks(r);
                        break;
                    case EventFieldKind.Opcode:
                        DumpOpcodes(r);
                        break;
                    case EventFieldKind.Keyword:
                        DumpKeywords(r);
                        break;
                    case EventFieldKind.Event:
                        DumpEvents(r);
                        break;
                    case EventFieldKind.Channel:
                        DumpChannels(r);
                        break;
                    case EventFieldKind.Maps:
                        DumpMaps(r);
                        break;
                    case EventFieldKind.Template:
                        DumpTemplates(r);
                        break;
                    case EventFieldKind.Filter:
                        DumpFilters(r);
                        break;
                    default:
                        writer.WriteLine("Unknown item type {0} at offset {1}.", list.Type, list.Offset);
                        break;
                }
            }

            writer.PopScope();
        }

        private void DumpEvents(BinaryReader r)
        {
            var block = r.ReadStruct<EventBlock>();
            VerifyMagic(0, block.Magic, CrimsonTags.EVNT);

            if (block.NumEvents == 0) {
                writer.WriteLine($"EVNT ({block.NumEvents} entries)");
                return;
            }

            if (terse) {
                writer.PushDictScope($"EVNT ({block.NumEvents} entries)");
            } else {
                writer.PushDictScope("EVNT");
                writer.WriteHex("Magic", block.Magic);
                writer.WriteHex("Length", block.Length);
                writer.WriteHex("NumEvents", block.NumEvents);
                writer.WriteHex("Unknown", block.Unknown);
            }

            for (uint i = 0; i < block.NumEvents; ++i) {
                var entry = r.ReadStruct<EventEntry>();
                uint[] keywordOffsets = ReadUInt32At(r, entry.KeywordsOffset, entry.NumKeywords);

                writer.WriteLine(
                    "Event({0}, Msg=0x{1:X} TP=0x{2:X} Op=0x{3:X} L=0x{4:X} T=0x{5:X} K={6}@0x{7:X}[{8}] C=0x{9:X}",
                    FormatDescriptor(in entry.Descriptor), entry.MessageId,
                    entry.TemplateOffset, entry.OpcodeOffset, entry.LevelOffset,
                    entry.TaskOffset, entry.NumKeywords, entry.KeywordsOffset,
                    string.Join(", ", keywordOffsets),
                    entry.ChannelOffset);
            }

            if (block.NumEvents != 0)
                writer.PopScope();
        }

        private static string FormatDescriptor(in EventDescriptor d)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "EvtDescr({0} V={1} C={2} L={3} O={4} T={5} K=0x{6:X})",
                d.Id, d.Version, d.Channel, d.Level, d.Opcode, d.Task, d.Keyword);
        }

        private ListBlock ReadAndDumpListBlock(BinaryReader r, uint tag)
        {
            var block = r.ReadStruct<ListBlock>();
            VerifyMagic(0, block.Magic, tag);
            var name = FormatMagic(tag);

            if (block.NumEntries == 0) {
                writer.WriteLine($"{name} ({block.NumEntries} entries)");
                return block;
            }

            if (terse) {
                writer.PushDictScope($"{name} ({block.NumEntries} entries)");
            } else {
                writer.PushDictScope(name);
                writer.WriteHex("Magic", block.Magic);
                writer.WriteHex("Length", block.Length);
                writer.WriteHex("NumEntries", block.NumEntries);
            }

            return block;
        }

        private void DumpChannels(BinaryReader r)
        {
            var block = ReadAndDumpListBlock(r, CrimsonTags.CHAN);

            for (uint i = 0; i < block.NumEntries; ++i) {
                var entry = r.ReadStruct<ChannelEntry>();
                var name = ReadStringAt(r, entry.NameOffset);

                if (terse) {
                    writer.WriteLine(
                        "Channel(Name=0x{0} ({1}), Flags={2}, Value={3}, Message=0x{4:X})",
                        entry.NameOffset, name, entry.Flags, entry.Value, entry.MessageId);
                } else {
                    writer.PushDictScope("Channel");
                    writer.WriteEnum("Flags", entry.Flags);
                    writer.WriteHex("NameOffset", entry.NameOffset, name);
                    writer.WriteNumber("Value", entry.Value);
                    writer.WriteHex("MessageId", entry.MessageId);
                    writer.PopScope();
                }
            }

            if (block.NumEntries != 0)
                writer.PopScope();
        }

        private void DumpLevels(BinaryReader r)
        {
            var block = ReadAndDumpListBlock(r, CrimsonTags.LEVL);

            for (uint i = 0; i < block.NumEntries; ++i) {
                var entry = r.ReadStruct<LevelEntry>();
                var name = ReadStringAt(r, entry.NameOffset);

                writer.WriteLine(
                    "Level(Name=0x{0} ({1}), Value={2}, Message=0x{3:X})",
                    entry.NameOffset, name, entry.Value, entry.MessageId);
            }

            if (block.NumEntries != 0)
                writer.PopScope();
        }

        private void DumpTasks(BinaryReader r)
        {
            var block = ReadAndDumpListBlock(r, CrimsonTags.TASK);

            for (uint i = 0; i < block.NumEntries; ++i) {
                var entry = r.ReadStruct<TaskEntry>();
                var name = ReadStringAt(r, entry.NameOffset);

                writer.WriteLine(
                    "Task(Name=0x{0} ({1}), Value={2}, Message=0x{3:X}, Guid={4})",
                    name, entry.NameOffset, entry.Value, entry.MessageId, entry.EventGuid);
            }

            if (block.NumEntries != 0)
                writer.PopScope();
        }

        private void DumpOpcodes(BinaryReader r)
        {
            var block = ReadAndDumpListBlock(r, CrimsonTags.OPCO);

            for (uint i = 0; i < block.NumEntries; ++i) {
                var entry = r.ReadStruct<OpcodeEntry>();
                var name = ReadStringAt(r, entry.NameOffset);

                writer.WriteLine(
                    "Opcode(Name=0x{0} ({1}), Value={2}, Message=0x{3:X}, TaskId={4})",
                    name, entry.NameOffset, entry.Value, entry.MessageId, entry.TaskId);
            }

            if (block.NumEntries != 0)
                writer.PopScope();
        }

        private void DumpKeywords(BinaryReader r)
        {
            var block = ReadAndDumpListBlock(r, CrimsonTags.KEYW);

            for (uint i = 0; i < block.NumEntries; ++i) {
                var entry = r.ReadStruct<KeywordEntry>();
                var name = ReadStringAt(r, entry.NameOffset);

                writer.WriteLine(
                    "Keyword(Name=0x{0} ({1}), Mask={2}, Message=0x{3:X})",
                    name, entry.NameOffset, entry.Mask, entry.MessageId);
            }

            if (block.NumEntries != 0)
                writer.PopScope();
        }

        private void DumpMaps(BinaryReader r)
        {
            var block = ReadAndDumpListBlock(r, CrimsonTags.MAPS);

            var offsets = new uint[block.NumEntries];
            for (uint i = 0; i < block.NumEntries; ++i) {
                offsets[i] = r.ReadUInt32();
                writer.WriteLine("[Offset {0}] 0x{1:X}", i, offsets[i]);
            }

            foreach (var offset in offsets) {
                r.BaseStream.Position = offset;
                DumpMap(r);
            }

            if (block.NumEntries != 0)
                writer.PopScope();
        }

        private void DumpMap(BinaryReader r)
        {
            long offset = r.BaseStream.Position;

            var block = r.ReadStruct<MapEntry>();
            if (block.Magic != CrimsonTags.VMAP && block.Magic != CrimsonTags.BMAP)
                throw new InternalException("Unknown map magic {0}", block.Magic);

            var name = ReadStringAt(r, block.NameOffset);

            writer.PushListScope(
                "Map(Name=0x{0} ({1}), Flags={2})",
                block.NameOffset, name, block.Flags);

            for (uint i = 0; i < block.NumItems; ++i) {
                var item = r.ReadStruct<MapItemEntry>();
                writer.WriteLine("Item({0}, Message=0x{1:X})", item.Value, item.MessageId);
            }

            writer.PopScope();

            if (r.BaseStream.Position != offset + block.Length)
                throw new IOException("Block length does not match actual read length.");
        }

        private void DumpTemplates(BinaryReader r)
        {
            var block = ReadAndDumpListBlock(r, CrimsonTags.TTBL);

            for (uint i = 0; i < block.NumEntries; ++i)
                DumpTemplate(r);

            writer.PopScope();
        }

        private void DumpTemplate(BinaryReader r)
        {
            long offset = r.BaseStream.Position;

            var entry = r.ReadStruct<TemplateEntry>();
            VerifyMagic(0, entry.Magic, CrimsonTags.TEMP);

            writer.PushDictScope(
                "Template(params={0}, props={1}, id={2}, flags={3})",
                entry.NumParams, entry.NumProperties, entry.TemplateId, entry.Flags);

            XDocument doc = BinXmlReader.Read(r.BaseStream);
            r.BaseStream.Position = entry.PropertyOffset;

            writer.WriteStringBlock("BinXml", doc.ToString());

            writer.PushListScope("Properties");
            long structPropertyOffset = entry.PropertyOffset + entry.NumParams * Marshal.SizeOf<PropertyEntry>();
            for (uint i = 0; i < entry.NumParams; ++i) {
                r.BaseStream.Position = entry.PropertyOffset + i * Marshal.SizeOf<PropertyEntry>();
                DumpProperty(r, ref structPropertyOffset);
            }

            writer.PopScope();

            writer.PopScope();

            r.BaseStream.Position = offset + entry.Length;
        }

        private void DumpProperty(BinaryReader r, ref long structPropertyOffset)
        {
            var entry = r.ReadStruct<PropertyEntry>();

            if ((entry.Flags & PropertyFlags.Struct) != 0) {
                var name = ReadStringAt(r, entry.NameOffset).TrimEnd('\0');

                writer.PushListScope(
                    "Struct(Name=0x{0:X} ({1}), flags={2} (0x{2:X}), fields={3} (@{4}), count={5}, length={6})",
                    entry.NameOffset,
                    name,
                    entry.Flags,
                    entry.structType.NumStructMembers,
                    entry.structType.StructStartIndex,
                    entry.Count,
                    entry.Length);

                r.BaseStream.Position = structPropertyOffset;
                long dummy = -1;
                for (int i = 0; i < entry.structType.NumStructMembers; ++i)
                    DumpProperty(r, ref dummy);
                structPropertyOffset = r.BaseStream.Position;

                writer.PopScope();
            } else {
                var inputType = entry.nonStructType.InputType;
                var outputType = entry.nonStructType.OutputType;
                uint mapOffset = entry.nonStructType.MapOffset;
                var name = ReadStringAt(r, entry.NameOffset).TrimEnd('\0');

                writer.WriteLine(
                    "Data(Name=0x{0:X} ({1}), flags={2} (0x{2:X}), in={3:D} ({3}), out={4:D} ({4}), count={5}, length={5}, map=0x{7:X})",
                    entry.NameOffset,
                    name,
                    entry.Flags,
                    inputType,
                    outputType,
                    entry.Count,
                    entry.Length,
                    mapOffset);
            }
        }

        private void DumpFilters(BinaryReader r)
        {
            var block = r.ReadStruct<FilterBlock>();
            VerifyMagic(0, block.Magic, CrimsonTags.FLTR);

            if (block.NumFilters == 0) {
                writer.WriteLine($"FLTR ({block.NumFilters} entries)");
                return;
            }

            if (terse) {
                writer.PushDictScope($"FLTR ({block.NumFilters} entries)");
            } else {
                writer.PushDictScope("FLTR");
                writer.WriteHex("Magic", block.Magic);
                writer.WriteHex("Length", block.Length);
                writer.WriteHex("NumEntries", block.NumFilters);
                writer.WriteHex("Junk", block.Junk);
            }

            for (uint i = 0; i < block.NumFilters; ++i) {
                var entry = r.ReadStruct<FilterEntry>();
                var name = ReadStringAt(r, entry.NameOffset);

                writer.WriteLine(
                    "Filter(Name=0x{0:X} ({1}), Value={2}, Version={3}, Message=0x{4:X}, Template=0x{5:X})",
                    entry.NameOffset, name, entry.Value, entry.Version, entry.MessageId, entry.TemplateOffset);
            }

            writer.PopScope();
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
    }
}
