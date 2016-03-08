namespace InstrManifestCompiler.EventManifestSchema
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using InstrManifestCompiler.Support;

    public sealed class ValueMapItem : MapItem
    {
        private readonly IMap map;

        public ValueMapItem(ValueMap map, StructValue<uint> value, LocalizedString message)
            : base(value, message)
        {
            Contract.Requires<ArgumentNullException>(map != null);
            this.map = map;
        }

        public ValueMapItem(
            ValueMap map, StructValue<uint> value, RefValue<string> symbol,
            LocalizedString message)
            : base(value, message, symbol)
        {
            Contract.Requires<ArgumentNullException>(map != null);
            this.map = map;
        }

        public override IMap Map
        {
            get { return map; }
        }

        public override MapKind Kind
        {
            get { return MapKind.ValueMap; }
        }
    }

    [DebuggerDisplay("{Name}")]
    public sealed class ValueMap : Map
    {
        public ValueMap(RefValue<string> name)
            : base(name)
        {
        }

        public ValueMap(RefValue<string> name, RefValue<string> symbol)
            : base(name, symbol)
        {
        }

        public override MapKind Kind
        {
            get { return MapKind.ValueMap; }
        }

        public override void Accept(IProviderItemVisitor visitor)
        {
            visitor.VisitValueMap(this);
        }
    }
}
