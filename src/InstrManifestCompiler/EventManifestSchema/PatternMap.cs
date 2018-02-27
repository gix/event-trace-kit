namespace InstrManifestCompiler.EventManifestSchema
{
    using System;
    using System.Diagnostics;
    using InstrManifestCompiler.Support;

    public sealed class PatternMapItem : SourceItem
    {
        public PatternMapItem(PatternMap map, RefValue<string> name, RefValue<string> value)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            Map = map;
            Name = name;
            Value = value;
        }

        public RefValue<string> Name { get; private set; }
        public RefValue<string> Value { get; private set; }
        public PatternMap Map { get; private set; }
    }

    [DebuggerDisplay("{Name} ({Value})")]
    public sealed class PatternMap : ProviderItem
    {
        public PatternMap(RefValue<string> name, RefValue<string> format)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (format == null)
                throw new ArgumentNullException(nameof(format));
            Name = name;
            Format = format;
            Items = new PatternMapItemCollection();
        }

        public PatternMap(
            RefValue<string> name, RefValue<string> format, RefValue<string> symbol)
            : this(name, format)
        {
            Symbol = symbol;
        }

        public RefValue<string> Name { get; private set; }
        public RefValue<string> Format { get; private set; }
        public RefValue<string> Symbol { get; set; }
        public PatternMapItemCollection Items { get; private set; }

        public override string ToString()
        {
            return Name;
        }

        public override void Accept(IProviderItemVisitor visitor)
        {
            visitor.VisitPatternMap(this);
        }
    }
}
