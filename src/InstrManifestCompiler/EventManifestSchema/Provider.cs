namespace InstrManifestCompiler.EventManifestSchema
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
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
            Contract.Requires<ArgumentNullException>(name != null);
            Contract.Requires<ArgumentNullException>(symbol != null);

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
        public RefValue<string> Name { get; private set; }
        public StructValue<Guid> Id { get; private set; }
        public RefValue<string> Symbol { get; private set; }
        public RefValue<string> ResourceFileName { get; set; }
        public RefValue<string> MessageFileName { get; set; }
        public RefValue<string> ParameterFileName { get; set; }
        public LocalizedString Message { get; set; }

        public ChannelCollection Channels { get; private set; }
        public LevelCollection Levels { get; private set; }
        public OpcodeCollection Opcodes { get; private set; }
        public TaskCollection Tasks { get; private set; }
        public KeywordCollection Keywords { get; private set; }
        public MapCollection Maps { get; private set; }
        public PatternMapCollection NamedQueries { get; private set; }
        public FilterCollection Filters { get; private set; }
        public TemplateCollection Templates { get; private set; }
        public EventCollection Events { get; private set; }

        public IReadOnlyList<EnableBit> EnableBits
        {
            get { return enableBits; }
        }

        public void PopulateEnableBits()
        {
            var lookup = new Dictionary<Tuple<int, ulong>, EnableBit>();
            int bitPosition = 0;
            foreach (var evt in Events) {
                int level = evt.LevelValue;
                ulong mask = evt.KeywordMask;
                var key = Tuple.Create(level, mask);

                EnableBit bit;
                if (!lookup.TryGetValue(key, out bit)) {
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
