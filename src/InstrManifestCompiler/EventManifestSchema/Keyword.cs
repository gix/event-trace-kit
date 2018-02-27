namespace InstrManifestCompiler.EventManifestSchema
{
    using System;
    using System.Diagnostics;
    using InstrManifestCompiler.EventManifestSchema.Base;
    using InstrManifestCompiler.Support;

    [DebuggerDisplay("{" + nameof(Name) + "} (0x{Mask,h})")]
    public sealed class Keyword : ProviderItem
    {
        public Keyword(RefValue<QName> name, StructValue<ulong> mask)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            Name = name;
            Mask = mask;
        }

        public Keyword(RefValue<QName> name, StructValue<ulong> mask,
                       RefValue<string> symbol, LocalizedString message)
            : this(name, mask)
        {
            Symbol = symbol;
            Message = message;
        }

        public RefValue<QName> Name { get; }
        public StructValue<ulong> Mask { get; }

        public RefValue<string> Symbol { get; }
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
