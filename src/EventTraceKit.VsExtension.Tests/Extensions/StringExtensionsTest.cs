namespace EventTraceKit.VsExtension.Tests.Extensions
{
    using System.Collections.Generic;
    using EventTraceKit.VsExtension.Extensions;
    using Xunit;

    public class StringExtensionsTest
    {
        [Theory]
        [InlineData(null, 0, TrimPosition.End, null)]
        [InlineData("1234567890", 0, TrimPosition.End, "")]
        [InlineData("1234567890", 0, TrimPosition.Start, "")]
        [InlineData("1234567890", 0, TrimPosition.Middle, "")]
        [InlineData("1234567890", 1, TrimPosition.End, "…")]
        [InlineData("1234567890", 1, TrimPosition.Start, "…")]
        [InlineData("1234567890", 1, TrimPosition.Middle, "…")]
        [InlineData("1234567890", 2, TrimPosition.End, "1…")]
        [InlineData("1234567890", 2, TrimPosition.Start, "…0")]
        [InlineData("1234567890", 2, TrimPosition.Middle, "1…")]
        [InlineData("1234567890", 5, TrimPosition.End, "1234…")]
        [InlineData("1234567890", 5, TrimPosition.Start, "…7890")]
        [InlineData("1234567890", 5, TrimPosition.Middle, "12…90")]
        [InlineData("1234567890", 6, TrimPosition.End, "12345…")]
        [InlineData("1234567890", 6, TrimPosition.Start, "…67890")]
        [InlineData("1234567890", 6, TrimPosition.Middle, "123…90")]
        [InlineData("1234567890", 10, TrimPosition.End, "1234567890")]
        [InlineData("1234567890", 10, TrimPosition.Start, "1234567890")]
        [InlineData("1234567890", 10, TrimPosition.Middle, "1234567890")]
        public void TrimToLength(string input, int maxLength, TrimPosition position, string expected)
        {
            Assert.Equal(expected, input.TrimToLength(maxLength, position));
            Assert.Equal(expected, StringExtensions.TrimToLength(input, maxLength, position));
        }

        [Theory]
        [InlineData(null, null, null)]
        [InlineData("foo", "foo", null)]
        [InlineData("foo", "foo", new string[0])]
        [InlineData("foo", "foo", new[] { "bar" })]
        [InlineData("foo", "foo (Copy)", new[] { "foo" })]
        [InlineData("foo bar", "foo bar (Copy)", new[] { "foo bar" })]
        [InlineData("foo bar", "foo bar (Copy 2)", new[] { "foo bar", "foo bar (Copy)" })]
        [InlineData("foo bar", "foo bar (Copy 3)", new[] { "foo bar", "foo bar (Copy)", "foo bar (Copy 2)" })]
        [InlineData("foo bar", "foo bar (Copy)", new[] { "foo bar", "foo bar (Copy 2)" })]
        [InlineData("foo bar", "foo bar (Copy 2)", new[] { "foo bar", "foo bar (Copy)", "foo bar (Copy 3)" })]
        [InlineData("foo (Copy)", "foo (Copy)", new[] { "foo" })]
        [InlineData("foo (Copy)", "foo (Copy 2)", new[] { "foo (Copy)" })]
        [InlineData("foo (Copy 2)", "foo (Copy 3)", new[] { "foo (Copy 2)" })]
        public void MakeNumberedCopy(string input, string expected, string[] used)
        {
            Assert.Equal(expected, input.MakeNumberedCopy(used != null ? new HashSet<string>(used) : null));
            Assert.Equal(expected, StringExtensions.MakeNumberedCopy(input, used != null ? new HashSet<string>(used) : null));
        }
    }
}
