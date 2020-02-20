namespace EventTraceKit.EventTracing.Compiler.ResGen
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Xml.Linq;
    using EventTraceKit.EventTracing.Compiler.BinXml;
    using EventTraceKit.EventTracing.Compiler.Native;
    using EventTraceKit.EventTracing.Compiler.ResGen.Crimson;
    using EventTraceKit.EventTracing.Compiler.Support;
    using EventTraceKit.EventTracing.Schema;
    using PropertyFlags = EventTraceKit.EventTracing.Compiler.ResGen.Crimson.PropertyFlags;

    internal sealed unsafe class EventTemplateDumperFast
    {
        private readonly ScopedWriter writer;

        private Dictionary<uint, Message> messageMap;
        private MemoryBuffer buffer;
        private byte* begin;
        private bool terse = true;

        public EventTemplateDumperFast(TextWriter writer)
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
            //using (var reader = IO.CreateBinaryReader(input))
            //    DumpMessageTable(reader);
        }

        public void DumpWevtTemplate(string inputFile)
        {
            using (var input = File.Open(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                DumpWevtTemplate(input);
        }

        public void DumpWevtTemplate(Stream input)
        {
            DumpWevtTemplate(input, null);
        }

        private sealed class MemoryBuffer : IDisposable
        {
            public readonly MemoryMappedFile mappedFile;
            private readonly MemoryMappedViewAccessor mappedViewAccessor;

            private readonly byte[] buffer;
            private GCHandle bufferHandle;

            private MemoryBuffer(MemoryMappedFile mappedFile, long offset, long size)
            {
                this.mappedFile = mappedFile;
                mappedViewAccessor = mappedFile.CreateViewAccessor(offset, size, MemoryMappedFileAccess.Read);
                BufferStart = (byte*)mappedViewAccessor.SafeMemoryMappedViewHandle.DangerousGetHandle();
            }

            private MemoryBuffer(byte[] buffer)
            {
                this.buffer = buffer;
                var handle = GCHandle.Alloc(buffer);
                BufferStart = (byte*)handle.AddrOfPinnedObject();
                bufferHandle = handle;
            }

            public static MemoryBuffer Create(Stream input)
            {
                if (input is FileStream inputFile /*&& inputFile.Length >= 4 * 4096*/) {
                    var mappedFile = MemoryMappedFile.CreateFromFile(
                        inputFile,
                        null,
                        inputFile.Length,
                        MemoryMappedFileAccess.Read,
                        null,
                        HandleInheritability.None,
                        true);
                    return new MemoryBuffer(mappedFile, 0, inputFile.Length);
                }

                var buffer = new byte[input.Length];
                input.Read(buffer, 0, buffer.Length);
                return new MemoryBuffer(buffer);
            }

            public byte* BufferStart { get; }

            public void Dispose()
            {
                mappedViewAccessor?.Dispose();
                mappedFile?.Dispose();
                if (bufferHandle.IsAllocated)
                    bufferHandle.Free();
            }
        }

        public void DumpWevtTemplate(Stream input, IEnumerable<Message> messages)
        {
            if (messages != null)
                messageMap = messages.ToDictionary(m => m.Id);

            try {
                buffer = MemoryBuffer.Create(input);
                begin = buffer.BufferStart;
                DumpCrimBlock(begin);
            } finally {
                begin = null;
                buffer?.Dispose();
                buffer = null;
                messageMap = null;
            }
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

        private void DumpCrimBlock(byte* r)
        {
            var header = (FileHeader*)r;
            VerifyMagic(0, header->Magic, CrimsonTags.CRIM);

            writer.PushDictScope("CRIM");
            writer.WriteHex("Magic", header->Magic);
            writer.WriteNumber("Length", header->Length);
            writer.WriteNumber("Major", header->Major);
            writer.WriteNumber("Minor", header->Minor);
            writer.WriteNumber("NumProviders", header->NumProviders);

            var providerEntries = new List<ProviderEntry>();
            for (uint i = 0; i < header->NumProviders; ++i) {
                var provider = ((ProviderEntry*)&header[1]) + i;
                providerEntries.Add(*provider);
                writer.WriteLine("Guid={0}, Offset=0x{1:X}", provider->Guid, provider->Offset);
            }

            foreach (var entry in providerEntries) {
                DumpWevtBlock(begin + entry.Offset, entry.Guid);
            }

            writer.PopScope();
        }

        private void DumpWevtBlock(byte* r, Guid providerId)
        {
            var block = (ProviderBlock*)r;
            VerifyMagic(0, block->Magic, CrimsonTags.WEVT);

            writer.PushDictScope($"WEVT({providerId})");
            writer.WriteHex("Magic", block->Magic);
            writer.WriteNumber("Length", block->Length);
            writer.WriteNumber("MessageId", block->MessageId);
            writer.WriteNumber("NumOffsets", block->NumOffsets);

            var offsets = new List<ProviderListOffset>();
            for (uint i = 0; i < block->NumOffsets; ++i) {
                var entry = ((ProviderListOffset*)&block[1]) + i;
                offsets.Add(*entry);
            }

            foreach (var pair in offsets) {
                var ptr = begin + pair.Offset;
                switch (pair.Type) {
                    case EventFieldKind.Level:
                        DumpLevels(ptr);
                        break;
                    case EventFieldKind.Task:
                        DumpTasks(ptr);
                        break;
                    case EventFieldKind.Opcode:
                        DumpOpcodes(ptr);
                        break;
                    case EventFieldKind.Keyword:
                        DumpKeywords(ptr);
                        break;
                    case EventFieldKind.Event:
                        DumpEvents(ptr);
                        break;
                    case EventFieldKind.Channel:
                        DumpChannels(ptr);
                        break;
                    case EventFieldKind.Maps:
                        DumpMaps(ptr);
                        break;
                    case EventFieldKind.Template:
                        DumpTemplates(ptr);
                        break;
                    case EventFieldKind.Filter:
                        DumpFilters(ptr);
                        break;
                    default:
                        writer.WriteLine("Unknown item type {0} at offset {1}.", pair.Type, pair.Offset);
                        break;
                }
            }

            writer.PopScope();
        }

        private void DumpEvents(byte* r)
        {
            var block = (EventBlock*)r;
            VerifyMagic(0, block->Magic, CrimsonTags.EVNT);
            var entries = (EventEntry*)&block[1];

            if (block->NumEvents == 0) {
                writer.WriteLine($"EVNT ({block->NumEvents} entries)");
                return;
            }

            if (terse) {
                writer.PushDictScope($"EVNT ({block->NumEvents} entries)");
            } else {
                writer.PushDictScope("EVNT");
                writer.WriteHex("Magic", block->Magic);
                writer.WriteHex("Length", block->Length);
                writer.WriteHex("NumEvents", block->NumEvents);
                writer.WriteHex("Unknown", block->Unknown);
            }

            for (uint i = 0; i < block->NumEvents; ++i) {
                var entry = &entries[i];

                uint[] keywordOffsets = ReadUInt32At(entry->KeywordsOffset, entry->NumKeywords);

                writer.WriteLine(
                    "Event({0}, Msg=0x{1:X} TP=0x{2:X} Op=0x{3:X} L=0x{4:X} T=0x{5:X} K={6}@0x{7:X}[{8}] C=0x{9:X}",
                    entry->Descriptor, entry->MessageId, entry->TemplateOffset,
                    entry->OpcodeOffset, entry->LevelOffset,
                    entry->TaskOffset, entry->NumKeywords, entry->KeywordsOffset,
                    string.Join(", ", keywordOffsets),
                    entry->ChannelOffset);
            }

            if (block->NumEvents != 0)
                writer.PopScope();
        }

        private ListBlock* ReadAndDumpListBlock(byte* r, uint tag)
        {
            var block = (ListBlock*)r;
            VerifyMagic(0, block->Magic, tag);
            var name = FormatMagic(tag);

            if (block->NumEntries == 0) {
                writer.WriteLine($"{name} ({block->NumEntries} entries)");
                return block;
            }

            if (terse) {
                writer.PushDictScope($"{name} ({block->NumEntries} entries)");
            } else {
                writer.PushDictScope(name);
                writer.WriteHex("Magic", block->Magic);
                writer.WriteHex("Length", block->Length);
                writer.WriteHex("NumEntries", block->NumEntries);
            }

            return block;
        }

        private void DumpChannels(byte* r)
        {
            var block = ReadAndDumpListBlock(r, CrimsonTags.CHAN);
            var entries = (ChannelEntry*)&block[1];

            for (uint i = 0; i < block->NumEntries; ++i) {
                var entry = &entries[i];
                var name = ReadStringAt(entry->NameOffset);

                if (terse) {
                    writer.WriteLine(
                        "Channel(Name=0x{0} ({1}), Flags={2}, Value={3}, Message=0x{4:X})",
                        entry->NameOffset, name, entry->Flags, entry->Value, entry->MessageId);
                } else {
                    writer.PushDictScope("Channel");
                    writer.WriteEnum("Flags", entry->Flags);
                    writer.WriteHex("NameOffset", entry->NameOffset, name);
                    writer.WriteNumber("Value", entry->Value);
                    writer.WriteHex("MessageId", entry->MessageId);
                    writer.PopScope();
                }
            }

            if (block->NumEntries != 0)
                writer.PopScope();
        }

        private void DumpLevels(byte* r)
        {
            var block = ReadAndDumpListBlock(r, CrimsonTags.LEVL);
            var entries = (LevelEntry*)&block[1];

            for (uint i = 0; i < block->NumEntries; ++i) {
                var entry = &entries[i];
                var name = ReadStringAt(entry->NameOffset);

                writer.WriteLine(
                    "Level(Name=0x{0} ({1}), Value={2}, Message=0x{3:X})",
                    entry->NameOffset, name, entry->Value, entry->MessageId);
            }

            if (block->NumEntries != 0)
                writer.PopScope();
        }

        private void DumpTasks(byte* r)
        {
            var block = ReadAndDumpListBlock(r, CrimsonTags.TASK);
            var entries = (TaskEntry*)&block[1];

            for (uint i = 0; i < block->NumEntries; ++i) {
                var entry = &entries[i];
                var name = ReadStringAt(entry->NameOffset);

                writer.WriteLine(
                    "Task(Name=0x{0} ({1}), Value={2}, Message=0x{3:X}, Guid={4})",
                    name, entry->NameOffset, entry->Value, entry->MessageId, entry->EventGuid);
            }

            if (block->NumEntries != 0)
                writer.PopScope();
        }

        private void DumpOpcodes(byte* r)
        {
            var block = ReadAndDumpListBlock(r, CrimsonTags.OPCO);
            var entries = (OpcodeEntry*)&block[1];

            for (uint i = 0; i < block->NumEntries; ++i) {
                var entry = &entries[i];
                var name = ReadStringAt(entry->NameOffset);

                writer.WriteLine(
                    "Opcode(Name=0x{0} ({1}), Value={2}, Message=0x{3:X}, TaskId={4})",
                    name, entry->NameOffset, entry->Value, entry->MessageId, entry->TaskId);
            }

            if (block->NumEntries != 0)
                writer.PopScope();
        }

        private void DumpKeywords(byte* r)
        {
            var block = ReadAndDumpListBlock(r, CrimsonTags.KEYW);
            var entries = (KeywordEntry*)&block[1];

            for (uint i = 0; i < block->NumEntries; ++i) {
                var entry = &entries[i];
                var name = ReadStringAt(entry->NameOffset);

                writer.WriteLine(
                    "Keyword(Name=0x{0} ({1}), Mask={2}, Message=0x{3:X})",
                    name, entry->NameOffset, entry->Mask, entry->MessageId);
            }

            if (block->NumEntries != 0)
                writer.PopScope();
        }

        private void DumpMaps(byte* r)
        {
            var block = ReadAndDumpListBlock(r, CrimsonTags.MAPS);

            var offsets = (uint*)&block[1];
            for (uint i = 0; i < block->NumEntries; ++i)
                writer.WriteLine("[Offset {0}] 0x{1:X}", i, offsets[i]);

            for (uint i = 0; i < block->NumEntries; ++i)
                DumpMap(begin + offsets[i]);

            if (block->NumEntries != 0)
                writer.PopScope();
        }

        private void DumpMap(byte* r)
        {
            var block = (MapEntry*)r;
            if (block->Magic != CrimsonTags.VMAP && block->Magic != CrimsonTags.BMAP)
                throw new InternalException("Unknown map magic {0}", block->Magic);

            var name = ReadStringAt(block->NameOffset);

            writer.PushListScope(
                "Map(Name=0x{0} ({1}), Flags={2})",
                block->NameOffset, name, block->Flags);

            for (uint i = 0; i < block->NumItems; ++i) {
                var item = (MapItemEntry*)r;
                writer.WriteLine("Item({0}, Message=0x{1:X})", item->Value, item->MessageId);
            }

            writer.PopScope();
        }

        private void DumpTemplates(byte* r)
        {
            var block = ReadAndDumpListBlock(r, CrimsonTags.TTBL);

            var ptr = (byte*)&block[1];
            for (uint i = 0; i < block->NumEntries; ++i)
                DumpTemplate(ref ptr);

            writer.PopScope();
        }

        private void DumpTemplate(ref byte* r)
        {
            var entry = (TemplateEntry*)r;
            VerifyMagic(0, entry->Magic, CrimsonTags.TEMP);

            writer.PushDictScope(
                "Template(params={0}, props={1}, id={2}, flags={3})",
                entry->NumParams, entry->NumProperties, entry->TemplateId, entry->Flags);

            XDocument doc = BinXmlReader.Read(buffer.mappedFile.CreateViewStream((byte*)&entry[1] - begin, entry->Length, MemoryMappedFileAccess.Read));

            writer.WriteStringBlock("BinXml", doc.ToString());

            writer.PushListScope("Properties");
            var properties = (PropertyEntry*)(begin + entry->PropertyOffset);
            var structProperties = (PropertyEntry*)(begin + entry->PropertyOffset + entry->NumParams * Marshal.SizeOf<PropertyEntry>());
            for (uint i = 0; i < entry->NumParams; ++i) {
                DumpProperty(&properties[i], &structProperties);
            }

            writer.PopScope();

            writer.PopScope();
        }

        private void DumpProperty(PropertyEntry* entry, PropertyEntry** structPropertyOffset)
        {
            if ((entry->Flags & PropertyFlags.Struct) != 0) {
                var name = ReadStringAt(entry->NameOffset).TrimEnd('\0');

                writer.PushListScope(
                    "Struct(Name=0x{0:X} ({1}), flags={2} (0x{2:X}), fields={3} (@{4}), count={5}, length={6})",
                    entry->NameOffset,
                    name,
                    entry->Flags,
                    entry->structType.NumStructMembers,
                    entry->structType.StructStartIndex,
                    entry->Count,
                    entry->Length);

                for (int i = 0; i < entry->structType.NumStructMembers; ++i) {
                    DumpProperty(*structPropertyOffset, null);
                    ++*structPropertyOffset;
                }

                writer.PopScope();
            } else {
                byte inputType = entry->nonStructType.InputType;
                byte outputType = entry->nonStructType.OutputType;
                uint mapOffset = entry->nonStructType.MapOffset;
                var name = ReadStringAt(entry->NameOffset).TrimEnd('\0');

                writer.WriteLine(
                    "Data(Name=0x{0:X} ({1}), flags={2} (0x{2:X}), in={3:D} ({3}), out={4:D} ({4}), count={5}, length={5}, map=0x{7:X})",
                    entry->NameOffset,
                    name,
                    entry->Flags,
                    (InTypeKind)inputType,
                    (OutTypeKind)outputType,
                    entry->Count,
                    entry->Length,
                    mapOffset);
            }
        }

        private void DumpFilters(byte* r)
        {
            var block = (FilterBlock*)r;
            VerifyMagic(0, block->Magic, CrimsonTags.FLTR);
            var entries = (FilterEntry*)&block[1];

            if (block->NumFilters == 0) {
                writer.WriteLine($"FLTR ({block->NumFilters} entries)");
                return;
            }

            if (terse) {
                writer.PushDictScope($"FLTR ({block->NumFilters} entries)");
            } else {
                writer.PushDictScope("FLTR");
                writer.WriteHex("Magic", block->Magic);
                writer.WriteHex("Length", block->Length);
                writer.WriteHex("NumEntries", block->NumFilters);
                writer.WriteHex("Junk", block->Junk);
            }

            for (uint i = 0; i < block->NumFilters; ++i) {
                var entry = &entries[i];
                var name = ReadStringAt(entry->NameOffset);

                writer.WriteLine(
                    "Filter(Name=0x{0:X} ({1}), Value={2}, Version={3}, Message=0x{4:X}, Template=0x{5:X})",
                    entry->NameOffset, name, entry->Value, entry->Version, entry->MessageId, entry->TemplateOffset);
            }

            writer.PopScope();
        }

        private string ReadStringAt(long offset)
        {
            uint byteCount = *(uint*)(begin + offset);
            return new string((char*)(begin + offset + 4), 0, ((int)byteCount - 4) / 2);
        }

        private uint[] ReadUInt32At(long offset, uint count)
        {
            var ptr = (uint*)(begin + offset);
            var values = new uint[count];
            for (uint i = 0; i < count; ++i)
                values[i] = ptr[i];
            return values;
        }

        private string LookupMessage(uint messageId)
        {
            if (messageMap != null && messageMap.TryGetValue(messageId, out Message message))
                return message.Text;
            return null;
        }
    }
}
