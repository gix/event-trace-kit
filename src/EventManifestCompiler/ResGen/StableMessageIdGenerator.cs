namespace EventManifestCompiler.ResGen
{
    using System;
    using EventManifestFramework.Schema;
    using EventManifestFramework.Support;

    internal sealed class StableMessageIdGenerator : IMessageIdGenerator
    {
        private readonly IDiagnostics diags;
        private uint generatedId = 1;
        private uint bitMapId = 1;
        private uint valueMapId = 1;

        public StableMessageIdGenerator(IDiagnostics diags)
        {
            if (diags == null)
                throw new ArgumentNullException(nameof(diags));
            this.diags = diags;
        }

        public uint CreateId(Provider provider)
        {
            return CreateId(provider, 0x9, provider.Index, 0, generatedId++);
        }

        public uint CreateId(Channel channel, Provider provider)
        {
            if (channel == null)
                return Message.UnusedId;
            return CreateId(channel, 0x9, provider.Index, 0, generatedId++);
        }

        public uint CreateId(Level level, Provider provider)
        {
            if (level == null)
                return Message.UnusedId;
            return CreateId(level, 0x5, provider.Index, 0, level.Value);
        }

        public uint CreateId(Task task, Provider provider)
        {
            if (task == null)
                return Message.UnusedId;
            return CreateId(task, 0x7, provider.Index, 0, task.Value);
        }

        public uint CreateId(Opcode opcode, Provider provider)
        {
            if (opcode == null)
                return Message.UnusedId;
            var taskId = opcode.Task != null ? (byte)opcode.Task.Value.Value : (byte)0;
            return CreateId(opcode, 0x3, provider.Index, taskId, opcode.Value);
        }

        public uint CreateId(Keyword keyword, Provider provider)
        {
            if (keyword == null)
                return Message.UnusedId;
            uint value = 0;
            ulong mask = keyword.Mask;
            for (; mask != 0; mask >>= 1)
                ++value;
            return CreateId(keyword, 0x1, provider.Index, 0, value);
        }

        public uint CreateId(Event evt, Provider provider)
        {
            if (evt == null)
                return Message.UnusedId;
            var version = evt.Version;
            byte tag = 0xB;
            if (evt.Channel != null && evt.Channel.IsGlobal())
                tag = 0;
            return CreateId(evt, tag, provider.Index, version, evt.Value);
        }

        public uint CreateId(MapItem item, Map map, Provider provider)
        {
            switch (map.Kind) {
                case MapKind.BitMap:
                    return CreateId((BitMapItem)item, (BitMap)map, provider);
                case MapKind.ValueMap:
                    return CreateId((ValueMapItem)item, (ValueMap)map, provider);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public uint CreateId(ValueMapItem item, ValueMap map, Provider provider)
        {
            if (item == null)
                return Message.UnusedId;
            return CreateId(item, 0xD, provider.Index, 0, valueMapId++);
        }

        public uint CreateId(BitMapItem item, BitMap map, Provider provider)
        {
            if (item == null)
                return Message.UnusedId;
            return CreateId(item, 0xF, provider.Index, 0, bitMapId++);
        }

        public uint CreateId(Filter filter, Provider provider)
        {
            if (filter == null)
                return Message.UnusedId;
            uint value =
                (0x80 << 16) +
                (((uint)filter.Version & 0xFF) << 8) +
                ((uint)filter.Value & 0xFF);
            return CreateId(filter, 0x5, provider.Index, value);
        }

        private uint CreateId(SourceItem item, byte tag, int providerIdx, byte version, uint value)
        {
            return CreateId(item, tag, providerIdx, ((uint)version << 16) + (value & 0xFFFF));
        }

        private uint CreateId(SourceItem item, byte tag, int providerIdx, uint value)
        {
            if (providerIdx > 15) {
                diags.ReportError("Provider {0} requires manual message ids. Only the first 16 providers can receive automated message ids.", providerIdx);
                throw new UserException();
            }
            if (providerIdx < 0)
                throw new Exception("Negative provider index.");
            return
                ((uint)(tag & 0xF) << 28) +
                (((uint)providerIdx & 0xF) << 24) +
                (value & 0xFFFFFF);
        }
    }
}
