namespace InstrManifestCompiler.Support
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    internal static class Value
    {
        public static RefValue<T> Create<T>(T value, SourceLocation location = null)
            where T : class
        {
            if (value == null && location == null)
                return null;
            return new RefValue<T>(value, location);
        }

        public static StructValue<bool> Create(bool value, SourceLocation location = null)
        {
            return new StructValue<bool>(value, location);
        }

        public static StructValue<byte> Create(byte value, SourceLocation location = null)
        {
            return new StructValue<byte>(value, location);
        }

        public static StructValue<ushort> Create(ushort value, SourceLocation location = null)
        {
            return new StructValue<ushort>(value, location);
        }

        public static StructValue<uint> Create(uint value, SourceLocation location = null)
        {
            return new StructValue<uint>(value, location);
        }

        public static StructValue<ulong> Create(ulong value, SourceLocation location = null)
        {
            return new StructValue<ulong>(value, location);
        }

        public static StructValue<sbyte> Create(sbyte value, SourceLocation location = null)
        {
            return new StructValue<sbyte>(value, location);
        }

        public static StructValue<short> Create(short value, SourceLocation location = null)
        {
            return new StructValue<short>(value, location);
        }

        public static StructValue<int> Create(int value, SourceLocation location = null)
        {
            return new StructValue<int>(value, location);
        }

        public static StructValue<long> Create(long value, SourceLocation location = null)
        {
            return new StructValue<long>(value, location);
        }

        public static StructValue<Guid> Create(Guid value, SourceLocation location = null)
        {
            return new StructValue<Guid>(value, location);
        }

        public static StructValue<T> CreateStruct<T>(T value, SourceLocation location = null)
            where T : struct
        {
            return new StructValue<T>(value, location);
        }

        public static NullableValue<T> Create<T>(T? value, SourceLocation location = null)
            where T : struct
        {
            return new NullableValue<T>(value, location);
        }

        public static NullableValue<T> CreateOptional<T>(T? value) where T : struct
        {
            return new NullableValue<T>(value);
        }
    }

    public sealed class RefValue<T>
        : SourceItem
        , IEquatable<RefValue<T>>
        , IEquatable<T>
        , IComparable<RefValue<T>>
        , IComparable<T>
        , IComparable
        , IFormattable
        where T : class
    {
        private T value;

        public RefValue(T value, SourceLocation location = null)
        {
            this.value = value;
            Location = location;
        }

        public T Value
        {
            get { return value; }
            set
            {
                this.value = value;
                Location = null;
            }
        }

        public override int GetHashCode()
        {
            return EqualityComparer<T>.Default.GetHashCode(Value);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            var f = Value as IFormattable;
            if (f != null)
                return f.ToString(format, formatProvider);
            if (Value != null)
                return Value.ToString();
            return string.Empty;
        }

        public override string ToString()
        {
            if (Value == null)
                return string.Empty;
            return Value.ToString();
        }

        public override bool Equals(object obj)
        {
            var sourceValue = obj as RefValue<T>;
            if (sourceValue != null)
                return Equals(sourceValue);
            if (obj is T)
                return Equals((T)obj);
            return false;
        }

        public bool Equals(RefValue<T> other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return EqualityComparer<T>.Default.Equals(value, other.value);
        }

        public bool Equals(T other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return EqualityComparer<T>.Default.Equals(value, other);
        }

        public int CompareTo(object obj)
        {
            var comparable = Value as IComparable;
            return comparable?.CompareTo(obj) ?? Comparer.Default.Compare(this, obj);
        }

        public int CompareTo(RefValue<T> other)
        {
            var comparable = Value as IComparable<T>;
            return comparable?.CompareTo(other.Value) ??
                   Comparer<T>.Default.Compare(Value, other.Value);
        }

        public int CompareTo(T other)
        {
            var comparable = Value as IComparable<T>;
            return comparable?.CompareTo(other) ??
                   Comparer<T>.Default.Compare(this, other);
        }

        public static implicit operator RefValue<T>(T value)
        {
            return new RefValue<T>(value);
        }

        public static implicit operator T(RefValue<T> value)
        {
            return value?.Value;
        }
    }

    public struct StructValue<T>
        : ISourceItem
        , IEquatable<StructValue<T>>
        , IEquatable<T>
        , IComparable<StructValue<T>>
        , IComparable<T>
        , IComparable
        , IFormattable
        where T : struct
    {
        private T value;

        public StructValue(T value, SourceLocation location = null)
            : this()
        {
            this.value = value;
            Location = location;
        }

        public T Value
        {
            get { return value; }
            set
            {
                this.value = value;
                Location = null;
            }
        }

        public SourceLocation Location { get; set; }

        public override int GetHashCode()
        {
            return EqualityComparer<T>.Default.GetHashCode(Value);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            var f = Value as IFormattable;
            if (f == null)
                return string.Empty;
            return f.ToString(format, formatProvider);
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj is StructValue<T>)
                return Equals((StructValue<T>)obj);
            if (obj is T)
                return Equals((T)obj);
            return false;
        }

        public bool Equals(StructValue<T> other)
        {
            return EqualityComparer<T>.Default.Equals(value, other.value);
        }

        public bool Equals(T other)
        {
            return EqualityComparer<T>.Default.Equals(value, other);
        }

        public int CompareTo(object obj)
        {
            var comparable = Value as IComparable;
            return comparable?.CompareTo(obj) ??
                   Comparer.Default.Compare(this, obj);
        }

        public int CompareTo(StructValue<T> other)
        {
            var comparable = Value as IComparable<T>;
            return comparable?.CompareTo(other.Value) ??
                   Comparer<T>.Default.Compare(Value, other.Value);
        }

        public int CompareTo(T other)
        {
            var comparable = Value as IComparable<T>;
            return comparable?.CompareTo(other) ??
                   Comparer<T>.Default.Compare(this, other);
        }

        //public static implicit operator StructValue<T>(T value)
        //{
        //    return new StructValue<T>(value);
        //}

        public static implicit operator T(StructValue<T> value)
        {
            return value.Value;
        }
    }

    public struct NullableValue<T>
        : ISourceItem
        , IEquatable<NullableValue<T>>
        , IEquatable<T>
        , IComparable<NullableValue<T>>
        , IComparable<T>
        , IComparable
        , IFormattable
        where T : struct
    {
        private T? value;

        public NullableValue(T? value, SourceLocation location = null)
            : this()
        {
            this.value = value;
            Location = location;
        }

        public T Value
        {
            get { return value.Value; }
            set
            {
                this.value = value;
                Location = null;
            }
        }

        public bool HasValue
        {
            get { return value.HasValue; }
        }

        public SourceLocation Location { get; set; }

        public StructValue<T> GetValueOrDefault()
        {
            return new StructValue<T>(value.GetValueOrDefault());
        }

        public StructValue<T> GetValueOrDefault(T defaultValue)
        {
            return new StructValue<T>(value.GetValueOrDefault(defaultValue));
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            var f = value as IFormattable;
            if (f == null)
                return string.Empty;
            return f.ToString(format, formatProvider);
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj is NullableValue<T>)
                return Equals((NullableValue<T>)obj);
            return value.Equals(obj);
        }

        public bool Equals(NullableValue<T> other)
        {
            return Nullable.Equals(value, other.value);
        }

        public bool Equals(T other)
        {
            return Nullable.Equals(value, other);
        }

        public int CompareTo(object obj)
        {
            var comparable = Value as IComparable;
            return comparable?.CompareTo(obj) ??
                   Comparer.Default.Compare(this, obj);
        }

        public int CompareTo(NullableValue<T> other)
        {
            var comparable = Value as IComparable<T>;
            return comparable?.CompareTo(other.Value) ??
                   Comparer<T>.Default.Compare(Value, other.Value);
        }

        public int CompareTo(T other)
        {
            var comparable = Value as IComparable<T>;
            return comparable?.CompareTo(other) ??
                   Comparer<T>.Default.Compare(this, other);
        }

        public static implicit operator T?(NullableValue<T> value)
        {
            return value.value;
        }

        public static implicit operator T(NullableValue<T> value)
        {
            return value.Value;
        }

        public static implicit operator NullableValue<T>(T? value)
        {
            return new NullableValue<T>(value);
        }

        public static implicit operator NullableValue<T>(T value)
        {
            return new NullableValue<T>(value);
        }

        public static bool operator ==(NullableValue<T> left, NullableValue<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(NullableValue<T> left, NullableValue<T> right)
        {
            return !left.Equals(right);
        }
    }
}
