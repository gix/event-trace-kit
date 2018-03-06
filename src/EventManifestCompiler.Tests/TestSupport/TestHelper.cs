namespace EventManifestCompiler.Tests.TestSupport
{
    using System.IO;
    using EventManifestCompiler.ResGen;
    using EventManifestFramework;
    using EventManifestFramework.Schema;
    using EventManifestFramework.Support;

    public static class TestHelper
    {
        public static EventManifest LoadManifest(Stream input, string inputName)
        {
            var diagSink = new DiagnosticSink();
            var diags = new DiagnosticsEngine(diagSink);

            var parser = EventManifestParser.CreateWithWinmeta(diags);
            var manifest = parser.ParseManifest(input, inputName);
            MessageHelpers.AssignMessageIds(diags, manifest, () => new StableMessageIdGenerator(diags));

            diagSink.AssertNoErrors();
            return manifest;
        }
    }
}
