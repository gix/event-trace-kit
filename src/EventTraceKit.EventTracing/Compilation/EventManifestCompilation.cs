namespace EventTraceKit.EventTracing.Compilation
{
    using System;
    using System.Collections.Generic;
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
        private readonly List<EventManifest> resGenManifests = new List<EventManifest>();
        private readonly List<EventManifest> codeGenManifests = new List<EventManifest>();
        private readonly IDiagnostics diags;
        private readonly CompilationOptions opts;
        private readonly Func<IMessageIdGenerator> msgIdGenFactory;

        private EventManifest combinedCodeGenManifest;
        private EventManifest combinedResGenManifest;

        private EventManifestCompilation(IDiagnostics diags, CompilationOptions opts)
        {
            this.diags = diags ?? throw new ArgumentNullException(nameof(diags));
            this.opts = opts ?? throw new ArgumentNullException(nameof(opts));

            msgIdGenFactory = () => new StableMessageIdGenerator(diags);
        }

        public static EventManifestCompilation Create(
            IDiagnostics diags, CompilationOptions opts)
        {
            return new EventManifestCompilation(diags, opts);
        }

        private EventManifest CreateManifest(IEnumerable<EventManifest> manifests)
        {
            var combinedManifest = EventManifest.Combine(manifests);
            if (combinedManifest == null)
                return null;

            MessageHelpers.AssignMessageIds(diags, combinedManifest, msgIdGenFactory);
            return combinedManifest;
        }

        public bool AddManifests(IEnumerable<EventManifest> manifests)
        {
            if (manifests is null)
                throw new ArgumentNullException(nameof(manifests));

            var manifestList = manifests.ToList();
            return AddCodeGenManifests(manifestList) &&
                   AddResourceGenManifests(manifestList);
        }

        public bool AddManifests(params EventManifest[] manifests)
        {
            return AddManifests((IEnumerable<EventManifest>)manifests);
        }

        public bool AddCodeGenManifests(IEnumerable<EventManifest> manifests)
        {
            if (manifests is null)
                throw new ArgumentNullException(nameof(manifests));

            codeGenManifests.AddRange(manifests);

            var trap = diags.TrapError();
            var combinedManifest = CreateManifest(codeGenManifests);
            if (trap.ErrorOccurred)
                return false;

            combinedCodeGenManifest = combinedManifest;
            return true;
        }

        public bool AddCodeGenManifests(params EventManifest[] manifests)
        {
            return AddCodeGenManifests((IEnumerable<EventManifest>)manifests);
        }

        public bool AddResourceGenManifests(IEnumerable<EventManifest> manifests)
        {
            if (manifests is null)
                throw new ArgumentNullException(nameof(manifests));

            resGenManifests.AddRange(manifests);

            var trap = diags.TrapError();
            var combinedManifest = CreateManifest(resGenManifests);
            if (trap.ErrorOccurred)
                return false;

            combinedResGenManifest = combinedManifest;
            return true;
        }

        public bool AddResourceGenManifests(params EventManifest[] manifests)
        {
            return AddResourceGenManifests((IEnumerable<EventManifest>)manifests);
        }

        public bool Emit()
        {
            if (diags.ErrorOccurred)
                return false;

            if (opts.GenerateResources && combinedResGenManifest != null) {
                if (!EmitEventTemplate() || !EmitMessageTables() || !EmitResourceFile())
                    return false;
            }

            if (opts.CodeGenOptions.GenerateCode && combinedCodeGenManifest != null) {
                if (!EmitCode())
                    return false;
            }

            return !diags.ErrorOccurred;
        }

        public bool EmitCode()
        {
            if (combinedCodeGenManifest == null) {
                diags.ReportError("Compilation has no manifests for code generation.");
                return false;
            }

            var trap = diags.TrapError();

            ICodeGenerator codeGen = opts.CodeGenOptions.CodeGeneratorFactory?.Invoke();
            if (codeGen == null) {
                diags.ReportError("Failed to create the code generator instance.");
                return false;
            }

            using var output = FileUtilities.TryCreateFile(diags, opts.CodeGenOptions.CodeHeaderFile);
            if (output == null)
                return false;
            codeGen.Generate(combinedCodeGenManifest, output);

            return !trap.ErrorOccurred;
        }

        public bool EmitEventTemplate()
        {
            if (combinedResGenManifest == null) {
                diags.ReportError("Compilation has no manifests for resource generation.");
                return false;
            }

            var trap = diags.TrapError();

            FileStream output = FileUtilities.TryCreateFile(
                diags, opts.EventTemplateFile, 4096, FileOptions.RandomAccess);
            if (output == null)
                return false;

            using (output)
            using (var writer = new EventTemplateWriter(output)) {
                if (opts.CompatibilityLevel != null) {
                    if (opts.CompatibilityLevel <= new Version(8, 1))
                        writer.UseLegacyTemplateIds = true;
                    else if (opts.CompatibilityLevel < new Version(10, 0, 16299))
                        writer.Version = 3;
                }
                writer.Write(combinedResGenManifest.Providers);
            }

            return !trap.ErrorOccurred;
        }

        public bool EmitMessageTables()
        {
            if (combinedResGenManifest == null) {
                diags.ReportError("Compilation has no manifests for resource generation.");
                return false;
            }

            var trap = diags.TrapError();

            foreach (var resourceSet in combinedResGenManifest.Resources) {
                string fileName = AddCultureName(opts.MessageTableFile, resourceSet.Culture);
                using FileStream output = FileUtilities.TryCreateFile(diags, fileName);
                if (output == null)
                    continue;

                using var writer = new MessageTableWriter(output);
                writer.Write(resourceSet.Strings.Select(CreateMessage), diags);
            }

            return !trap.ErrorOccurred;
        }

        public bool EmitResourceFile()
        {
            if (combinedResGenManifest == null) {
                diags.ReportError("Compilation has no manifests for resource generation.");
                return false;
            }

            var trap = diags.TrapError();

            using FileStream output = FileUtilities.TryCreateFile(diags, opts.ResourceFile);
            if (output == null)
                return false;

            using var writer = IO.CreateStreamWriter(output);
            writer.NewLine = "\n";

            foreach (var resourceSet in combinedResGenManifest.Resources.OrderBy(ResourceSortKey)) {
                CultureInfo culture = resourceSet.Culture;
                string fileName = AddCultureName(opts.MessageTableFile, culture);
                int primaryLangId = culture.GetPrimaryLangId();
                int subLangId = culture.GetSubLangId();

                writer.WriteLine("LANGUAGE 0x{0:X},0x{1:X}", primaryLangId, subLangId);
                writer.WriteLine("1 11 \"{0}\"", EscapeCStringLiteral(fileName));
            }

            writer.WriteLine("1 WEVT_TEMPLATE \"{0}\"", EscapeCStringLiteral(opts.EventTemplateFile));

            return !trap.ErrorOccurred;
        }

        private static string EscapeCStringLiteral(string value)
        {
            return value.Replace("\\", "\\\\");
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
