namespace EventTraceKit.EventTracing.Schema
{
    using System;
    using System.Diagnostics;
    using EventTraceKit.EventTracing.Schema.Base;
    using EventTraceKit.EventTracing.Support;

    [DebuggerDisplay("{Name} ({Value})")]
    public sealed class Task : ProviderItem
    {
        public Task(LocatedRef<QName> name, LocatedVal<ushort> value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value;
        }

        public Task(
            LocatedRef<QName> name, LocatedVal<ushort> value,
            LocatedRef<string> symbol, LocatedNullable<Guid> guid,
            LocalizedString message)
            : this(name, value)
        {
            Symbol = symbol;
            Guid = guid;
            Message = message;
        }

        public LocatedRef<QName> Name { get; }
        public LocatedVal<ushort> Value { get; }

        public LocatedRef<string> Symbol { get; set; }
        public LocatedNullable<Guid> Guid { get; set; }
        public LocalizedString Message { get; set; }

        public bool Imported { get; set; }

        public TaskOpcodeCollection Opcodes { get; } = new TaskOpcodeCollection();

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
