namespace EventTraceKit.EventTracing.Tests.Support
{
    using System;
    using System.Globalization;
    using EventTraceKit.EventTracing.Support;
    using Xunit;

    public class LocatedRefTest
    {
        private class X
        {
            public X(int value = 23)
            {
                Value = value;
            }

            public int Value { get; }

            public override string ToString()
            {
                return $"X({Value})";
            }
        }

        [Fact]
        public void Constructor()
        {
            var value = new X();
            var location = new SourceLocation("z:\\foo", 23, 42);

            var located = new LocatedRef<X>(value, location);

            Assert.Equal(value, located.Value);
            Assert.Equal(location, located.Location);
        }

        [Fact]
        public void ImplicitConversion()
        {
            var value = new X();

            X convertedValue = new LocatedRef<X>(value, new SourceLocation());
            LocatedRef<X> convertedRef = convertedValue;

            Assert.Same(value, convertedValue);
            Assert.Same(value, convertedRef.Value);
            Assert.Null(convertedRef.Location);
        }

        private class EquatableX : IEquatable<EquatableX>
        {
            public EquatableX(int value = 23)
            {
                Value = value;
            }

            public int Value { get; }

            public bool Equals(EquatableX other)
            {
                return other != null && Value == other.Value;
            }

            public override bool Equals(object obj)
            {
                if (obj is EquatableX other)
                    return Equals(other);
                return Value.Equals(obj);
            }

            public override int GetHashCode()
            {
                return Value.GetHashCode();
            }
        }

        [Fact]
        public void Equatable()
        {
            var sameValue = new EquatableX(1);
            var equalValue = new EquatableX(1);
            var notEqualValue = new EquatableX(3);
            var location = new SourceLocation("a", 1, 1);
            var otherLocation = new SourceLocation("b", 2, 2);

            var located = new LocatedRef<EquatableX>(sameValue, location);

            var same = new LocatedRef<EquatableX>(sameValue);
            var equal = new LocatedRef<EquatableX>(equalValue);
            var notEqual = new LocatedRef<EquatableX>(notEqualValue);

            var sameWithLoc = new LocatedRef<EquatableX>(sameValue, otherLocation);
            var equalWithLoc = new LocatedRef<EquatableX>(equalValue, otherLocation);
            var notEqualWithLoc = new LocatedRef<EquatableX>(notEqualValue, otherLocation);

            Assert.True(located.Equals(located));
            Assert.True(located.Equals(same));
            Assert.True(located.Equals(equal));
            Assert.False(located.Equals(notEqual));

            Assert.True(located.Equals(sameWithLoc));
            Assert.True(located.Equals(equalWithLoc));
            Assert.False(located.Equals(notEqualWithLoc));

            Assert.True(located.Equals((object)same));
            Assert.True(located.Equals((object)equal));
            Assert.False(located.Equals((object)notEqual));

            Assert.True(located.Equals((object)sameWithLoc));
            Assert.True(located.Equals((object)equalWithLoc));
            Assert.False(located.Equals((object)notEqualWithLoc));

            Assert.True(located.Equals(sameValue));
            Assert.True(located.Equals(equalValue));
            Assert.False(located.Equals(notEqualValue));

            Assert.True(located.Equals((object)sameValue));
            Assert.True(located.Equals((object)equalValue));
            Assert.False(located.Equals((object)notEqualValue));

            Assert.True(located.Equals((object)sameValue.Value));
            Assert.True(located.Equals((object)equalValue.Value));
            Assert.False(located.Equals((object)notEqualValue.Value));
        }

        private class ComparableX : IComparable<ComparableX>
        {
            public ComparableX(int value = 23)
            {
                Value = value;
            }

            public int Value { get; }

            public int CompareTo(ComparableX other)
            {
                return Value.CompareTo(other.Value);
            }
        }

