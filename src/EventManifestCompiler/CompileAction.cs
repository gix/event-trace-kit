namespace EventManifestCompiler
{
    using System;
    using EventTraceKit.EventTracing.Schema;
    using EventTraceKit.EventTracing.Support;

    internal sealed class CompileAction : IAction
    {
        private readonly IDiagnosticsEngine diags;
        private readonly EmcCommandLineArguments arguments;

        public CompileAction(IDiagnosticsEngine diags, EmcCommandLineArguments arguments)
        {
            this.diags = diags ?? throw new ArgumentNullException(nameof(diags));
            this.arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        }

        public int Execute()
        {
            if (arguments.CompilationOptions.Inputs.Count == 0) {
                diags.ReportError("No input manifest specified.");
                Program.ShowBriefHelp();
                return ExitCode.UserError;
            }

            if (arguments.CompilationOptions.Inputs.Count > 1) {
                diags.ReportError("Too many input manifests specified.");
                Program.ShowBriefHelp();
                return ExitCode.UserError;
            }

            string manifest = arguments.CompilationOptions.Inputs[0];
            try {
                return ProcessManifest(manifest) ? ExitCode.Success : ExitCode.Error;
            } catch (SchemaValidationException ex) {
                var location = new SourceLocation(ex.BaseUri, ex.LineNumber, ex.ColumnNumber);
                diags.ReportError(location, ex.OriginalMessage);
                diags.ReportError("Input manifest '{0}' is invalid.", manifest);
                return ExitCode.UserError;
            }
        }

        private bool ProcessManifest(string manifestFile)
        {
            var compiler = new EventTraceKit.EventTracing.Compilation.EventManifestCompiler(
                diags, arguments.CompilationOptions, new[] { manifestFile });

            return compiler.Run();
        }
    }
}
