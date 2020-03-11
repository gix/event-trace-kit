namespace EventManifestCompiler
{
    using System;
    using System.Linq;
    using EventTraceKit.EventTracing;
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
            bool hasManifests = options.Inputs != null && options.Inputs.Count > 0;
            bool hasResOnlyManifests =
                options.ResourceGenOnlyInputs != null &&
                options.ResourceGenOnlyInputs.Count > 0;

            if (!hasManifests && !hasResOnlyManifests) {
                diags.ReportError("No input manifest specified.");
                Program.ShowBriefHelp();
                return ExitCode.UserError;
            }

            if (hasManifests && options.Inputs.Count > 1) {
                diags.ReportError("Too many input manifests specified.");
                Program.ShowBriefHelp();
                return ExitCode.UserError;
            }

            var parser = EventManifestParser.CreateWithWinmeta(
                diags, options.SchemaPath, options.WinmetaPath);

            var compilation = EventManifestCompilation.Create(diags, options);
            if (compilation == null)
                return ExitCode.Error;

            var trap = diags.TrapError();
            var manifests = options.Inputs.Select(x => parser.ParseManifest(x)).ToList();
            var resGenManifests = options.ResourceGenOnlyInputs.Select(x => parser.ParseManifest(x)).ToList();

            if (trap.ErrorOccurred)
                return ExitCode.Error;

            if (!compilation.AddManifests(manifests) ||
                !compilation.AddResourceGenManifests(resGenManifests))
                return ExitCode.Error;

            return compilation.Emit() ? ExitCode.Success : ExitCode.Error;
        }
    }
}
