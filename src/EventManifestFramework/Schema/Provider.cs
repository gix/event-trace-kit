namespace EventManifestFramework.Schema
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using EventManifestFramework.Schema.Base;
    using EventManifestFramework.Support;

    public sealed class Provider : SourceItem
    {
        private readonly List<EnableBit> enableBits = new List<EnableBit>();
        private uint channelValue = 17;

        public Provider(
            LocatedRef<string> name,
            LocatedVal<Guid> id,
            LocatedRef<string> symbol)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol));

            Name = name;
            Id = id;
            Symbol = symbol;
        }

        public Provider(
            LocatedRef<string> name,
            LocatedVal<Guid> id,
            LocatedRef<string> symbol,
            LocalizedString message)
            : this(name, id, symbol)
        {
            Message = message;
        }

        public EventManifest Manifest { get; set; }
        public int Index { get; set; }
        public LocatedRef<string> Name { get; }
        public LocatedVal<Guid> Id { get; }
        public LocatedRef<string> Symbol { get; }
        public LocatedRef<string> ResourceFileName { get; set; }
        public LocatedRef<string> MessageFileName { get; set; }
        public LocatedRef<string> ParameterFileName { get; set; }
        public LocalizedString Message { get; set; }

        public ChannelCollection Channels { get; } = new ChannelCollection();
        public LevelCollection Levels { get; } = new LevelCollection();
        public OpcodeCollection Opcodes { get; } = new OpcodeCollection();
        public TaskCollection Tasks { get; } = new TaskCollection();
        public KeywordCollection Keywords { get; } = new KeywordCollection();
        public MapCollection Maps { get; } = new MapCollection();
        public PatternMapCollection NamedQueries { get; } = new PatternMapCollection();
        public FilterCollection Filters { get; } = new FilterCollection();
        public TemplateCollection Templates { get; } = new TemplateCollection();
        public EventCollection Events { get; } = new EventCollection();

        public IReadOnlyList<EnableBit> EnableBits => enableBits;

        public void PopulateEnableBits()
        {
            var lookup = new Dictionary<Tuple<int, ulong>, EnableBit>();
            int bitPosition = 0;
            foreach (var evt in Events) {
                int level = evt.LevelValue;
                ulong mask = evt.KeywordMask;
                var key = Tuple.Create(level, mask);

                if (!lookup.TryGetValue(key, out var bit)) {
                    bit = new EnableBit(bitPosition++, level, mask);
                    lookup.Add(key, bit);
                    enableBits.Add(bit);
                }

                evt.EnableBit = bit;
            }
        }

        public uint CreateChannelValue()
        {
            while (Channels.Any(c => c.Value == channelValue))
                ++channelValue;

            return channelValue;
        }
    }
}
