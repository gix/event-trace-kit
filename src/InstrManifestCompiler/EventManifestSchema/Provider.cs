namespace InstrManifestCompiler.EventManifestSchema
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using InstrManifestCompiler.EventManifestSchema.Base;
    using InstrManifestCompiler.Support;

    public sealed class Provider : SourceItem
    {
        private readonly List<EnableBit> enableBits = new List<EnableBit>();
        private uint channelValue = 17;

        public Provider(
            RefValue<string> name,
            StructValue<Guid> id,
            RefValue<string> symbol)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol));

            Name = name;
            Id = id;
            Symbol = symbol;

            Channels = new ChannelCollection();
            Levels = new LevelCollection();
            Opcodes = new OpcodeCollection();
            Tasks = new TaskCollection();
            Keywords = new KeywordCollection();
            Maps = new MapCollection();
            NamedQueries = new PatternMapCollection();
            Filters = new FilterCollection();
            Templates = new TemplateCollection();
            Events = new EventCollection();
        }

        public Provider(
            RefValue<string> name,
            StructValue<Guid> id,
            RefValue<string> symbol,
            LocalizedString message)
            : this(name, id, symbol)
        {
            Message = message;
        }

        public EventManifest Manifest { get; set; }
        public int Index { get; set; }
        public RefValue<string> Name { get; }
        public StructValue<Guid> Id { get; }
        public RefValue<string> Symbol { get; }
        public RefValue<string> ResourceFileName { get; set; }
        public RefValue<string> MessageFileName { get; set; }
        public RefValue<string> ParameterFileName { get; set; }
        public LocalizedString Message { get; set; }

        public ChannelCollection Channels { get; }
        public LevelCollection Levels { get; }
        public OpcodeCollection Opcodes { get; }
        public TaskCollection Tasks { get; }
        public KeywordCollection Keywords { get; }
        public MapCollection Maps { get; }
        public PatternMapCollection NamedQueries { get; }
        public FilterCollection Filters { get; }
        public TemplateCollection Templates { get; }
        public EventCollection Events { get; }

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
