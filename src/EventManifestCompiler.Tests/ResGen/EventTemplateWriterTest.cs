namespace EventManifestCompiler.Tests.ResGen
{
    using System.IO;
    using EventManifestCompiler.ResGen;
    using EventManifestCompiler.Tests.ResGen.TestCases;
    using EventManifestCompiler.Tests.TestSupport;
    using EventManifestFramework.Schema;
    using Xunit;

    public class EventTemplateWriterTest
    {
        [Theory]
        [ResGenTestData(typeof(ResGenTestCases), ".wevt.bin")]
        public void Write(string testCase, ExceptionOr<EventManifest> inputManifest, Stream expectedWevt)
        {
            using (var temp = new TempFile()) {
                using (var writer = new EventTemplateWriter(temp.Stream))
                    writer.Write(inputManifest.Value.Providers);
                StreamAssert.SequenceEqual(temp.Stream, expectedWevt, DumpWevt);
            }
        }

        private static string DumpWevt(Stream stream)
        {
            stream.Position = 0;
            using (var writer = new StringWriter()) {
                var dumper = new EventTemplateDumper(writer);
                dumper.DumpWevtTemplate(stream);
                return writer.ToString();
            }
        }
    }
}
