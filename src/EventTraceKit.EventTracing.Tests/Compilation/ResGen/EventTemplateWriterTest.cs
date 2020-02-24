namespace EventTraceKit.EventTracing.Tests.Compilation.ResGen
{
    using System;
    using System.IO;
    using EventTraceKit.EventTracing.Compilation.ResGen;
    using EventTraceKit.EventTracing.Tests.Compilation.ResGen.TestCases;
    using EventTraceKit.EventTracing.Tests.Compilation.TestSupport;
    using Xunit;

    public class EventTemplateWriterTest
    {
        [Theory]
        [ResGenTestData(typeof(ResGenTestCases), ".wevt.bin")]
        public void Write(string inputResourceName, Type resourceAnchor, byte[] expectedWevt)
        {
            var manifest = TestHelper.LoadManifest(resourceAnchor, inputResourceName);

            using var tempFile = new TempFile();
            using (var writer = new EventTemplateWriter(tempFile.Stream)) {
                writer.Version = 3;
                writer.Write(manifest.Providers);
            }

            SequenceAssert.SequenceEqual(expectedWevt, tempFile.Stream.ReadAllBytes());
        }

        private static string DumpWevt(Stream stream)
        {
            stream.Position = 0;
            using var writer = new StringWriter();
            using var dumper = new EventTemplateDumper(writer);
            dumper.Verify = false;
            dumper.DumpWevtTemplate(stream);
            return writer.ToString();
        }
    }
}
