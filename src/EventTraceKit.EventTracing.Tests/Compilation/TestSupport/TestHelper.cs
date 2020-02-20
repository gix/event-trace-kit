namespace EventTraceKit.EventTracing.Tests.Compilation.TestSupport
{
    using System.IO;
    using EventTraceKit.EventTracing;
    using EventTraceKit.EventTracing.Compilation;
    using EventTraceKit.EventTracing.Compilation.ResGen;
    using EventTraceKit.EventTracing.Schema;
    using EventTraceKit.EventTracing.Support;

    public static class TestHelper
    {
        public static EventManifest LoadManifest(Stream input, string inputName)
        {
            var diagSink = new DiagnosticSink();
            var diags = new DiagnosticsEngine(diagSink);

            var parser = EventManifestParser.CreateWithWinmeta(diags);
            var manifest = parser.ParseManifest(input, inputName);
            diagSink.AssertNoErrors();

            MessageHelpers.AssignMessageIds(diags, manifest, () => new StableMessageIdGenerator(diags));

            diagSink.AssertNoErrors();
            return manifest;
        }
    }
}
