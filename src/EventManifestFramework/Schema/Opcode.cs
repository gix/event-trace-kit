namespace EventManifestFramework.Schema
{
    using System;
    using System.Diagnostics;
    using EventManifestFramework.Schema.Base;
    using EventManifestFramework.Support;

    [DebuggerDisplay("{Name} ({Value})")]
    public sealed class Opcode : ProviderItem
    {
        private Task task;

        public Opcode(LocatedRef<QName> name, LocatedVal<byte> value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value;
        }

        public Opcode(
            LocatedRef<QName> name, LocatedVal<byte> value,
            LocatedRef<string> symbol, LocalizedString message)
            : this(name, value)
        {
            Symbol = symbol;
            Message = message;
        }

        public LocatedRef<QName> Name { get; }
        public LocatedVal<byte> Value { get; }
        public LocatedRef<string> Symbol { get; set; }
        public LocalizedString Message { get; set; }

        public bool Imported { get; set; }

        public Task Task
        {
            get => task;
            set
            {
                task = value;
                Provider = value?.Provider;
            }
        }

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
