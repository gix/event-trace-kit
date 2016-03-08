namespace InstrManifestCompiler.EventManifestSchema
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using InstrManifestCompiler.Support;

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
        public Channel(RefValue<string> name, StructValue<ChannelType> type)
        {
            Contract.Requires<ArgumentNullException>(name != null);
            Name = name;
            Type = type;
        }

        public Channel(
            RefValue<string> name,
            StructValue<ChannelType> type,
            RefValue<string> id,
            RefValue<string> symbol,
            NullableValue<byte> value,
            RefValue<string> access,
            NullableValue<ChannelIsolationType> isolation,
            NullableValue<bool> enabled,
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

        public RefValue<string> Name { get; private set; }
        public StructValue<ChannelType> Type { get; private set; }

        public RefValue<string> Id { get; set; }
        public RefValue<string> Symbol { get; set; }
        public NullableValue<byte> Value { get; set; }
        public RefValue<string> Access { get; set; }
        public NullableValue<ChannelIsolationType> Isolation { get; set; }
        public NullableValue<bool> Enabled { get; set; }
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
