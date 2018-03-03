namespace EventManifestFramework.Schema
{
    using System;
    using System.Diagnostics;
    using EventManifestFramework.Schema.Base;
    using EventManifestFramework.Support;

    [DebuggerDisplay("{Name} ({Value})")]
    public sealed class Level : ProviderItem
    {
        public Level(LocatedRef<QName> name, LocatedVal<byte> value)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            Name = name;
            Value = value;
        }

        public Level(
            LocatedRef<QName> name, LocatedVal<byte> value,
            LocatedRef<string> symbol, LocalizedString message)
            : this(name, value)
        {
            Symbol = symbol;
            Message = message;
        }

        public LocatedRef<QName> Name { get; }
        public LocatedVal<byte> Value { get; }
        public LocatedRef<string> Symbol { get; }
        public LocalizedString Message { get; set; }

        public bool Imported { get; set; }

        public override string ToString()
        {
            return Name.ToString();
        }

        public override void Accept(IProviderItemVisitor visitor)
        {
            visitor.VisitLevel(this);
        }
    }
}
