namespace EventManifestFramework.Support
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>Nullable value with a <see cref="SourceLocation"/>.</summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    [DebuggerDisplay("{" + nameof(value) + "}")]
    public struct LocatedNullable<T>
        : ISourceItem
        , IEquatable<LocatedNullable<T>>
        , IEquatable<T>
        , IComparable<LocatedNullable<T>>
        , IComparable<T>
        , IComparable
        , IFormattable
        where T : struct
    {
        private readonly T? value;

        public LocatedNullable(T? value, SourceLocation location = null)
            : this()
        {
            this.value = value;
            Location = location;
        }

        public T Value => value.Value;

        public bool HasValue => value.HasValue;

        public SourceLocation Location { get; set; }

        public LocatedVal<T> GetValueOrDefault()
        {
            return new LocatedVal<T>(value.GetValueOrDefault());
        }

        public LocatedVal<T> GetValueOrDefault(T defaultValue)
        {
            return new LocatedVal<T>(value.GetValueOrDefault(defaultValue));
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (value is IFormattable f)
                return f.ToString(format, formatProvider);
            return value.ToString();
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj is LocatedNullable<T> nv)
                return Equals(nv);
            return value.Equals(obj);
        }

        public bool Equals(LocatedNullable<T> other)
        {
            return Nullable.Equals(value, other.value);
        }

        public bool Equals(T other)
        {
            return Nullable.Equals(value, other);
        }

        public int CompareTo(object obj)
        {
            switch (obj) {
                case LocatedNullable<T> other:
                    return (Value as IComparable)?.CompareTo(other.Value) ??
                           Comparer<T?>.Default.Compare(this, other.Value);
                default:
                    return (Value as IComparable)?.CompareTo(obj) ??
                           Comparer<object>.Default.Compare(this, obj);
            }
        }

        public int CompareTo(LocatedNullable<T> other)
        {
            var comparable = Value as IComparable<T>;
            return comparable?.CompareTo(other.Value) ??
                   Comparer<T>.Default.Compare(Value, other.Value);
        }

        public int CompareTo(T other)
        {
            var comparable = Value as IComparable<T>;
            return comparable?.CompareTo(other) ??
                   Comparer<T?>.Default.Compare(this, other);
        }

        public static implicit operator T?(LocatedNullable<T> value)
        {
            return value.value;
        }

        public static implicit operator LocatedNullable<T>(T? value)
        {
            return new LocatedNullable<T>(value);
        }

        public static implicit operator LocatedNullable<T>(T value)
        {
            return new LocatedNullable<T>(value);
        }

        public static bool operator ==(LocatedNullable<T> left, LocatedNullable<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LocatedNullable<T> left, LocatedNullable<T> right)
        {
            return !left.Equals(right);
        }
    }
}
