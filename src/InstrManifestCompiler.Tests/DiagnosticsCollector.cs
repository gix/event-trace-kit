namespace InstrManifestCompiler.Tests
{
    using System.Collections.Generic;
    using System.Globalization;
    using InstrManifestCompiler.Support;

    internal sealed class DiagnosticsCollector : IDiagnostics
    {
        public DiagnosticsCollector()
        {
            Errors = new List<Diagnostic>();
            Diagnostics = new List<Diagnostic>();
        }

        public bool ErrorOccurred => ErrorCount > 0;
        public int ErrorCount { get; private set; }

        public void Report(
            DiagnosticSeverity severity, SourceLocation location, string message, params object[] args)
        {
            var diag = new Diagnostic(severity, location, message, args);
            if (severity == DiagnosticSeverity.Error) {
                ++ErrorCount;
                Errors.Add(diag);
            }
            Diagnostics.Add(diag);
        }

        public List<Diagnostic> Errors { get; private set; }
        public List<Diagnostic> Diagnostics { get; private set; }

        public sealed class Diagnostic
        {
            public Diagnostic(
                DiagnosticSeverity severity, SourceLocation location,
                string message, object[] args)
            {
                Severity = severity;
                Location = location;
                Message = message;
                Args = args;
            }

            public DiagnosticSeverity Severity { get; private set; }
            public SourceLocation Location { get; private set; }
            public string Message { get; private set; }
            public object[] Args { get; private set; }

            public string FormattedMessage =>
                string.Format(CultureInfo.InvariantCulture, Message, Args);

            public override string ToString()
            {
                return FormattedMessage;
            }
        }
    }
}
