namespace InstrManifestCompiler.EventManifestSchema
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using InstrManifestCompiler.EventManifestSchema.Base;
    using InstrManifestCompiler.Support;

    [DebuggerDisplay("{Name} ({Value})")]
    public sealed class Task : ProviderItem
    {
        public Task(RefValue<QName> name, StructValue<ushort> value)
        {
            Contract.Requires<ArgumentNullException>(name != null);
            Name = name;
            Value = value;
        }

        public Task(
            RefValue<QName> name, StructValue<ushort> value,
            RefValue<string> symbol, NullableValue<Guid> guid,
            LocalizedString message)
            : this(name, value)
        {
            Symbol = symbol;
            Guid = guid;
            Message = message;
        }

        public RefValue<QName> Name { get; private set; }
        public StructValue<ushort> Value { get; private set; }

        public RefValue<string> Symbol { get; set; }
        public NullableValue<Guid> Guid { get; set; }
        public LocalizedString Message { get; set; }

        public bool Imported { get; set; }

        public override string ToString()
        {
            return Name.ToString();
        }

        public override void Accept(IProviderItemVisitor visitor)
        {
            visitor.VisitTask(this);
        }
    }
}
