namespace EventManifestCompiler
{
    using System;
    using EventTraceKit.EventTracing.Support;

    internal sealed class GenerateManifestFromProviderAction : IAction
    {
        private readonly IDiagnostics diags;
        private readonly EmcCommandLineArguments arguments;

        public GenerateManifestFromProviderAction(IDiagnostics diags, EmcCommandLineArguments arguments)
        {
            this.diags = diags ?? throw new ArgumentNullException(nameof(diags));
            this.arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        }

        public int Execute()
        {
            if (arguments.DecompilationOptions.InputModule is null &&
                (arguments.DecompilationOptions.InputEventTemplate is null || arguments.DecompilationOptions.InputMessageTable is null)) {
                diags.ReportError("No input provider specified.");
                Program.ShowBriefHelp();
                return ExitCode.UserError;
            }

            var decompiler = new EventTemplateDecompiler(
                diags, arguments.DecompilationOptions);

            if (!decompiler.Run())
                return ExitCode.Error;

            return ExitCode.Success;
        }
    }
}
