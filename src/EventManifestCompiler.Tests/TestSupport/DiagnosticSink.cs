namespace EventManifestCompiler.Tests.TestSupport
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using EventManifestFramework.Support;
    using Xunit.Sdk;

    internal class DiagnosticSink : IDiagnosticConsumer
    {
        private readonly List<DiagnosticInfo> diagnostics = new List<DiagnosticInfo>();

        public void HandleDiagnostic(
            DiagnosticSeverity severity, SourceLocation location, string message)
        {
            if (!Enum.IsDefined(typeof(DiagnosticSeverity), severity))
                throw new ArgumentOutOfRangeException(nameof(severity), severity, null);
            if (location == null)
                throw new ArgumentNullException(nameof(location));
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (severity == DiagnosticSeverity.Ignored)
                return;

            diagnostics.Add(new DiagnosticInfo(severity, location, message));
        }

        public IReadOnlyList<DiagnosticInfo> Diagnostics => diagnostics;

        public sealed class DiagnosticInfo
        {
            public DiagnosticInfo(DiagnosticSeverity severity, SourceLocation location, string message)
            {
                Severity = severity;
                Location = location;
                Message = message;
            }

            public DiagnosticSeverity Severity { get; }

            public SourceLocation Location { get; }

            public string Message { get; }
        }

        public void AssertNoErrors()
        {
            var errors = Diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error).ToList();
            if (errors.Count == 0)
                return;

            throw new XunitException(
                "Diagnostic errors reported:\n" +
                string.Join("\n", errors.Select(x => $"{x.Location}: {x.Message}")));
        }
    }
}
