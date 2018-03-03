namespace InstrManifestCompiler.EventManifestSchema
{
    using System;
    using System.Diagnostics;
    using InstrManifestCompiler.EventManifestSchema.Base;
    using InstrManifestCompiler.Support;

    [DebuggerDisplay("{Name} ({Value})")]
    public sealed class Opcode : ProviderItem
    {
        public Opcode(RefValue<QName> name, StructValue<byte> value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value;
        }

        public Opcode(
            RefValue<QName> name, StructValue<byte> value,
            RefValue<string> symbol, LocalizedString message)
            : this(name, value)
        {
            Symbol = symbol;
            Message = message;
        }

        public RefValue<QName> Name { get; }
        public StructValue<byte> Value { get; }
        public RefValue<string> Symbol { get; set; }
        public LocalizedString Message { get; set; }

        public bool Imported { get; set; }

        public override string ToString()
        {
            return Name.ToString();
        }

        public override void Accept(IProviderItemVisitor visitor)
        {
            visitor.VisitOpcode(this);
        }
    }
}
