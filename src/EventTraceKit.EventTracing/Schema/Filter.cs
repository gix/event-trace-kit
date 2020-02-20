namespace EventTraceKit.EventTracing.Schema
{
    using System;
    using System.Diagnostics;
    using EventTraceKit.EventTracing.Schema.Base;
    using EventTraceKit.EventTracing.Support;

    [DebuggerDisplay("{Name} ({Value})")]
    public sealed class Filter : ProviderItem
    {
        public Filter(
            LocatedRef<QName> name,
            LocatedVal<byte> value)
            : this(name, value, new LocatedVal<byte>(0))
        {
        }

        public Filter(
            LocatedRef<QName> name,
            LocatedVal<byte> value,
            LocatedVal<byte> version)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            Name = name;
            Value = value;
            Version = version;
        }

        public Filter(
            LocatedRef<QName> name,
            LocatedVal<byte> value,
            LocatedVal<byte> version,
            LocatedRef<string> symbol,
            LocalizedString message)
            : this(name, value, version)
        {
            Symbol = symbol;
            Message = message;
        }

        public LocatedRef<QName> Name { get; }
        public LocatedVal<byte> Value { get; }
        public LocatedVal<byte> Version { get; }

        public LocatedRef<string> Symbol { get; }
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
