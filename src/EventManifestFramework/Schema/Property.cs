namespace EventManifestFramework.Schema
{
    using System;
    using EventManifestFramework.Schema.Base;
    using EventManifestFramework.Support;

    public enum PropertyKind
    {
        Data,
        Struct,
    }

    [Flags]
    public enum PropertyFlags : uint
    {
        Struct = 0x1,
        FixedLength = 0x2,
        VarLength = 0x4,
        FixedCount = 0x8,
        VarCount = 0x10,
    }

    public abstract class Property : SourceItem
    {
        protected Property(LocatedRef<string> name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public abstract PropertyKind Kind { get; }

        public abstract uint BinXmlType { get; }

        public virtual PropertyFlags GetFlags()
        {
            PropertyFlags flags = 0;
            if (Length.IsFixed)
                flags |= PropertyFlags.FixedLength;
            else if (Length.IsVariable)
                flags |= PropertyFlags.VarLength;
            if (Count.IsFixed)
                flags |= PropertyFlags.FixedCount;
            else if (Count.IsVariable)
                flags |= PropertyFlags.VarCount;

            return flags;
        }

        public LocatedRef<string> Name { get; }
        public IPropertyNumber Count { get; } = new Count();
        public IPropertyNumber Length { get; } = new Length();

        public int Index { get; set; }
    }
}
