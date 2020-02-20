namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using EventTraceKit.EventTracing.Support;

    internal sealed class DiagnosticCollector : IDiagnostics
    {
        private readonly List<DiagnosticInfo> diagnostics = new List<DiagnosticInfo>();

        public bool ErrorOccurred => ErrorCount > 0;
        public int ErrorCount => diagnostics.Count;

        public void Report(
            DiagnosticSeverity severity, SourceLocation location, string message,
            params object[] args)
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
    }
}
