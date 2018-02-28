namespace EventTraceKit.VsExtension.Tests
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Xunit;

    public class TimePointTest
    {
        [Fact]
        public void DefaultConstructor()
        {
            var tp = new TimePoint();
            Assert.Equal(0, tp.Ticks);
        }

        [Theory]
        [InlineData(0L)]
        [InlineData(100L)]
        [InlineData(long.MaxValue)]
        public void TicksConstructor(long ticks)
        {
            var tp = new TimePoint(ticks);
            Assert.Equal(ticks, tp.Ticks);
        }

        [Fact]
        public void IsSequentialStruct()
        {
            Assert.True(typeof(TimePoint).IsValueType);
            Assert.Equal(LayoutKind.Sequential, typeof(TimePoint).StructLayoutAttribute?.Value);
        }

        [Fact]
        public void HasTypeConverter()
        {
            Assert.Equal(
                typeof(TimePointConverter).AssemblyQualifiedName,
                typeof(TimePoint).GetCustomAttribute<TypeConverterAttribute>().ConverterTypeName);
        }

        [Theory]
        [InlineData(0L, 0L)]
        [InlineData(1234567800L, 12345678L)]
        public void FromNanoseconds(long ns, long expectedTicks)
        {
            Assert.Equal(new TimePoint(expectedTicks), TimePoint.FromNanoseconds(ns));
        }

        [Fact]
        public void SpecialValues()
        {
            Assert.Equal(new TimePoint(), TimePoint.Zero);
            Assert.Equal(new TimePoint(long.MinValue), TimePoint.MinValue);
            Assert.Equal(new TimePoint(long.MaxValue), TimePoint.MaxValue);
        }

        [Theory]
        [InlineData(0L, 0L, 0L, 0L, 0L)]
        [InlineData(12345678L, 1234567800L, 1234567L, 1234L, 1L)]
        public void Conversions(long ticks, long ns, long us, long ms, long s)
        {
            Assert.Equal(ns, new TimePoint(ticks).TotalNanoseconds);
            Assert.Equal(us, new TimePoint(ticks).TotalMicroseconds);
            Assert.Equal(ms, new TimePoint(ticks).TotalMilliseconds);
            Assert.Equal(s, new TimePoint(ticks).TotalSeconds);
        }

        [Theory]
        [InlineData(0L)]
        [InlineData(-1L)]
        [InlineData(1L)]
        public void Abs(long ticks)
        {
            Assert.Equal(new TimePoint(Math.Abs(ticks)), TimePoint.Abs(new TimePoint(ticks)));
        }

        [Theory]
        [InlineData(0L, 1L)]
        [InlineData(-10L, 10L)]
        [InlineData(42L, 42L)]
        public void Min(long smaller, long greater)
        {
            Assert.Equal(new TimePoint(smaller), TimePoint.Min(new TimePoint(smaller), new TimePoint(greater)));
            Assert.Equal(new TimePoint(smaller), TimePoint.Min(new TimePoint(greater), new TimePoint(smaller)));
        }

        [Theory]
        [InlineData(0L, 1L)]
        [InlineData(-10L, 10L)]
        [InlineData(42L, 42L)]
        public void Max(long smaller, long greater)
        {
            Assert.Equal(new TimePoint(greater), TimePoint.Max(new TimePoint(smaller), new TimePoint(greater)));
            Assert.Equal(new TimePoint(greater), TimePoint.Max(new TimePoint(greater), new TimePoint(smaller)));
        }

        [Fact]
        public void EqualsT()
        {
            var tp1 = new TimePoint(100);
            var tp2 = new TimePoint(100);
            var tp3 = new TimePoint(200);

            Assert.True(tp1.Equals(tp1));

            Assert.True(tp1.Equals(tp2));
            Assert.True(tp2.Equals(tp1));

            Assert.True(!tp1.Equals(tp3));
            Assert.True(!tp3.Equals(tp1));
        }

        [Fact]
        public void EqualsObject()
        {
            var tp1 = new TimePoint(100);
            var tp2 = new TimePoint(100);
            var tp3 = new TimePoint(200);

            Assert.True(tp1.Equals((object)tp1));

            Assert.True(tp1.Equals((object)tp2));
            Assert.True(tp2.Equals((object)tp1));

            Assert.True(!tp1.Equals((object)tp3));
            Assert.True(!tp3.Equals((object)tp1));

            Assert.True(!tp1.Equals((object)null));
            Assert.True(!tp1.Equals((object)100));
        }

        [Fact]
        public void EqualsOperator()
        {
            var tp1 = new TimePoint(100);
            var tp2 = new TimePoint(100);
            var tp3 = new TimePoint(200);

#pragma warning disable CS1718 // Comparison made to same variable
            Assert.True(tp1 == tp1);
            Assert.False(tp1 != tp1);
#pragma warning restore CS1718 // Comparison made to same variable

            Assert.True(tp1 == tp2);
            Assert.True(tp2 == tp1);
            Assert.False(tp1 != tp2);
            Assert.False(tp2 != tp1);

            Assert.True(tp1 != tp3);
            Assert.True(tp3 != tp1);
            Assert.False(tp1 == tp3);
            Assert.False(tp3 == tp1);
        }

        [Fact]
        public void CompareTo()
        {
            var tp1 = new TimePoint(100);
            var tp2 = new TimePoint(100);
            var tp3 = new TimePoint(200);

            Assert.True(tp1.CompareTo(tp1) == 0);
            Assert.True(tp1.CompareTo(tp2) == 0);
            Assert.True(tp1.CompareTo(tp3) == -1);
            Assert.True(tp3.CompareTo(tp1) == 1);
        }

        [Fact]
        public void ComparisonOperators()
        {
            var tp1 = new TimePoint(100);
            var tp2 = new TimePoint(100);
            var tp3 = new TimePoint(200);

#pragma warning disable CS1718 // Comparison made to same variable
            Assert.False(tp1 < tp1);
            Assert.False(tp1 < tp2);
            Assert.True(tp1 < tp3);
            Assert.False(tp3 < tp1);

            Assert.True(tp1 <= tp1);
            Assert.True(tp1 <= tp2);
            Assert.True(tp1 <= tp3);
            Assert.False(tp3 <= tp1);

            Assert.False(tp1 > tp1);
            Assert.False(tp1 > tp2);
            Assert.False(tp1 > tp3);
            Assert.True(tp3 > tp1);

            Assert.True(tp1 >= tp1);
            Assert.True(tp1 >= tp2);
            Assert.False(tp1 >= tp3);
            Assert.True(tp3 >= tp1);
#pragma warning restore CS1718 // Comparison made to same variable
        }

        [Theory]
        [InlineData(0L)]
        [InlineData(100L)]
        [InlineData(long.MaxValue)]
        public void HashCode(long ticks)
        {
            Assert.Equal(ticks.GetHashCode(), new TimePoint(ticks).GetHashCode());
        }

        public static IEnumerable<object[]> StringCases
        {
            get
            {
                yield return new object[] { 0L, "0" };
                yield return new object[] { -1L, "-1" };
                yield return new object[] { 42L, "42" };
                yield return new object[] { long.MinValue, "-9223372036854775808" };
                yield return new object[] { long.MaxValue, "9223372036854775807" };
            }
        }

        [Theory]
        [MemberData(nameof(StringCases))]
        public void StringConversion(long ticks, string str)
        {
            Assert.Equal(str, new TimePoint(ticks).ToString());
        }

        [Theory]
        [MemberData(nameof(StringCases))]
        public void Parse(long ticks, string str)
        {
            Assert.Equal(new TimePoint(ticks), TimePoint.Parse(str));
        }
    }
}
