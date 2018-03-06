namespace EventManifestCompiler.Tests.ResGen
{
    using System.IO;
    using System.Linq;
    using EventManifestCompiler.ResGen;
    using EventManifestCompiler.Tests.ResGen.TestCases;
    using EventManifestCompiler.Tests.TestSupport;
    using EventManifestFramework.Schema;
    using EventManifestFramework.Support;
    using Xunit;

    public class MessageTableWriterTest
    {
        private readonly IDiagnostics diags = new AssertingDiagnostics();

        [Theory]
        [ResGenTestProvider(typeof(ResGenTestCases), ".msg.bin")]
        public void Write(string testCase, EventManifest inputManifest, Stream expectedMsgTable)
        {
            Assert.Single(inputManifest.Resources);

            using (var tempStream = new MemoryStream()) {
                using (var writer = new MessageTableWriter(tempStream))
                    writer.Write(inputManifest.Resources[0].Strings.Select(CreateMessage), diags);

                StreamAssert.SequenceEqual(tempStream, expectedMsgTable, DumpMsg);
            }
        }

        private static Message CreateMessage(LocalizedString str)
        {
            return new Message(str.Name, str.Id, str.Value);
        }

        private static string DumpMsg(Stream stream)
        {
            stream.Position = 0;
            using (var writer = new StringWriter()) {
                var dumper = new EventTemplateDumper(writer);
                dumper.DumpMessageTable(stream);
                return writer.ToString();
            }
        }
    }
}
