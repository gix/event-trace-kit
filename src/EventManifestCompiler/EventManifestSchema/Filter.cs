namespace InstrManifestCompiler.EventManifestSchema
{
    using System;
    using System.Diagnostics;
    using InstrManifestCompiler.EventManifestSchema.Base;
    using InstrManifestCompiler.Support;

    [DebuggerDisplay("{Name} ({Value})")]
    public sealed class Filter : ProviderItem
    {
        public Filter(
            RefValue<QName> name,
            StructValue<byte> value)
            : this(name, value, new StructValue<byte>(0))
        {
        }

        public Filter(
            RefValue<QName> name,
            StructValue<byte> value,
            StructValue<byte> version)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            Name = name;
            Value = value;
            Version = version;
        }

        public Filter(
            RefValue<QName> name,
            StructValue<byte> value,
            StructValue<byte> version,
            RefValue<string> symbol,
            LocalizedString message)
            : this(name, value, version)
        {
            Symbol = symbol;
            Message = message;
        }

        public RefValue<QName> Name { get; }
        public StructValue<byte> Value { get; }
        public StructValue<byte> Version { get; }

        public RefValue<string> Symbol { get; }
        public Template Template { get; set; }
        public LocalizedString Message { get; set; }

        public override string ToString()
        {
            return Name.ToString();
        }

        public override void Accept(IProviderItemVisitor visitor)
        {
            visitor.VisitFilter(this);
        }
    }
}