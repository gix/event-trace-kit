namespace EventManifestCompiler
{
    using System;
    using EventTraceKit.EventTracing.Compilation;
    using EventTraceKit.EventTracing.Support;

    internal sealed class CompileAction : IAction
    {
        private readonly IDiagnosticsEngine diags;
        private readonly CompilationOptions options;

        public CompileAction(IDiagnosticsEngine diags, CompilationOptions options)
        {
            this.diags = diags ?? throw new ArgumentNullException(nameof(diags));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public int Execute()
        {
            if (options.Inputs == null || options.Inputs.Count == 0) {
                diags.ReportError("No input manifest specified.");
                Program.ShowBriefHelp();
                return ExitCode.UserError;
            }

            if (options.Inputs.Count > 1) {
                diags.ReportError("Too many input manifests specified.");
                Program.ShowBriefHelp();
                return ExitCode.UserError;
            }

            var manifestFile = options.Inputs[0];
            var compilation = EventManifestCompilation.Create(
                manifestFile, diags, options);

            return compilation.Emit() ? ExitCode.Success : ExitCode.Error;
        }
    }
}
