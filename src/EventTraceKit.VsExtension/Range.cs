namespace EventTraceKit.VsExtension
{
    using System;

    public struct Range
        : IEquatable<Range>, IComparable<Range>, IComparable<int>
    {
        public Range(int begin, int end)
        {
            if (begin > end)
                throw new ArgumentException("Begin must be equal or smaller than end.");
            Begin = begin;
            End = end;
        }

        public int Begin { get; }
        public int End { get; }

        public int Length => End - Begin;

        public int CompareTo(Range other)
        {
            if (Begin < other.Begin) return -1;
            if (Begin > other.Begin) return +1;

            if (End < other.End) return -1;
            if (End > other.End) return +1;

            return 0;
        }

        public int CompareTo(int value)
        {
            if (Begin > value) return +1;
            if (End <= value) return -1;
            return 0;
        }

        public bool Equals(Range other)
        {
            return Begin == other.Begin && End == other.End;
        }

        public override bool Equals(object obj)
        {
            return obj is Range r && Equals(r);
        }

        public override int GetHashCode()
        {
            unchecked {
                return (Begin * 397) ^ End;
            }
        }

        public override string ToString()
        {
            return "[" + Begin + "," + End + ")";
        }

        public static bool operator ==(Range lhs, Range rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(Range lhs, Range rhs)
        {
            return !lhs.Equals(rhs);
        }
    }
}
