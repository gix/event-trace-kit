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
        private readonly long ns100Ticks;

        public TimePoint(long ns100Ticks)
        {
            this.ns100Ticks = ns100Ticks;
        }

        public static TimePoint FromNanoseconds(long nanoseconds)
        {
            return new TimePoint(nanoseconds * 100);
        }

        public static TimePoint Zero => new TimePoint();
        public static TimePoint MinValue => new TimePoint(long.MinValue);
        public static TimePoint MaxValue => new TimePoint(long.MaxValue);

        public long Ticks => ns100Ticks;
        public long ToNanoseconds => ns100Ticks * 100;
        public long ToMicroseconds => ns100Ticks / 10;
        public long ToMilliseconds => ns100Ticks / 10000;
        public long ToSeconds => ns100Ticks / 10000000;

        public static TimePoint Abs(TimePoint value)
        {
            return FromNanoseconds(Math.Abs(value.ns100Ticks));
        }

        public static TimePoint Min(TimePoint lhs, TimePoint rhs)
        {
            return new TimePoint(Math.Min(lhs.ToNanoseconds, rhs.ToNanoseconds));
        }

        public static TimePoint Max(TimePoint lhs, TimePoint rhs)
        {
            return new TimePoint(Math.Max(lhs.ToNanoseconds, rhs.ToNanoseconds));
        }

        public int CompareTo(TimePoint other)
        {
            return
                ns100Ticks < other.ns100Ticks ? -1 :
                ns100Ticks <= other.ns100Ticks ? 0 :
                1;
        }

        public bool Equals(TimePoint other)
        {
            return ns100Ticks == other.ns100Ticks;
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
            return ns100Ticks.GetHashCode();
        }

        public override string ToString()
        {
            return ns100Ticks.ToString("D");
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return TimePointFormatter.ToString(ns100Ticks, format, formatProvider);
        }

        public static TimePoint Parse(string s)
        {
            return new TimePoint(long.Parse(s));
        }
    }
}