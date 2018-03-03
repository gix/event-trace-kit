namespace EventManifestCompiler.X
{
    using System;
    using System.ComponentModel.Composition;
    using System.IO;
    using EventManifestFramework.Support;
    using EventManifestCompiler.Support;

    [Export(typeof(IDiagnostics))]
    public sealed class DiagnosticsEngine2 : IDiagnosticsEngine
    {
        private IDiagnosticConsumer consumer;

        public DiagnosticsEngine2()
            : this(new IgnoringDiagnosticConsumer())
        {
        }

        public DiagnosticsEngine2(IDiagnosticConsumer consumer)
        {
            Consumer = consumer;
        }

        public bool ErrorOccurred => ErrorCount > 0;
        public int ErrorCount { get; private set; }

        public IDiagnosticConsumer Consumer
        {
            get => consumer;
            set => consumer = value ?? new IgnoringDiagnosticConsumer();
        }

        public void Report(
            DiagnosticSeverity severity, SourceLocation location, string message, params object[] args)
        {
            if (!Enum.IsDefined(typeof(DiagnosticSeverity), severity))
                throw new ArgumentOutOfRangeException(nameof(severity), severity, null);
            if (location == null)
                throw new ArgumentNullException(nameof(location));
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (severity == DiagnosticSeverity.Error)
                ++ErrorCount;
            if (args != null && args.Length > 0)
                message = string.Format(message, args);
            Consumer.HandleDiagnostic(severity, location, message);
        }
    }

    /// <summary>
    ///   An error trap for a specific diagnostic engine. The trap allows determining
    ///   whether errors occurred after the trap was created or reset.
    /// </summary>
    internal sealed class DiagnosticErrorTrap
    {
        private readonly IDiagnostics diags;
        private int savedErrorCount;

        /// <summary>
        ///   Initializes a new instance of the <see cref="DiagnosticErrorTrap"/>
        ///   class for the specified diagnostics engine.
        /// </summary>
        /// <param name="diags">
        ///   The diagnostics engine to monitor.
        /// </param>
        public DiagnosticErrorTrap(IDiagnostics diags)
        {
            this.diags = diags ?? throw new ArgumentNullException(nameof(diags));
            Reset();
        }

        /// <summary>
        ///   Gets the number of errors that occurred since the trap was created
        ///   or reset.
        /// </summary>
        public bool ErrorOccurred => diags.ErrorCount > savedErrorCount;

        /// <summary>Resets the traps error count.</summary>
        public void Reset()
        {
            savedErrorCount = diags.ErrorCount;
        }
    }

    /// <summary>
    ///   Diagnostics engine without a consumer.
    /// </summary>
    internal sealed class NullDiagnostics : IDiagnostics
    {
        public bool ErrorOccurred => ErrorCount > 0;
        public int ErrorCount { get; private set; }

        public void Report(
            DiagnosticSeverity severity, SourceLocation location, string message, params object[] args)
        {
            if (severity == DiagnosticSeverity.Error)
                ++ErrorCount;
            // Ignore.
        }
    }

    /// <summary>Diagnostic consumer that ignores all diagnostics.</summary>
    internal sealed class IgnoringDiagnosticConsumer : IDiagnosticConsumer
    {
        public void HandleDiagnostic(
            DiagnosticSeverity severity, SourceLocation location, string message)
        {
            // Ignored
        }
    }

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
