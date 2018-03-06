namespace EventManifestCompiler
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using EventManifestCompiler.Extensions;
    using EventManifestCompiler.Native;
    using EventManifestCompiler.ResGen;
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
            var d = new EventTemplateDumper(Console.Out);

            if (msgTableFile != null) {
                try {
                    if (IsModule(wevtFile))
                        WithMessageResource(wevtFile, s => d.DumpMessageTable(s));
                    else
                        d.DumpMessageTable(msgTableFile);
                } catch (Exception ex) {
                    diags.ReportError(ex.Message);
                }
            } else if (wevtFile != null) {
                try {
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

        private static void WithMessageResource(string fileName, Action<Stream> action)
        {
            var module = SafeModuleHandle.LoadImageResource(fileName);
            if (module.IsInvalid)
                throw new Win32Exception();

            using (module)
            using (var stream = module.OpenResource((IntPtr)11, 1))
                action(stream);
        }
    }
}
