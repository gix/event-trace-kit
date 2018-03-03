namespace EventManifestFramework.Schema
{
    using System;
    using System.Diagnostics;
    using EventManifestFramework.Schema.Base;
    using EventManifestFramework.Support;

    [DebuggerDisplay("{" + nameof(Name) + "} (0x{Mask,h})")]
    public sealed class Keyword : ProviderItem
    {
        public Keyword(LocatedRef<QName> name, LocatedVal<ulong> mask)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            Name = name;
            Mask = mask;
        }

        public Keyword(LocatedRef<QName> name, LocatedVal<ulong> mask,
                       LocatedRef<string> symbol, LocalizedString message)
            : this(name, mask)
        {
            Symbol = symbol;
            Message = message;
        }

        public LocatedRef<QName> Name { get; }
        public LocatedVal<ulong> Mask { get; }

        public LocatedRef<string> Symbol { get; }
        public LocalizedString Message { get; set; }

        public bool Imported { get; set; }

        public override string ToString()
        {
            return Name.ToString();
        }

        public override void Accept(IProviderItemVisitor visitor)
        {
            visitor.VisitKeyword(this);
        }
    }
}
