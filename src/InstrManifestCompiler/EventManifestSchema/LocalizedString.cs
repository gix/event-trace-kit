namespace InstrManifestCompiler.EventManifestSchema
{
    using System;
    using System.Diagnostics;
    using InstrManifestCompiler.Support;

    [DebuggerDisplay("{RefId}(0x{Id,h}) = '{Value}'")]
    public sealed class LocalizedString : SourceItem
    {
        public const uint UnusedId = uint.MaxValue;

        public LocalizedString(RefValue<string> name, RefValue<string> value)
            : this(name, value, UnusedId)
        {
        }

        public LocalizedString(RefValue<string> name, string value, uint id)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            Name = name;
            Value = value;
            Id = id;
        }

        public RefValue<string> Name { get; private set; }
        public RefValue<string> Value { get; private set; }
        public uint Id { get; set; }
        public RefValue<string> Symbol { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
