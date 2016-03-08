namespace InstrManifestCompiler
{
    using System;
    using System.Diagnostics.Contracts;
    using InstrManifestCompiler.ResGen;

    internal sealed class DumpAction : IAction
    {
        private readonly IDiagnostics diags;
        private readonly ImcOpts opts;

        public DumpAction(IDiagnostics diags, ImcOpts opts)
        {
            Contract.Requires<ArgumentNullException>(diags != null);
            Contract.Requires<ArgumentNullException>(opts != null);
            this.diags = diags;
            this.opts = opts;
        }

        public int Execute()
        {
            var msgTableFile = opts.DumpMessageTable;
            var wevtFile = opts.DumpEventTemplate;
            var d = new ResourceDumper();

            if (msgTableFile != null) {
                try {
                    Console.WriteLine("--- Dump of message table '{0}' ---", msgTableFile);
                    d.DumpMessageTable(msgTableFile);
                } catch (Exception ex) {
                    diags.ReportError(ex.Message);
                }
            }

            if (wevtFile != null) {
                try {
                    Console.WriteLine("--- Dump of WEVT template '{0}' ---", wevtFile);
                    d.DumpWevtTemplate(wevtFile);
                } catch (Exception ex) {
                    diags.ReportError(ex.Message);
                }
            }

            return ExitCode.Success;
        }
    }
}
