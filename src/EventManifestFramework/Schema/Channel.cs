namespace EventManifestFramework.Schema
{
    using System;
    using System.Diagnostics;
    using EventManifestFramework.Support;

    public enum ChannelType
    {
        Admin,
        Operational,
        Analytic,
        Debug,
    }

    public enum ChannelIsolationType
    {
        Application,
        System,
        Custom,
    }

    [DebuggerDisplay("{Name} ({Type})")]
    public sealed class Channel : ProviderItem
    {
        public Channel(LocatedRef<string> name, LocatedVal<ChannelType> type)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            Name = name;
            Type = type;
        }

        public Channel(
            LocatedRef<string> name,
            LocatedVal<ChannelType> type,
            LocatedRef<string> id,
            LocatedRef<string> symbol,
            LocatedNullable<byte> value,
            LocatedRef<string> access,
            LocatedNullable<ChannelIsolationType> isolation,
            LocatedNullable<bool> enabled,
            LocalizedString message)
            : this(name, type)
        {
            Id = id;
            Symbol = symbol;
            Value = value;
            Access = access;
            Isolation = isolation;
            Enabled = enabled;
            Message = message;
        }

        public int Index { get; set; }

        public LocatedRef<string> Name { get; }
        public LocatedVal<ChannelType> Type { get; }

        public LocatedRef<string> Id { get; set; }
        public LocatedRef<string> Symbol { get; set; }
        public LocatedNullable<byte> Value { get; set; }
        public LocatedRef<string> Access { get; set; }
        public LocatedNullable<ChannelIsolationType> Isolation { get; set; }
        public LocatedNullable<bool> Enabled { get; set; }
        public LocalizedString Message { get; set; }

        public bool Imported { get; set; }

        public override string ToString()
        {
            return Name;
        }

        public override void Accept(IProviderItemVisitor visitor)
        {
            visitor.VisitChannel(this);
        }
    }
}
