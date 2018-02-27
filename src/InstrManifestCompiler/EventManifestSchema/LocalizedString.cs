namespace InstrManifestCompiler.EventManifestSchema
{
    using System;
    using System.Diagnostics;
    using InstrManifestCompiler.Support;

    [DebuggerDisplay("{Name}(0x{Id,h}) = '{Value}'")]
    public sealed class LocalizedString : SourceItem
    {
        public const uint UnusedId = uint.MaxValue;

        public LocalizedString(RefValue<string> name, RefValue<string> value)
            : this(name, value, UnusedId)
        {
        }

        public LocalizedString(RefValue<string> name, string value, uint id)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Id = id;
        }

        public RefValue<string> Name { get; }
        public RefValue<string> Value { get; }
        public uint Id { get; set; }
        public RefValue<string> Symbol { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
