namespace EventManifestFramework.Schema
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using EventManifestFramework.Schema.Base;
    using EventManifestFramework.Support;

    [DebuggerDisplay("{Value} ({Version})")]
    public sealed class Event : ProviderItem
    {
        private Channel channel;
        private ulong channelBit;

        public Event(LocatedVal<uint> value)
            : this(value, new LocatedVal<byte>(0))
        {
        }

        public Event(LocatedVal<uint> value, LocatedVal<byte> version)
        {
            Value = value;
            Version = version;
        }

        public LocatedVal<uint> Value { get; }
        public LocatedVal<byte> Version { get; }
        public LocatedRef<string> Symbol { get; set; }

        public Channel Channel
        {
            get => channel;
            set
            {
                channel = value;

                if (Channel == null || Channel.IsTraceClassic() || Channel.IsTraceLogging())
                    channelBit = 0;
                else
                    channelBit |= (ulong)1 << (63 - Channel.Index);
            }
        }

        public Level Level { get; set; }
        public Task Task { get; set; }
        public Opcode Opcode { get; set; }
        public List<Keyword> Keywords { get; } = new List<Keyword>();
        public Template Template { get; set; }
        public LocatedNullable<bool> NotLogged { get; set; }
        public LocalizedString Message { get; set; }

        public EnableBit EnableBit { get; internal set; }

        public byte ChannelValue => Channel?.Value.GetValueOrDefault() ?? 0;
        public byte LevelValue => Level?.Value ?? 0;
        public ushort TaskValue => Task?.Value ?? 0;
        public byte OpcodeValue => Opcode?.Value ?? 0;

        public ulong KeywordMask
        {
            get
            {
                ulong mask = 0;
                foreach (var keyword in Keywords)
                    mask |= keyword.Mask;

                return mask | channelBit;
            }
        }

        public override void Accept(IProviderItemVisitor visitor)
        {
            visitor.VisitEvent(this);
        }
    }
}
