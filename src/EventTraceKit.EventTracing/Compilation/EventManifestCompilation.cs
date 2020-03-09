namespace EventTraceKit.EventTracing.Compilation
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using EventTraceKit.EventTracing;
    using EventTraceKit.EventTracing.Compilation.CodeGen;
    using EventTraceKit.EventTracing.Compilation.ResGen;
    using EventTraceKit.EventTracing.Compilation.Support;
    using EventTraceKit.EventTracing.Internal.Extensions;
    using EventTraceKit.EventTracing.Schema;
    using EventTraceKit.EventTracing.Support;

    public sealed class EventManifestCompilation
    {
        private readonly EventManifest manifest;
        private readonly IDiagnosticsEngine diags;
        private readonly CompilationOptions opts;
        private readonly Func<IMessageIdGenerator> msgIdGenFactory;

        private EventManifestCompilation(
            EventManifest manifest, IDiagnosticsEngine diags, CompilationOptions opts)
        {
            this.manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
            this.diags = diags ?? throw new ArgumentNullException(nameof(diags));
            this.opts = opts ?? throw new ArgumentNullException(nameof(opts));

            msgIdGenFactory = () => new StableMessageIdGenerator(diags);

            MessageHelpers.AssignMessageIds(diags, manifest, msgIdGenFactory);
        }

        public static EventManifestCompilation Create(
            EventManifest manifest, IDiagnosticsEngine diags, CompilationOptions opts)
        {
            return new EventManifestCompilation(manifest, diags, opts);
        }

        public static EventManifestCompilation Create(
            string manifestFile, IDiagnosticsEngine diags, CompilationOptions opts)
        {
            if (!File.Exists(manifestFile)) {
                diags.ReportError("No such file '{0}'.", manifestFile);
                return null;
            }

            var parser = EventManifestParser.CreateWithWinmeta(
                diags, opts.SchemaPath, opts.WinmetaPath);

            var manifest = parser?.ParseManifest(manifestFile);
            return new EventManifestCompilation(manifest, diags, opts);
        }

        public bool Emit()
        {
            if (diags.ErrorOccurred)
                return false;

            if (opts.GenerateResources) {
                EmitEventTemplate();
                EmitMessageTables();
                EmitResourceFile();
            }

            if (opts.CodeGenOptions.GenerateCode)
                EmitCode();

            return !diags.ErrorOccurred;
        }

        public bool EmitEventTemplate()
        {
            var trap = diags.TrapError();

            FileStream output = FileUtilities.TryCreateFile(
                diags, opts.EventTemplateFile, 4096, FileOptions.RandomAccess);
            if (output == null)
                return false;

            using (output)
            using (var writer = new EventTemplateWriter(output)) {
                if (opts.CompatibilityLevel <= new Version(8, 1))
                    writer.UseLegacyTemplateIds = true;
                else if (opts.CompatibilityLevel < new Version(10, 0, 16299))
                    writer.Version = 3;
                writer.Write(manifest.Providers);
            }

            return trap.ErrorOccurred;
        }

        public bool EmitCode()
        {
            var trap = diags.TrapError();

            ICodeGenerator codeGen = opts.CodeGenOptions.CodeGeneratorFactory?.Invoke();
            if (codeGen == null)
                return false;

            using var output = FileUtilities.TryCreateFile(diags, opts.CodeGenOptions.CodeHeaderFile);
            if (output == null)
                return false;
            codeGen.Generate(manifest, output);

            return trap.ErrorOccurred;
        }

        public bool EmitMessageTables()
        {
            var trap = diags.TrapError();

            foreach (var resourceSet in manifest.Resources) {
                string fileName = AddCultureName(opts.MessageTableFile, resourceSet.Culture);
                using FileStream output = FileUtilities.TryCreateFile(diags, fileName);
                if (output == null)
                    continue;

                using var writer = new MessageTableWriter(output);
                writer.Write(resourceSet.Strings.Select(CreateMessage), diags);
            }

            return trap.ErrorOccurred;
        }

        public bool EmitResourceFile()
        {
            var trap = diags.TrapError();

            using FileStream output = FileUtilities.TryCreateFile(diags, opts.ResourceFile);
            if (output == null)
                return false;

            using var writer = IO.CreateStreamWriter(output);
            writer.NewLine = "\n";

            foreach (var resourceSet in manifest.Resources.OrderBy(ResourceSortKey)) {
                CultureInfo culture = resourceSet.Culture;
                string fileName = AddCultureName(opts.MessageTableFile, culture);
                int primaryLangId = culture.GetPrimaryLangId();
                int subLangId = culture.GetSubLangId();

                writer.WriteLine("LANGUAGE 0x{0:X},0x{1:X}", primaryLangId, subLangId);
                writer.WriteLine("1 11 \"{0}\"", fileName);
            }

            writer.WriteLine("1 WEVT_TEMPLATE \"{0}\"", opts.EventTemplateFile);

            return trap.ErrorOccurred;
        }

        private static Tuple<int, int> ResourceSortKey(LocalizedResourceSet resourceSet)
        {
            CultureInfo culture = resourceSet.Culture;
            return Tuple.Create(culture.GetPrimaryLangId(), culture.GetSubLangId());
        }

        private static Message CreateMessage(LocalizedString str)
        {
            return new Message(str.Name, str.Id, str.Value);
        }

        private static string AddCultureName(string fileName, CultureInfo culture)
        {
            int idx = fileName.IndexOf("<culture>", StringComparison.Ordinal);
            if (idx != -1)
                return fileName.Replace("<culture>", culture.Name);

            string ext = Path.GetExtension(fileName);
            return Path.ChangeExtension(fileName, "." + culture.Name + ext);
        }
    }
}
