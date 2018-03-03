namespace EventManifestFramework.Schema
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using EventManifestFramework.Schema.Base;
    using EventManifestFramework.Support;

    [DebuggerDisplay("{Value} ({Version})")]
    public sealed class Event : ProviderItem
    {
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
        public Channel Channel { get; set; }
        public Level Level { get; set; }
        public Task Task { get; set; }
        public Opcode Opcode { get; set; }
        public List<Keyword> Keywords { get; } = new List<Keyword>();
        public Template Template { get; set; }
        public LocatedNullable<bool> NotLogged { get; set; }
        public LocalizedString Message { get; set; }

        public EnableBit EnableBit { get; internal set; }

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
