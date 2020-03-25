namespace EventManifestCompiler.Build.Tasks.Tests
{
    using System.Linq;
    using Xunit;

    public class CommandLineUtilsTest
    {
        [Theory]
        [InlineData(@"", new string[0])]
        [InlineData(@"a", new[] { @"a" })]
        [InlineData(@" a  b ", new[] { @"a", @"b" })]
        [InlineData(@"""abc"" d e", new[] { @"abc", @"d", @"e" })]
        [InlineData(@"a\\\b d""e f""g h", new[] { @"a\\\b", @"de fg", @"h" })]
        [InlineData(@"a\\\""b c d", new[] { @"a\""b", @"c", @"d" })]
        [InlineData(@"a\\\\""b c"" d e", new[] { @"a\\b c", @"d", @"e" })]
        public void EnumerateCommandLineArgs(string commandLine, string[] expectedArgs)
        {
            Assert.Equal(expectedArgs, CommandLineUtils.EnumerateCommandLineArgs(commandLine).ToArray());
        }
    }
}