        [Fact]
        public void Comparable()
        {
            var sameValue = new ComparableX(10);
            var equalValue = new ComparableX(10);
            var smallerValue = new ComparableX(9);
            var largerValue = new ComparableX(11);
            var location = new SourceLocation("a", 1, 1);
            var otherLocation = new SourceLocation("b", 2, 2);

            var located = new LocatedRef<ComparableX>(sameValue, location);

            var same = new LocatedRef<ComparableX>(sameValue);
            var equal = new LocatedRef<ComparableX>(equalValue);
            var smaller = new LocatedRef<ComparableX>(smallerValue);
            var larger = new LocatedRef<ComparableX>(largerValue);

            var sameWithLoc = new LocatedRef<ComparableX>(sameValue, otherLocation);
            var equalWithLoc = new LocatedRef<ComparableX>(equalValue, otherLocation);
            var smallerWithLoc = new LocatedRef<ComparableX>(smallerValue, otherLocation);
            var largerWithLoc = new LocatedRef<ComparableX>(largerValue, otherLocation);

            Assert.Equal(0, located.CompareTo(located));
            Assert.Equal(0, located.CompareTo(same));
            Assert.Equal(0, located.CompareTo(equal));
            Assert.Equal(1, located.CompareTo(smaller));
            Assert.Equal(-1, located.CompareTo(larger));

            Assert.Equal(0, located.CompareTo(sameWithLoc));
            Assert.Equal(0, located.CompareTo(equalWithLoc));
            Assert.Equal(1, located.CompareTo(smallerWithLoc));
            Assert.Equal(-1, located.CompareTo(largerWithLoc));

            Assert.Equal(0, located.CompareTo(sameValue));
            Assert.Equal(0, located.CompareTo(equalValue));
            Assert.Equal(1, located.CompareTo(smallerValue));
            Assert.Equal(-1, located.CompareTo(largerValue));
        }

        private class UntypedComparableX : IComparable
        {
            public UntypedComparableX(int value = 23)
            {
                Value = value;
            }

            public int Value { get; }

            public int CompareTo(object other)
            {
                if (ReferenceEquals(this, other)) return 0;
                return other switch
                {
                    UntypedComparableX obj => Value.CompareTo(obj.Value),
                    null => 1,
                    _ => Value.CompareTo(other),
                };
            }
        }

        [Fact]
        public void UntypedComparable()
        {
            var sameValue = new UntypedComparableX(10);
            var equalValue = new UntypedComparableX(10);
            var smallerValue = new UntypedComparableX(9);
            var largerValue = new UntypedComparableX(11);
            var location = new SourceLocation("a", 1, 1);
            var otherLocation = new SourceLocation("b", 2, 2);

            var located = new LocatedRef<UntypedComparableX>(sameValue, location);

            var same = new LocatedRef<UntypedComparableX>(sameValue);
            var equal = new LocatedRef<UntypedComparableX>(equalValue);
            var smaller = new LocatedRef<UntypedComparableX>(smallerValue);
            var larger = new LocatedRef<UntypedComparableX>(largerValue);

            var sameWithLoc = new LocatedRef<UntypedComparableX>(sameValue, otherLocation);
            var equalWithLoc = new LocatedRef<UntypedComparableX>(equalValue, otherLocation);
            var smallerWithLoc = new LocatedRef<UntypedComparableX>(smallerValue, otherLocation);
            var largerWithLoc = new LocatedRef<UntypedComparableX>(largerValue, otherLocation);

            Assert.Equal(0, located.CompareTo((object)located));
            Assert.Equal(0, located.CompareTo((object)same));
            Assert.Equal(0, located.CompareTo((object)equal));
            Assert.Equal(1, located.CompareTo((object)smaller));
            Assert.Equal(-1, located.CompareTo((object)larger));

            Assert.Equal(0, located.CompareTo((object)sameValue));
            Assert.Equal(0, located.CompareTo((object)equalValue));
            Assert.Equal(1, located.CompareTo((object)smallerValue));
            Assert.Equal(-1, located.CompareTo((object)largerValue));

            Assert.Equal(0, located.CompareTo((object)sameValue.Value));
            Assert.Equal(0, located.CompareTo((object)equalValue.Value));
            Assert.Equal(1, located.CompareTo((object)smallerValue.Value));
            Assert.Equal(-1, located.CompareTo((object)largerValue.Value));

            Assert.Equal(0, located.CompareTo((object)sameWithLoc));
            Assert.Equal(0, located.CompareTo((object)equalWithLoc));
            Assert.Equal(1, located.CompareTo((object)smallerWithLoc));
            Assert.Equal(-1, located.CompareTo((object)largerWithLoc));
        }

        private class FormattableX : IFormattable
        {
            public FormattableX(int value = 23)
            {
                Value = value;
            }

            public int Value { get; }

            public string ToString(string format, IFormatProvider formatProvider)
            {
                return "FormattableX(" + Value.ToString(format, formatProvider) + ")";
            }
        }

        [Fact]
        public void Formattable()
        {
            var location = new SourceLocation("a", 1, 1);
            var located = new LocatedRef<FormattableX>(new FormattableX(10), location);
            var located2 = new LocatedRef<X>(new X(10), location);

            Assert.Equal("FormattableX(A)", located.ToString("X", CultureInfo.InvariantCulture));
            Assert.Equal("X(10)", located2.ToString("X", CultureInfo.InvariantCulture));
        }
    }
}
