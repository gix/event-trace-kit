namespace EventTraceKit.VsExtension
{
    using System;
    using System.Globalization;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    public struct Keyword
        : IEquatable<Keyword>
        , IComparable<Keyword>
        , IFormattable
    {
        public Keyword(ulong keywordValue)
        {
            KeywordValue = keywordValue;
        }

        public ulong KeywordValue { get; }

        public static Keyword Zero => new Keyword();
        public static Keyword MinValue => new Keyword(0L);
        public static Keyword MaxValue => new Keyword(ulong.MaxValue);

        public static bool operator ==(Keyword lhs, Keyword rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(Keyword lhs, Keyword rhs)
        {
            return !lhs.Equals(rhs);
        }

        public static bool operator <(Keyword lhs, Keyword rhs)
        {
            return lhs.CompareTo(rhs) < 0;
        }

        public static bool operator >(Keyword lhs, Keyword rhs)
        {
            return lhs.CompareTo(rhs) > 0;
        }

        public static bool operator <=(Keyword lhs, Keyword rhs)
        {
            return lhs.CompareTo(rhs) <= 0;
        }

        public static bool operator >=(Keyword lhs, Keyword rhs)
        {
            return lhs.CompareTo(rhs) >= 0;
        }

        public static Keyword operator &(Keyword lhs, Keyword rhs)
        {
            return new Keyword(lhs.KeywordValue & rhs.KeywordValue);
        }

        public static implicit operator Keyword(ulong keywordValue)
        {
            return new Keyword(keywordValue);
        }

        public bool Equals(Keyword other)
        {
            return KeywordValue == other.KeywordValue;
        }

        public int CompareTo(Keyword other)
        {
            return KeywordValue.CompareTo(other.KeywordValue);
        }

        public override bool Equals(object other)
        {
            return other is Keyword keyword && Equals(keyword);
        }

        public override int GetHashCode()
        {
            return KeywordValue.GetHashCode();
        }

        public override string ToString()
        {
            return KeywordValue.ToString(CultureInfo.InvariantCulture);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return KeywordValue.ToString(format, formatProvider);
        }
    }
}
