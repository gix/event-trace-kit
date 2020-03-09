namespace EventManifestCompiler.Build.Tasks.Tests
{
    using System.Linq;
    using Xunit;

    public class CommandLineUtilsTest
    {
        [Theory]
        [InlineData(@"", new string[0])]
        [InlineData(@"a", new string[] { @"a" })]
        [InlineData(@" a  b ", new string[] { @"a", @"b" })]
        [InlineData(@"""abc"" d e", new string[] { @"abc", @"d", @"e" })]
        [InlineData(@"a\\\b d""e f""g h", new string[] { @"a\\\b", @"de fg", @"h" })]
        [InlineData(@"a\\\""b c d", new string[] { @"a\""b", @"c", @"d" })]
        [InlineData(@"a\\\\""b c"" d e", new string[] { @"a\\b c", @"d", @"e" })]
        public void EnumerateCommandLineArgs(string commandLine, string[] expectedArgs)
        {
            Assert.Equal(expectedArgs, CommandLineUtils.EnumerateCommandLineArgs(commandLine).ToArray());
        }
    }
}
