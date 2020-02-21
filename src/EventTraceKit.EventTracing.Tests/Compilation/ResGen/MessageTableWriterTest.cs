namespace EventTraceKit.EventTracing.Tests.Compilation.ResGen
{
    using System.IO;
    using System.Linq;
    using EventTraceKit.EventTracing.Compilation.ResGen;
    using EventTraceKit.EventTracing.Schema;
    using EventTraceKit.EventTracing.Support;
    using EventTraceKit.EventTracing.Tests.Compilation.ResGen.TestCases;
    using EventTraceKit.EventTracing.Tests.Compilation.TestSupport;
    using Xunit;

    public class MessageTableWriterTest
    {
        private readonly IDiagnostics diags = new AssertingDiagnostics();

        [Theory]
        [ResGenTestData(typeof(ResGenTestCases), ".msg.bin")]
        public void Write(ExceptionOr<EventManifest> inputManifest, Stream expectedMsgTable)
        {
            Assert.Single(inputManifest.Value.Resources);

            using (var tempStream = new MemoryStream()) {
                using (var writer = new MessageTableWriter(tempStream))
                    writer.Write(inputManifest.Value.Resources[0].Strings.Select(CreateMessage), diags);

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
