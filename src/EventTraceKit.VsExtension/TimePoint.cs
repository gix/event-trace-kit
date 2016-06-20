namespace EventTraceKit.VsExtension
{
    using System;
    using System.ComponentModel;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    [TypeConverter(typeof(TimePointConverter))]
    public struct TimePoint
        : IComparable<TimePoint>
            , IEquatable<TimePoint>
            , IFormattable
    {
        private readonly long nanoseconds;

        public TimePoint(long nanoseconds)
        {
            this.nanoseconds = nanoseconds;
        }

        public static TimePoint FromNanoseconds(long nanoseconds)
        {
            return new TimePoint(nanoseconds);
        }

        public long ToNanoseconds => nanoseconds;
        public long ToMicroseconds => nanoseconds / 1000;
        public long ToMilliseconds => nanoseconds / 1000000;
        public long ToSeconds => nanoseconds / 1000000000;

        public static TimePoint Abs(TimePoint value)
        {
            return FromNanoseconds(Math.Abs(value.nanoseconds));
        }

        public static TimePoint Min(TimePoint lhs, TimePoint rhs)
        {
            return new TimePoint(Math.Min(lhs.ToNanoseconds, rhs.ToNanoseconds));
        }

        public static TimePoint Max(TimePoint lhs, TimePoint rhs)
        {
            return new TimePoint(Math.Max(lhs.ToNanoseconds, rhs.ToNanoseconds));
        }

        public static TimePoint Zero => new TimePoint();

        public static TimePoint MinValue => new TimePoint(-9223372036854775808L);

        public static TimePoint MaxValue => new TimePoint(9223372036854775807);

        public int CompareTo(TimePoint other)
        {
            return
                nanoseconds < other.nanoseconds ? -1 :
                    nanoseconds <= other.nanoseconds ? 0 :
                        1;
        }

        public bool Equals(TimePoint other)
        {
            return nanoseconds == other.nanoseconds;
        }

        public static bool operator ==(TimePoint lhs, TimePoint rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(TimePoint lhs, TimePoint rhs)
        {
            return !lhs.Equals(rhs);
        }

        public static bool operator <(TimePoint lhs, TimePoint rhs)
        {
            return lhs.CompareTo(rhs) < 0;
        }

        public static bool operator >(TimePoint lhs, TimePoint rhs)
        {
            return lhs.CompareTo(rhs) > 0;
        }

        public static bool operator <=(TimePoint lhs, TimePoint rhs)
        {
            return lhs.CompareTo(rhs) <= 0;
        }

        public static bool operator >=(TimePoint lhs, TimePoint rhs)
        {
            return lhs.CompareTo(rhs) >= 0;
        }

        public override bool Equals(object other)
        {
            return other is TimePoint && Equals((TimePoint)other);
        }

        public override int GetHashCode()
        {
            return nanoseconds.GetHashCode();
        }

        public override string ToString()
        {
            return nanoseconds.ToString("F0");
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return TimePointFormatter.ToString(nanoseconds, format, formatProvider);
        }

        public static TimePoint Parse(string s)
        {
            return new TimePoint(long.Parse(s));
        }
    }
}