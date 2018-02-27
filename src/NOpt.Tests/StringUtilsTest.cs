namespace NOpt.Tests
{
    using System;
    using Xunit;
    using Xunit.Extensions;

    public class StringUtilsTest
    {
        [Theory]
        [InlineData("abc", "abc", 0)]
        [InlineData("abc", "ab", -1)]
        [InlineData("ab", "abc", +1)]
        public void CompareLongest(string left, string right, int expected)
        {
            Assert.Equal(expected, StringUtils.CompareLongest(left, right, StringComparison.Ordinal));
        }

        [Theory]
        [InlineData("abc", "abc", 0)]
        [InlineData("abc", "ABC", 0)]
        [InlineData("ABC", "abc", 0)]
        [InlineData("abc", "ab", -1)]
        [InlineData("abc", "AB", -1)]
        [InlineData("ABC", "ab", -1)]
        [InlineData("ab", "abc", +1)]
        [InlineData("ab", "ABC", +1)]
        [InlineData("AB", "abc", +1)]
        public void CompareLongestIgnoreCase(string left, string right, int expected)
        {
            Assert.Equal(expected, StringUtils.CompareLongest(left, right, StringComparison.OrdinalIgnoreCase));
        }
    }
}
