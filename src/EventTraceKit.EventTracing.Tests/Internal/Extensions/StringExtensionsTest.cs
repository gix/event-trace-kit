namespace EventTraceKit.EventTracing.Tests.Internal.Extensions
{
    using EventTraceKit.EventTracing.Internal.Extensions;
    using Xunit;

    public class StringExtensionsTest
    {
        [Theory]
        [InlineData("", "")]
        [InlineData("abc", "abc")]
        [InlineData("{", "{{")]
        [InlineData("abc {0}", "abc {{0}}")]
        [InlineData("abc {{0}}", "abc {{{{0}}}}")]
        [InlineData("abc {{{0}}}", "abc {{{{{{0}}}}}}")]
        public static void EscapeFormatting(string input, string expected)
        {
            Assert.Equal(expected, input.EscapeFormatting());
            Assert.Equal(input, string.Format(input.EscapeFormatting(), new object[0]));
        }

        [Theory]
        [InlineData(null, null, "")]
        [InlineData("abc", null, "")]
        [InlineData(null, "abc", "")]
        [InlineData("", "", "")]
        [InlineData("abc", "", "")]
        [InlineData("", "abc", "")]
        [InlineData("abc", "abc", "abc")]
        [InlineData("abcX", "abc", "abc")]
        [InlineData("abc", "abcX", "abc")]
        [InlineData("abcX", "abcY", "abc")]
        public static void LongestCommonPrefix(string input1, string input2, string expected)
        {
            Assert.Equal(expected, StringExtensions.LongestCommonPrefix(input1, input2));
        }
    }
}
