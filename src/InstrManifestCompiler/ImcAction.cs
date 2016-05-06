namespace InstrManifestCompiler
{
    using System;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using InstrManifestCompiler.CodeGen;
    using InstrManifestCompiler.EventManifestSchema;
    using InstrManifestCompiler.Extensions;
    using InstrManifestCompiler.ResGen;
    using InstrManifestCompiler.Support;

    internal sealed class ImcAction : IAction
    {
        private readonly IDiagnosticsEngine diags;
        private readonly ImcOpts opts;
        private readonly CompositionContainer container;
        private readonly Func<IMessageIdGenerator> msgIdGenFactory;

        public ImcAction(IDiagnosticsEngine diags, ImcOpts opts)
        {
            Contract.Requires<ArgumentNullException>(diags != null);
            Contract.Requires<ArgumentNullException>(opts != null);
            this.diags = diags;
            this.opts = opts;

            var catalog = new AggregateCatalog();
            catalog.Catalogs.Add(new AssemblyCatalog(Assembly.GetExecutingAssembly()));

            container = new CompositionContainer(catalog);
            container.ComposeParts(opts, diags);

            msgIdGenFactory = () => new StableMessageIdGenerator(diags);
        }

        public int Execute()
        {
            if (opts.Inputs.Count == 0) {
                diags.ReportError("No input manifest specified.");
                Program.ShowBriefHelp();
                return ExitCode.UserError;
            }
            if (opts.Inputs.Count > 1) {
                diags.ReportError("Too many input manifests specified.");
                Program.ShowBriefHelp();
                return ExitCode.UserError;
            }

            string manifest = opts.Inputs[0];
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
            EventManifest manifest = LoadManifest(manifestFile);
            if (manifest == null)
                return false;

            return Generate(manifest);
        }

        private EventManifest LoadManifest(string inputManifest)
        {
            if (!File.Exists(inputManifest)) {
                diags.ReportError("No such file '{0}'.", inputManifest);
                return null;
            }

            var parser = EventManifestParser.CreateWithWinmeta(
                diags, opts.SchemaPath, opts.WinmetaPath);

            return parser?.ParseManifest(inputManifest);
        }

        private bool Generate(EventManifest manifest)
        {
            AssignMessageIds(manifest);

            if (diags.ErrorOccurred)
                return false;

            WriteEventTemplate(manifest);
            WriteCode(manifest);
            WriteMessageTables(manifest);
            WriteResourceFile(manifest);

            return !diags.ErrorOccurred;
        }

        private void WriteEventTemplate(EventManifest manifest)
        {
            FileStream output = TryCreateFile(opts.EventTemplateFile, 4096,
                                              FileOptions.RandomAccess);
            if (output == null)
                return;

            using (output)
            using (var writer = new EventTemplateWriter(output))
                writer.Write(manifest.Providers);
        }

        private void WriteCode(EventManifest manifest)
        {
            ICodeGenerator codeGen = SelectCodeGenerator();
            if (codeGen == null)
                return;

            using (var output = TryCreateFile(opts.CodeHeaderFile)) {
                if (output == null)
                    return;
                codeGen.Generate(manifest, output);
            }
        }

        private ICodeGenerator SelectCodeGenerator()
        {
            var codeGenerators = container
                .GetExports<ICodeGenerator, ICodeGeneratorMetadata>().ToList();

            ICodeGenerator codeGen = null;
            foreach (var cg in codeGenerators) {
                if (cg.Metadata.Name == opts.CodeGenerator)
                    codeGen = cg.Value;
            }

            if (opts.CodeGenerator != null && codeGen == null) {
                diags.ReportError("No code generator found with name '{0}'.", opts.CodeGenerator);
                diags.Report(
                    DiagnosticSeverity.Note,
                    "Available generators: {0}",
                    string.Join(", ", codeGenerators.Select(g => g.Metadata.Name)));
                return null;
            }

            return codeGen;
        }

        private void WriteMessageTables(EventManifest manifest)
        {
            foreach (var resourceSet in manifest.Resources) {
                string fileName = AddCultureName(opts.MessageTableFile, resourceSet.Culture);
                FileStream output = TryCreateFile(fileName);
                if (output == null)
                    continue;

                using (output)
                using (var writer = new MessageTableWriter(output))
                    writer.Write(resourceSet.Strings.Select(CreateMessage), diags);
            }
        }

        private void WriteResourceFile(EventManifest manifest)
        {
            FileStream output = TryCreateFile(opts.ResourceFile);
            if (output == null)
                return;

            using (output)
            using (var writer = IO.CreateStreamWriter(output)) {
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
            }
        }

        private FileStream TryCreateFile(string fileName)
        {
            try {
                return File.Create(fileName);
            } catch (Exception ex) {
                diags.ReportError("Failed to create '{0}': {1}", fileName, ex.Message);
                return null;
            }
        }

        private FileStream TryCreateFile(
            string fileName, int bufferSize, FileOptions options)
        {
            try {
                return File.Create(fileName, bufferSize, options);
            } catch (Exception ex) {
                diags.ReportError("Failed to create '{0}': {1}", fileName, ex.Message);
                return null;
            }
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

        private void AssignMessageIds(EventManifest manifest)
        {
            foreach (var provider in manifest.Providers) {
                var msgIdGen = msgIdGenFactory();
                if (NeedsId(provider.Message))
                    provider.Message.Id = msgIdGen.CreateId(provider);

                foreach (var obj in provider.Channels.Where(e => NeedsId(e.Message)))
                    obj.Message.Id = msgIdGen.CreateId(obj, provider);
                foreach (var obj in provider.Levels.Where(e => NeedsId(e.Message)))
                    obj.Message.Id = msgIdGen.CreateId(obj, provider);
                foreach (var obj in provider.Tasks.Where(e => NeedsId(e.Message)))
                    obj.Message.Id = msgIdGen.CreateId(obj, provider);
                foreach (var obj in provider.Opcodes.Where(e => NeedsId(e.Message)))
                    obj.Message.Id = msgIdGen.CreateId(obj, provider);
                foreach (var obj in provider.Keywords.Where(e => NeedsId(e.Message)))
                    obj.Message.Id = msgIdGen.CreateId(obj, provider);
                foreach (var obj in provider.Events.Where(e => NeedsId(e.Message)))
                    obj.Message.Id = msgIdGen.CreateId(obj, provider);
                foreach (var map in provider.Maps)
                    foreach (var item in map.Items.Where(e => NeedsId(e.Message)))
                        item.Message.Id = msgIdGen.CreateId(item, map, provider);
                foreach (var obj in provider.Filters.Where(e => NeedsId(e.Message)))
                    obj.Message.Id = msgIdGen.CreateId(obj, provider);
            }

            var primaryResourceSet = manifest.PrimaryResourceSet;
            if (primaryResourceSet == null)
                return;

            foreach (var @string in primaryResourceSet.Strings.Used()) {
                foreach (var resourceSet in manifest.Resources) {
                    if (resourceSet == manifest.PrimaryResourceSet)
                        continue;

                    if (resourceSet.Strings.GetByName(@string.Name) == null)
                        diags.Report(
                            DiagnosticSeverity.Warning,
                            resourceSet.Location,
                            "String table for culture '{0}' is missing string '{1}'.",
                            resourceSet.Culture.Name,
                            @string.Name);
                }
            }
        }

        private static bool NeedsId(LocalizedString message)
        {
            return message != null && message.Id == LocalizedString.UnusedId;
        }
    }
}
