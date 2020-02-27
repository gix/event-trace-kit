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
    }
}
