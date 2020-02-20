namespace EventTraceKit.EventTracing.Tests.Compilation.TestSupport
{
    using EventTraceKit.EventTracing.Support;
    using Xunit.Sdk;

    public class AssertingDiagnostics : IDiagnosticsEngine
    {
        public IDiagnosticConsumer Consumer { get; set; }

        public bool ErrorOccurred => ErrorCount > 0;
        public int ErrorCount { get; private set; }

        public void Report(
            DiagnosticSeverity severity, SourceLocation location, string message, params object[] args)
        {
            if (severity == DiagnosticSeverity.Error) {
                ++ErrorCount;
                throw new XunitException($"Diagnostic reported: {location}: {string.Format(message, args)}");
            }
        }
    }
}
