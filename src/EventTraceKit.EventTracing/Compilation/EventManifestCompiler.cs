namespace EventTraceKit.EventTracing.Compilation
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using EventTraceKit.EventTracing;
    using EventTraceKit.EventTracing.Compilation.CodeGen;
    using EventTraceKit.EventTracing.Compilation.ResGen;
    using EventTraceKit.EventTracing.Compilation.Support;
    using EventTraceKit.EventTracing.Internal.Extensions;
    using EventTraceKit.EventTracing.Schema;
    using EventTraceKit.EventTracing.Support;

    [Export(typeof(ICodeGenOptions))]
    public sealed class CodeGenOptions : ICodeGenOptions
    {
        public CodeGenOptions()
        {
            GenerateCode = true;
            LogCallPrefix = "EventWrite";
            AlwaysInlineAttribute = "FORCEINLINE";
            NoInlineAttribute = "DECLSPEC_NOINLINE";
            UseCustomEventEnabledChecks = true;
            SkipDefines = false;
            EtwNamespace = "etw";
            CodeGenerator = "cxx";
        }

        public bool GenerateCode { get; set; }
        public string CodeHeaderFile { get; set; }
        public string CodeSourceFile { get; set; }
        public string CodeGenerator { get; set; }
        public string EtwNamespace { get; set; }
        public string LogNamespace { get; set; }
        public string LogCallPrefix { get; set; }
        public bool UseCustomEventEnabledChecks { get; set; }
        public bool SkipDefines { get; set; }
        public bool GenerateStubs { get; set; }
        public string AlwaysInlineAttribute { get; set; }
        public string NoInlineAttribute { get; set; }
    }

    public sealed class CompilationOptions
    {
        public CompilationOptions()
        {
            GenerateResources = true;
        }

        public List<string> Inputs { get; set; }
        public string WinmetaPath { get; set; }
        public string SchemaPath { get; set; }
        public string OutputBaseName { get; set; }

        public bool GenerateResources { get; set; }
        public string MessageTableFile { get; set; }
        public string EventTemplateFile { get; set; }
        public string ResourceFile { get; set; }

        public string CompatibilityLevel { get; set; }

        public CodeGenOptions CodeGenOptions { get; } = new CodeGenOptions();

        public void InferUnspecifiedOutputFiles(string baseName = null)
        {
            if (baseName == null)
                baseName = OutputBaseName;
            if (baseName == null && Inputs.Count > 0)
                baseName = Path.GetFileNameWithoutExtension(Inputs[0]);
            if (baseName == null)
                return;

            baseName = baseName.TrimStart(' ', '.');
            baseName = baseName.TrimEnd(' ', '.');

            if (CodeGenOptions.CodeHeaderFile == null)
                CodeGenOptions.CodeHeaderFile = baseName + ".h";
            if (CodeGenOptions.CodeSourceFile == null)
                CodeGenOptions.CodeSourceFile = baseName + ".cpp";
            if (MessageTableFile == null)
                MessageTableFile = baseName + ".msg.bin";
            if (EventTemplateFile == null)
                EventTemplateFile = baseName + ".wevt.bin";
            if (ResourceFile == null)
                ResourceFile = baseName + ".rc";
        }
    }

    public sealed class EventManifestCompiler
    {
        private readonly IDiagnosticsEngine diags;
        private readonly CompilationOptions opts;
        private readonly List<string> inputs;
        private readonly CompositionContainer container;
        private readonly Func<IMessageIdGenerator> msgIdGenFactory;

        public EventManifestCompiler(IDiagnosticsEngine diags, CompilationOptions opts, IEnumerable<string> inputs)
        {
            this.diags = diags ?? throw new ArgumentNullException(nameof(diags));
            this.opts = opts ?? throw new ArgumentNullException(nameof(opts));
            this.inputs = inputs?.ToList() ?? throw new ArgumentNullException(nameof(inputs));

            var catalog = new AggregateCatalog();
            catalog.Catalogs.Add(new AssemblyCatalog(Assembly.GetExecutingAssembly()));

            container = new CompositionContainer(catalog);
            container.ComposeParts(opts.CodeGenOptions, diags);

            msgIdGenFactory = () => new StableMessageIdGenerator(diags);
        }

        public bool Run()
        {
            if (inputs.Count == 0) {
                diags.ReportError("No input manifest specified.");
                return false;
            }

            if (inputs.Count > 1) {
                diags.ReportError("Too many input manifests specified.");
                return false;
            }

            string manifest = inputs[0];
            try {
                return ProcessManifest(manifest);
            } catch (SchemaValidationException ex) {
                var location = new SourceLocation(ex.BaseUri, ex.LineNumber, ex.ColumnNumber);
                diags.ReportError(location, ex.OriginalMessage);
                diags.ReportError("Input manifest '{0}' is invalid.", manifest);
                return false;
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
            MessageHelpers.AssignMessageIds(diags, manifest, msgIdGenFactory);

            if (diags.ErrorOccurred)
                return false;

            if (opts.GenerateResources) {
                WriteEventTemplate(manifest);
                WriteMessageTables(manifest);
                WriteResourceFile(manifest);
            }

            if (opts.CodeGenOptions.GenerateCode)
                WriteCode(manifest);

            return !diags.ErrorOccurred;
        }

        private void WriteEventTemplate(EventManifest manifest)
        {
            FileStream output = TryCreateFile(opts.EventTemplateFile, 4096,
                                              FileOptions.RandomAccess);
            if (output == null)
                return;

            using (output)
            using (var writer = new EventTemplateWriter(output)) {
                if (opts.CompatibilityLevel == "8.1")
                    writer.UseLegacyTemplateIds = true;
                writer.Write(manifest.Providers);
            }
        }

        private void WriteCode(EventManifest manifest)
        {
            ICodeGenerator codeGen = SelectCodeGenerator();
            if (codeGen == null)
                return;

            using (var output = TryCreateFile(opts.CodeGenOptions.CodeHeaderFile)) {
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
                if (cg.Metadata.Name == opts.CodeGenOptions.CodeGenerator)
                    codeGen = cg.Value;
            }

            if (opts.CodeGenOptions.CodeGenerator != null && codeGen == null) {
                diags.ReportError("No code generator found with name '{0}'.", opts.CodeGenOptions.CodeGenerator);
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
    }

    internal static class MessageHelpers
    {
        public static void AssignMessageIds(
            IDiagnostics diags, EventManifest manifest,
            Func<IMessageIdGenerator> generatorFactory)
        {
            foreach (var provider in manifest.Providers) {
                var msgIdGen = generatorFactory();
                if (NeedsId(provider.Message))
                    provider.Message.Id = msgIdGen.CreateId(provider);

                foreach (var obj in provider.Channels.Where(e => NeedsId(e.Message)))
                    obj.Message.Id = msgIdGen.CreateId(obj, provider);
                foreach (var obj in provider.Levels.Where(e => NeedsId(e.Message)))
                    obj.Message.Id = msgIdGen.CreateId(obj, provider);
                foreach (var obj in provider.Tasks.Where(e => NeedsId(e.Message)))
                    obj.Message.Id = msgIdGen.CreateId(obj, provider);
                foreach (var obj in provider.GetAllOpcodes().Where(e => NeedsId(e.Message)))
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

                    if (!resourceSet.ContainsName(@string.Name))
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
