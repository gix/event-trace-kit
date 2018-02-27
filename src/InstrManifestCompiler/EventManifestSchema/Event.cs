namespace InstrManifestCompiler.EventManifestSchema
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using InstrManifestCompiler.EventManifestSchema.Base;
    using InstrManifestCompiler.Support;

    [DebuggerDisplay("{Value} ({Version})")]
    public sealed class Event : ProviderItem
    {
        public Event(StructValue<uint> value)
            : this(value, new StructValue<byte>(0))
        {
        }

        public Event(StructValue<uint> value, StructValue<byte> version)
        {
            Value = value;
            Version = version;
            Keywords = new List<Keyword>();
        }

        public StructValue<uint> Value { get; private set; }
        public StructValue<byte> Version { get; private set; }
        public RefValue<string> Symbol { get; set; }
        public Channel Channel { get; set; }
        public Level Level { get; set; }
        public Task Task { get; set; }
        public Opcode Opcode { get; set; }
        public List<Keyword> Keywords { get; private set; }
        public Template Template { get; set; }
        public NullableValue<bool> NotLogged { get; set; }
        public LocalizedString Message { get; set; }

        internal EnableBit EnableBit { get; set; }

        public byte ChannelValue => Channel?.Value.GetValueOrDefault() ?? (byte)0;

        public byte LevelValue => Level?.Value ?? (byte)0;

        public ushort TaskValue => Task?.Value ?? (ushort)0;

        public byte OpcodeValue => Opcode?.Value ?? (byte)0;

        public ulong KeywordMask
        {
            get
            {
                ulong mask = 0;
                foreach (var keyword in Keywords)
                    mask |= keyword.Mask;

                if (Channel != null)
                    mask |= (ulong)1 << (63 - Channel.Index);

                return mask;
            }
        }

        public override void Accept(IProviderItemVisitor visitor)
        {
            visitor.VisitEvent(this);
        }
    }
}
