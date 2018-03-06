namespace EventManifestFramework.Support
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>Value with a <see cref="SourceLocation"/>.</summary>
    /// <typeparam name="T">The reference type of the value.</typeparam>
    /// <see cref="Located.Create{T}(T,SourceLocation)"/>
    [DebuggerDisplay(nameof(Value))]
    public sealed class LocatedRef<T>
        : ISourceItem
        , IEquatable<LocatedRef<T>>
        , IEquatable<T>
        , IComparable<LocatedRef<T>>
        , IComparable<T>
        , IComparable
        , IFormattable
        where T : class
    {
        public LocatedRef(T value, SourceLocation location = null)
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
            return Value?.ToString() ?? string.Empty;
        }

        public override string ToString()
        {
            return Value?.ToString() ?? string.Empty;
        }

        public override bool Equals(object obj)
        {
            switch (obj) {
                case LocatedRef<T> other:
                    return Equals(other);
                default:
                    if (Value == null)
                        return obj == null;
                    return Value.Equals(obj);
            }
        }

        public bool Equals(LocatedRef<T> other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return EqualityComparer<T>.Default.Equals(Value, other.Value);
        }

        public bool Equals(T other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return EqualityComparer<T>.Default.Equals(Value, other);
        }

        public int CompareTo(object obj)
        {
            switch (obj) {
                case LocatedRef<T> other:
                    return (Value as IComparable)?.CompareTo(other.Value) ??
                           Comparer<T>.Default.Compare(this, other.Value);
                default:
                    return (Value as IComparable)?.CompareTo(obj) ??
                           Comparer<object>.Default.Compare(this, obj);
            }
        }

        public int CompareTo(LocatedRef<T> other)
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

        public static implicit operator LocatedRef<T>(T value)
        {
            return new LocatedRef<T>(value);
        }

        public static implicit operator T(LocatedRef<T> value)
        {
            return value?.Value;
        }
    }
}
