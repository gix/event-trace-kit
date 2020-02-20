namespace EventManifestCompiler
{
    using System;
    using System.IO;
    using EventTraceKit.EventTracing.Compilation.ResGen;
    using EventTraceKit.EventTracing.Support;

    internal sealed class DumpAction : IAction
    {
        private readonly IDiagnostics diags;
        private readonly EmcCommandLineArguments opts;

        public DumpAction(IDiagnostics diags, EmcCommandLineArguments opts)
        {
            this.diags = diags ?? throw new ArgumentNullException(nameof(diags));
            this.opts = opts ?? throw new ArgumentNullException(nameof(opts));
        }

        public int Execute()
        {
            var msgTableFile = opts.DumpMessageTable;
            var wevtFile = opts.DumpEventTemplate;
            var d = new EventTemplateDumper(Console.Out);

            if (msgTableFile != null) {
                try {
                    if (IsModule(wevtFile))
                        d.DumpMessageTableResource(wevtFile);
                    else
                        d.DumpMessageTable(msgTableFile);
                } catch (Exception ex) {
                    diags.ReportError(ex.Message);
                }
            } else if (wevtFile != null) {
                try {
                    if (IsModule(wevtFile))
                        d.DumpWevtTemplateResource(wevtFile);
                    else
                        d.DumpWevtTemplate(wevtFile);
                } catch (Exception ex) {
                    diags.ReportError(ex.Message);
                }
            }

            return ExitCode.Success;
        }

        private static bool IsModule(string wevtFile)
        {
            string extension = Path.GetExtension(wevtFile);

            return
                string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".dll", StringComparison.OrdinalIgnoreCase);
        }
    }
}
