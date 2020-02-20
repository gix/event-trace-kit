namespace EventTraceKit.EventTracing.Schema
{
    using System;
    using System.Diagnostics;
    using EventTraceKit.EventTracing.Support;

    [DebuggerDisplay("{Name} ({Format})")]
    public sealed class PatternMap : ProviderItem
    {
        public PatternMap(LocatedRef<string> name, LocatedRef<string> format)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Format = format ?? throw new ArgumentNullException(nameof(format));
        }

        public PatternMap(
            LocatedRef<string> name, LocatedRef<string> format, LocatedRef<string> symbol)
            : this(name, format)
        {
            Symbol = symbol;
        }

        public LocatedRef<string> Name { get; }
        public LocatedRef<string> Format { get; }
        public LocatedRef<string> Symbol { get; set; }
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
