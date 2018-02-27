namespace InstrManifestCompiler.EventManifestSchema
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using InstrManifestCompiler.Support;

    public sealed class PatternMapItem : SourceItem
    {
        public PatternMapItem(PatternMap map, RefValue<string> name, RefValue<string> value)
        {
            Contract.Requires<ArgumentNullException>(map != null);
            Contract.Requires<ArgumentNullException>(name != null);
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
            Contract.Requires<ArgumentNullException>(name != null);
            Contract.Requires<ArgumentNullException>(format != null);
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
