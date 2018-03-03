namespace EventManifestCompiler
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using EventManifestCompiler.Extensions;
    using EventManifestCompiler.Native;
    using EventManifestCompiler.ResGen;
    using EventManifestFramework;
    using EventManifestFramework.Support;

    internal sealed class DumpAction : IAction
    {
        private readonly IDiagnostics diags;
        private readonly EmcOpts opts;

        public DumpAction(IDiagnostics diags, EmcOpts opts)
        {
            this.diags = diags ?? throw new ArgumentNullException(nameof(diags));
            this.opts = opts ?? throw new ArgumentNullException(nameof(opts));
        }

        public int Execute()
        {
            var msgTableFile = opts.DumpMessageTable;
            var wevtFile = opts.DumpEventTemplate;
            var metadata = EventManifestParser.LoadWinmeta(diags);
            var d = new EventTemplateReader(diags, metadata);

            if (msgTableFile != null) {
                try {
                    Console.WriteLine("--- Dump of message table '{0}' ---", msgTableFile);
                    if (IsModule(wevtFile))
                        WithWevtTemplateResource(wevtFile, s => d.DumpMessageTable(s));
                    else
                        d.DumpMessageTable(msgTableFile);
                } catch (Exception ex) {
                    diags.ReportError(ex.Message);
                }
            }

            if (wevtFile != null) {
                try {
                    Console.WriteLine("--- Dump of WEVT template '{0}' ---", wevtFile);
                    if (IsModule(wevtFile))
                        WithWevtTemplateResource(wevtFile, s => d.DumpWevtTemplate(s));
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

        private static void WithWevtTemplateResource(string fileName, Action<Stream> action)
        {
            var module = SafeModuleHandle.LoadImageResource(fileName);
            if (module.IsInvalid)
                throw new Win32Exception();

            using (module)
            using (var stream = module.OpenResource("WEVT_TEMPLATE", 1))
                action(stream);
        }
    }
}
