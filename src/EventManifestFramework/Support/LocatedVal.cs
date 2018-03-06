namespace EventManifestFramework.Support
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>Value with a <see cref="SourceLocation"/>.</summary>
    /// <typeparam name="T">The value type of the value.</typeparam>
    /// <see cref="Located.CreateStruct{T}(T,SourceLocation)"/>
    [DebuggerDisplay(nameof(Value))]
    public struct LocatedVal<T>
        : ISourceItem
        , IEquatable<LocatedVal<T>>
        , IEquatable<T>
        , IComparable<LocatedVal<T>>
        , IComparable<T>
        , IComparable
        , IFormattable
        where T : struct
    {
        public LocatedVal(T value, SourceLocation location = null)
            : this()
        {
            Value = value;
            Location = location;
        }

        public T Value { get; }

        public SourceLocation Location { get; set; }

        public override int GetHashCode()
        {
            return EqualityComparer<T>.Default.GetHashCode(Value);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (Value is IFormattable formattable)
                return formattable.ToString(format, formatProvider);
            return Value.ToString();
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public override bool Equals(object obj)
        {
            switch (obj) {
                case LocatedVal<T> other:
                    return Equals(other);
                default:
                    return Value.Equals(obj);
            }
        }

        public bool Equals(LocatedVal<T> other)
        {
            return EqualityComparer<T>.Default.Equals(Value, other.Value);
        }

        public bool Equals(T other)
        {
            return EqualityComparer<T>.Default.Equals(Value, other);
        }

        public int CompareTo(object obj)
        {
            switch (obj) {
                case LocatedVal<T> other:
                    return (Value as IComparable)?.CompareTo(other.Value) ??
                           Comparer<T>.Default.Compare(this, other.Value);
                default:
                    return (Value as IComparable)?.CompareTo(obj) ??
                           Comparer<object>.Default.Compare(this, obj);
            }
        }

        public int CompareTo(LocatedVal<T> other)
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

        public static implicit operator LocatedVal<T>(T value)
        {
            return new LocatedVal<T>(value);
        }

        public static implicit operator T(LocatedVal<T> value)
        {
            return value.Value;
        }

        public static implicit operator LocatedNullable<T>(LocatedVal<T> value)
        {
            return new LocatedNullable<T>(value.Value, value.Location);
        }
    }
}
