namespace EventTraceKit.EventTracing.Support
{
    using System;

    /// <summary>Indicates the severity of a diagnostic.</summary>
    public enum DiagnosticSeverity
    {
        Ignored = 0,
        Note = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
    }

    /// <summary>Allows reporting diagnostics.</summary>
    public interface IDiagnostics
    {
        bool ErrorOccurred { get; }
        int ErrorCount { get; }

        void Report(
            DiagnosticSeverity severity, SourceLocation location,
            string message, params object[] args);
    }

    /// <summary>Extensions for <see cref="IDiagnostics"/>.</summary>
    public static class IDiagnosticsExtensions
    {
        /// <summary>
        ///   Reports a diagnostic without source location.
        /// </summary>
        /// <param name="diags">
        ///   The diagnostic engine to use.
        /// </param>
        /// <param name="severity">
        ///   The severity of the diagnostic.
        /// </param>
        /// <param name="message">
        ///   The diagnostic message.
        /// </param>
        /// <param name="args">
        ///   Optional arguments used to format the <paramref name="message"/>.
        /// </param>
        public static void Report(
            this IDiagnostics diags, DiagnosticSeverity severity, string message, params object[] args)
        {
            diags.Report(severity, new SourceLocation(), message, args);
        }

        /// <summary>
        ///   Reports an error without source location.
        /// </summary>
        /// <param name="diags">
        ///   The diagnostic engine to use.
        /// </param>
        /// <param name="message">
        ///   The diagnostic message.
        /// </param>
        /// <param name="args">
        ///   Optional arguments used to format the <paramref name="message"/>.
        /// </param>
        public static void ReportError(
            this IDiagnostics diags, string message, params object[] args)
        {
            diags.Report(DiagnosticSeverity.Error, message, args);
        }

        /// <summary>
        ///   Reports an error.
        /// </summary>
        /// <param name="diags">
        ///   The diagnostic engine to use.
        /// </param>
        /// <param name="location">
        ///   The source location where the diagnostic occurred.
        /// </param>
        /// <param name="message">
        ///   The diagnostic message.
        /// </param>
        /// <param name="args">
        ///   Optional arguments used to format the <paramref name="message"/>.
        /// </param>
        public static void ReportError(
            this IDiagnostics diags, SourceLocation location, string message, params object[] args)
        {
            diags.Report(DiagnosticSeverity.Error, location, message, args);
        }

        /// <summary>
        ///   Creates an error trap for the specified diagnostic engine. The
        ///   trap allows determining whether errors occurred after the trap was
        ///   created.
        /// </summary>
        /// <param name="diags">
        ///   The diagnostic engine to use.
        /// </param>
        /// <returns/>
        public static DiagnosticErrorTrap TrapError(this IDiagnostics diags)
        {
            return new DiagnosticErrorTrap(diags);
        }
    }

    public interface IDiagnosticsEngine : IDiagnostics
    {
        IDiagnosticConsumer Consumer { get; set; }
    }

    /// <summary>
    ///   Provides functionality to consume diagnostics reported to a
    ///   <see cref="IDiagnostics"/> engine.
    /// </summary>
    public interface IDiagnosticConsumer
    {
        void HandleDiagnostic(
            DiagnosticSeverity severity, SourceLocation location, string message);
    }

    public sealed class DiagnosticsEngine : IDiagnosticsEngine
    {
        private IDiagnosticConsumer consumer;

        public DiagnosticsEngine()
            : this(new IgnoringDiagnosticConsumer())
        {
        }

        public DiagnosticsEngine(IDiagnosticConsumer consumer)
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
    public sealed class DiagnosticErrorTrap
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
    public sealed class NullDiagnostics : IDiagnostics
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
}
