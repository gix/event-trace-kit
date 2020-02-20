namespace EventManifestCompiler
{
    using System;
    using System.IO;
    using EventManifestCompiler.Support;
    using EventTraceKit.EventTracing.Support;

    /// <summary>
    ///   Diagnostic consumer that prints all diagnostics to the console.
    /// </summary>
    internal sealed class ConsoleDiagnosticPrinter : IDiagnosticConsumer
    {
        private readonly TextWriter output;

        public ConsoleDiagnosticPrinter(TextWriter output)
        {
            this.output = output ?? throw new ArgumentNullException(nameof(output));
        }

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

            using (new ConsoleColorScope()) {
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Black;
                if (location.FilePath != null) {
                    output.Write(location.FilePath);
                    if (location.LineNumber != -1) {
                        if (location.ColumnNumber != -1)
                            output.Write("({0},{1})", location.LineNumber, location.ColumnNumber);
                        else
                            output.Write("({0})", location.LineNumber);
                    }
                    output.Write(": ");
                }
                if (severity == DiagnosticSeverity.Note) {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    output.Write("note: ");
                } else if (severity == DiagnosticSeverity.Warning) {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    output.Write("warning: ");
                } else if (severity == DiagnosticSeverity.Error) {
                    Console.ForegroundColor = ConsoleColor.Red;
                    output.Write("error: ");
                }
                Console.ForegroundColor = ConsoleColor.White;
                output.WriteLine(message);
            }
        }
    }
}
