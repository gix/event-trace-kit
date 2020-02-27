namespace EventTraceKit.EventTracing.Schema
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using EventTraceKit.EventTracing.Schema.Base;
    using EventTraceKit.EventTracing.Support;

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

        /// <summary>
        ///   Gets or sets the non-localized event name.
        /// </summary>
        /// <remarks>
        ///   During event processing on Windows 10 1709 (16299) or later, this
        ///   value can be retrieved from the TRACE_EVENT_INFO EventNameOffset
        ///   field.
        /// </remarks>
        public LocatedRef<string> Name { get; set; }

        /// <summary>
        ///   Gets the non-localized event attributes, as a
        ///   semicolon-delimited list of <c>name=value</c> pairs.
        /// </summary>
        /// <remarks>
        ///   During event processing on Windows 10 1709 (16299) or later, this
        ///   value can be retrieved from the TRACE_EVENT_INFO
        ///   EventAttributesOffset field.
        /// </remarks>
        public List<EventAttribute> Attributes { get; } = new List<EventAttribute>();

        public Channel Channel
        {
            get => channel;
            set
            {
                channel = value;

                if (channel == null || channel.IsTraceClassic() || channel.IsTraceLogging() || channel.IsGlobal())
                    channelBit = 0;
                else
                    channelBit |= (ulong)1 << (63 - channel.Index);
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
