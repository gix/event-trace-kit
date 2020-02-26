namespace EventTraceKit.EventTracing.Tests.Compilation.CodeGen
{
    using System;
    using System.IO;
    using EventTraceKit.EventTracing.Compilation;
    using EventTraceKit.EventTracing.Compilation.CodeGen;
    using EventTraceKit.EventTracing.Tests.Compilation.ResGen;
    using EventTraceKit.EventTracing.Tests.Compilation.ResGen.TestCases;
    using EventTraceKit.EventTracing.Tests.Compilation.TestSupport;
    using Xunit;

    public class MCCompatibleCodeGeneratorTest
    {
        private readonly CodeGenOptions options = new CodeGenOptions {
            CodeGenerator = "mc"
        };
        private readonly MCCompatibleCodeGenerator codeGenerator;

        public MCCompatibleCodeGeneratorTest()
        {
            codeGenerator = new MCCompatibleCodeGenerator(options);
        }

        [Theory]
        [ResGenTestData(typeof(ResGenTestCases), ".h")]
        public void Generate(string inputResourceName, Type resourceAnchor, string expectedCode)
        {
            var manifest = TestHelper.LoadManifest(resourceAnchor, inputResourceName);
            var output = new MemoryStream();

            codeGenerator.Generate(manifest, output);

            string actualCode = output.ReadAllText();
            Assert.Equal(NormalizeCode(expectedCode), NormalizeCode(actualCode));
        }

        private static string NormalizeCode(string str)
        {
            str = str.Replace("\r\n", "\n");
            return StripFileComment(str);
        }

        private static string StripFileComment(string str)
        {
            // Start position of the current line.
            int pos = 0;

            while (pos < str.Length) {
                // Is the line a comment?
                if (str.IndexOf("//", pos, 2, StringComparison.Ordinal) == -1)
                    break;

                // If so, look for the end of the comment line.
                int idx = str.IndexOf('\n', pos);
                if (idx == -1)
                    // We reached the end of the string.
                    return string.Empty;

                pos = idx + 1;
            }

            return str.Substring(pos);
        }
    }
}
