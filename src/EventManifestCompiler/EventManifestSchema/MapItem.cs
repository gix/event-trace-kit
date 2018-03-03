namespace InstrManifestCompiler.EventManifestSchema
{
    using System;
    using System.Diagnostics;
    using InstrManifestCompiler.Support;

    public interface IMapItem
    {
        IMap Map { get; }
        MapKind Kind { get; }
        RefValue<string> Symbol { get; }
        StructValue<uint> Value { get; }
        LocalizedString Message { get; }
    }

    [DebuggerDisplay("{Symbol}: {Value}")]
    public abstract class MapItem : SourceItem, IMapItem
    {
        protected MapItem(StructValue<uint> value, LocalizedString message)
        {
            Value = value;
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        protected MapItem(StructValue<uint> value, LocalizedString message,
                          RefValue<string> symbol)
            : this(value, message)
        {
            Symbol = symbol;
        }

        public abstract IMap Map { get; }
        public abstract MapKind Kind { get; }

        public StructValue<uint> Value { get; }
        public LocalizedString Message { get; }
        public RefValue<string> Symbol { get; }
    }
}
