namespace InstrManifestCompiler.EventManifestSchema
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using InstrManifestCompiler.EventManifestSchema.Base;
    using InstrManifestCompiler.Support;

    [DebuggerDisplay("{Name} ({Value})")]
    public sealed class Level : ProviderItem
    {
        public Level(RefValue<QName> name, StructValue<byte> value)
        {
            Contract.Requires<ArgumentNullException>(name != null);
            Name = name;
            Value = value;
        }

        public Level(
            RefValue<QName> name, StructValue<byte> value,
            RefValue<string> symbol, LocalizedString message)
            : this(name, value)
        {
            Symbol = symbol;
            Message = message;
        }

        public RefValue<QName> Name { get; private set; }
        public StructValue<byte> Value { get; private set; }
        public RefValue<string> Symbol { get; set; }
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
