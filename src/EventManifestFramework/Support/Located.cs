namespace EventManifestFramework.Support
{
    using System;

    public static class Located
    {
        public static LocatedRef<T> Create<T>(T value, SourceLocation location = null)
            where T : class
        {
            if (value == null && location == null)
                return null;
            return new LocatedRef<T>(value, location);
        }

        public static LocatedVal<bool> Create(bool value, SourceLocation location = null)
        {
            return new LocatedVal<bool>(value, location);
        }

        public static LocatedVal<byte> Create(byte value, SourceLocation location = null)
        {
            return new LocatedVal<byte>(value, location);
        }

        public static LocatedVal<ushort> Create(ushort value, SourceLocation location = null)
        {
            return new LocatedVal<ushort>(value, location);
        }

        public static LocatedVal<uint> Create(uint value, SourceLocation location = null)
        {
            return new LocatedVal<uint>(value, location);
        }

        public static LocatedVal<ulong> Create(ulong value, SourceLocation location = null)
        {
            return new LocatedVal<ulong>(value, location);
        }

        public static LocatedVal<sbyte> Create(sbyte value, SourceLocation location = null)
        {
            return new LocatedVal<sbyte>(value, location);
        }

        public static LocatedVal<short> Create(short value, SourceLocation location = null)
        {
            return new LocatedVal<short>(value, location);
        }

        public static LocatedVal<int> Create(int value, SourceLocation location = null)
        {
            return new LocatedVal<int>(value, location);
        }

        public static LocatedVal<long> Create(long value, SourceLocation location = null)
        {
            return new LocatedVal<long>(value, location);
        }

        public static LocatedVal<Guid> Create(Guid value, SourceLocation location = null)
        {
            return new LocatedVal<Guid>(value, location);
        }

        public static LocatedVal<T> CreateStruct<T>(T value, SourceLocation location = null)
            where T : struct
        {
            return new LocatedVal<T>(value, location);
        }

        public static LocatedNullable<T> Create<T>(T? value, SourceLocation location = null)
            where T : struct
        {
            return new LocatedNullable<T>(value, location);
        }
    }
}
