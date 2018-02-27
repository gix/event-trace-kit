namespace InstrManifestCompiler.ResGen
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using InstrManifestCompiler.EventManifestSchema;
    using InstrManifestCompiler.Extensions;
    using InstrManifestCompiler.Native;

    internal class MessageTableWriter : IDisposable
    {
        private const int ResourceEntryFixedSize = 4;
        private const string MessageSuffix = "\r\n\0";

        private readonly BinaryWriter writer;

        public MessageTableWriter(Stream output)
        {
            writer = new BinaryWriter(output, Encoding.ASCII, true);
        }

        void IDisposable.Dispose()
        {
            writer.Dispose();
        }

        public void Close()
        {
            ((IDisposable)this).Dispose();
        }

        public void Write(IEnumerable<Message> messages, IDiagnostics diags)
        {
            var uniqueMessages = new List<Message>();
            var messageIds = new HashSet<uint>();

            foreach (var message in messages.Where(m => m.IsUsed)) {
                if (messageIds.Contains(message.Id)) {
                    diags.ReportError(
                        "Message '{0}' has duplicate id 0x{1:X}.",
                        message.Name,
                        message.Id);
                    continue;
                }
                messageIds.Add(message.Id);
                uniqueMessages.Add(message);
            }

            uniqueMessages.StableSortBy(m => m.Id);

            var blocks = BuildBlocks(uniqueMessages);
            WriteResourceData(blocks, uniqueMessages);
        }

        private List<MessageBlock> BuildBlocks(IEnumerable<Message> messages)
        {
            var blocks = new List<MessageBlock>();
            MessageBlock block = null;
            foreach (var message in messages) {
                if (block != null && block.HighId + 1 == message.Id) {
                    ++block.HighId;
                    continue;
                }

                block = new MessageBlock {
                    LowId = message.Id,
                    HighId = message.Id,
                };
                blocks.Add(block);
            }

            return blocks;
        }

        private void WriteResourceData(List<MessageBlock> blocks, List<Message> messages)
        {
            writer.WriteUInt32((uint)blocks.Count);

            int i = 0;
            int offset = 4 + (blocks.Count * Marshal.SizeOf<MESSAGE_RESOURCE_BLOCK>());
            foreach (var block in blocks) {
                var mbr = new MESSAGE_RESOURCE_BLOCK {
                    LowId = block.LowId,
                    HighId = block.HighId,
                    Offset = (uint)offset,
                };
                WriteResourceBlock(ref mbr);

                for (; i < messages.Count; ++i) {
                    var message = messages[i];
                    if (message.Id > block.HighId)
                        break;

                    offset += GetResourceEntryByteCount(message);
                }
            }

            foreach (var message in messages)
                WriteResourceEntry(message);
        }

        private void WriteResourceBlock(ref MESSAGE_RESOURCE_BLOCK block)
        {
            writer.WriteUInt32(block.LowId);
            writer.WriteUInt32(block.HighId);
            writer.WriteUInt32(block.Offset);
        }

        private void WriteResourceEntry(Message message)
        {
            byte[] bytes = EncodeMessage(message);
            int flags = message.Ansi ? 0 : NativeMethods.MESSAGE_RESOURCE_UNICODE;

            writer.WriteUInt16((ushort)(bytes.Length + ResourceEntryFixedSize));
            writer.WriteUInt16((ushort)flags);
            writer.Write(bytes, 0, bytes.Length);
        }

        private Encoding EncodingFor(Message message)
        {
            return message.Ansi ? Encoding.ASCII : Encoding.Unicode;
        }

        private int GetMessageByteCount(Message message)
        {
            var encoding = EncodingFor(message);
            int count = encoding.GetByteCount(message.Text) + encoding.GetByteCount(MessageSuffix);
            return (count + 3) & ~3;
        }

        private int GetResourceEntryByteCount(Message message)
        {
            return GetMessageByteCount(message) + ResourceEntryFixedSize;
        }

        private byte[] EncodeMessage(Message message)
        {
            var bytes = new byte[GetMessageByteCount(message)];
            var encoding = EncodingFor(message);
            int offset = encoding.GetBytes(message.Text, 0, message.Text.Length, bytes, 0);
            encoding.GetBytes(MessageSuffix, 0, MessageSuffix.Length, bytes, offset);
            return bytes;
        }

        private sealed class MessageBlock
        {
            public uint LowId;
            public uint HighId;
        }
    }
}
