namespace InstrManifestCompiler.EventManifestSchema
{
    using System;
    using System.Diagnostics;
    using InstrManifestCompiler.Support;

    public sealed class PatternMapItem : SourceItem
    {
        public PatternMapItem(PatternMap map, RefValue<string> name, RefValue<string> value)
        {
            Map = map ?? throw new ArgumentNullException(nameof(map));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value;
        }

        public RefValue<string> Name { get; }
        public RefValue<string> Value { get; }
        public PatternMap Map { get; }
    }

    [DebuggerDisplay("{Name} ({Format})")]
    public sealed class PatternMap : ProviderItem
    {
        public PatternMap(RefValue<string> name, RefValue<string> format)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Format = format ?? throw new ArgumentNullException(nameof(format));
        }

        public PatternMap(
            RefValue<string> name, RefValue<string> format, RefValue<string> symbol)
            : this(name, format)
        {
            Symbol = symbol;
        }

        public RefValue<string> Name { get; }
        public RefValue<string> Format { get; }
        public RefValue<string> Symbol { get; set; }
        public PatternMapItemCollection Items { get; } =
            new PatternMapItemCollection();

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
