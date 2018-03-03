namespace EventManifestCompiler.Build.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization.Json;
    using EventManifestCompiler;
    using EventManifestFramework.Support;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using NOption;

    /// <summary>Event Manifest Compiler task.</summary>
    public sealed class InprocEmc : NOptionTrackedTask, IDiagnosticConsumer
    {
        private readonly DiagnosticsEngine diags;
        private readonly EmcOpts opts = new EmcOpts();

        private ITaskItem source;
        private ITaskItem[] generatedFiles;

        // Output
        private string outputBaseName;
        private string codeHeaderFile;
        private string codeSourceFile;
        private string messageTableFile;
        private string eventTemplateFile;

        // CodeGen
        private string codeGenerator;
        private string logNamespace;
        private string etwNamespace;
        private string logCallPrefix;
        private bool? useCustomEnabledChecks;
        private bool? skipDefines;
        private bool? generateLogStubs;
        private string alwaysInlineAttribute;
        private string noInlineAttribute;

        /// <summary>
        ///   Initializes a new instance of the <see cref="InprocEmc"/> class.
        /// </summary>
        public InprocEmc()
            : base(new EmcOptTable())
        {
            diags = new DiagnosticsEngine(this);

            OptionOrder = new List<OptSpecifier> {
                Opt.out_eq,
                Opt.header_file_eq,
                Opt.source_file_eq,
                Opt.msg_file_eq,
                Opt.wevt_file_eq,
                Opt.Gcustom_enabled_checks, Opt.Gno_custom_enabled_checks,
                Opt.Gskip_defines, Opt.Gno_skip_defines,
                Opt.Ggenerator_eq,
                Opt.Glog_ns_eq,
                Opt.Getw_ns_eq,
                Opt.Galways_inline_attr_eq,
                Opt.Gnoinline_attr_eq,
                Opt.Glog_prefix_eq,
                Opt.Input,
            };
        }

        /// <inheritdoc/>
        protected override string ActionName => "emc";

        /// <inheritdoc/>
        protected override List<OptSpecifier> OptionOrder { get; }

        /// <inheritdoc/>
        protected override ITaskItem[] TrackedInputFiles => new[] { Source };

        /// <inheritdoc/>
        protected override string[] ReadTLogNames =>
            new[] { "emc.read.1.tlog", "emc.*.read.1.tlog" };

        /// <inheritdoc/>
        protected override string[] WriteTLogNames =>
            new[] { "emc.write.1.tlog", "emc.*.write.1.tlog" };

        /// <inheritdoc/>
        protected override string CommandTLogName => "emc.command.1.tlog";

        /// <summary>Gets or sets the input event manifest.</summary>
        [Required]
        public ITaskItem Source
        {
            get => source;
            set => source = value;
        }

        public string OutputBaseName
        {
            get => outputBaseName;
            set => outputBaseName = value;
        }

        public string HeaderFile
        {
            get => codeHeaderFile;
            set => codeHeaderFile = value;
        }

        public string SourceFile
        {
            get => codeSourceFile;
            set => codeSourceFile = value;
        }

        public string MessageTableFile
        {
            get => messageTableFile;
            set => messageTableFile = value;
        }

        public string EventTemplateFile
        {
            get => eventTemplateFile;
            set => eventTemplateFile = value;
        }

        public string CodeGenerator
        {
            get => codeGenerator;
            set => codeGenerator = value;
        }

        public string LogNamespace
        {
            get => logNamespace;
            set => logNamespace = value;
        }

        public string EtwNamespace
        {
            get => etwNamespace;
            set => etwNamespace = value;
        }

        public string LogCallPrefix
        {
            get => logCallPrefix;
            set => logCallPrefix = value;
        }

        public bool UseCustomEnabledChecks
        {
            get => useCustomEnabledChecks.GetValueOrDefault();
            set => useCustomEnabledChecks = value;
        }

        public bool SkipDefines
        {
            get => skipDefines.GetValueOrDefault();
            set => skipDefines = value;
        }

        public bool GenerateLogStubs
        {
            get => generateLogStubs.GetValueOrDefault();
            set => generateLogStubs = value;
        }

        public string AlwaysInlineAttribute
        {
            get => alwaysInlineAttribute;
            set => alwaysInlineAttribute = value;
        }

        public string NoInlineAttribute
        {
            get => noInlineAttribute;
            set => noInlineAttribute = value;
        }

        protected override string GenerateOptions()
        {
            var dictionary = new SortedDictionary<string, string>();

            var opts = CreateOptions();
            dictionary.Add("OutputBaseName", opts.OutputBaseName);
            dictionary.Add("CodeHeaderFile", opts.CodeHeaderFile);
            dictionary.Add("CodeSourceFile", opts.CodeSourceFile);
            dictionary.Add("MessageTableFile", opts.MessageTableFile);
            dictionary.Add("EventTemplateFile", opts.EventTemplateFile);
            dictionary.Add("GenerateResources", opts.GenerateResources.ToString());
            dictionary.Add("GenerateCode", opts.GenerateCode.ToString());
            dictionary.Add("Generator", opts.CodeGenerator);
            dictionary.Add("LogNamespace", opts.LogNamespace);
            dictionary.Add("EtwNamespace", opts.EtwNamespace);
            dictionary.Add("LogCallPrefix", opts.LogCallPrefix);
            dictionary.Add("UseCustomEnabledChecks", opts.UseCustomEventEnabledChecks.ToString());
            dictionary.Add("SkipDefines", opts.SkipDefines.ToString());
            dictionary.Add("GenerateLogStubs", opts.GenerateStubs.ToString());
            dictionary.Add("AlwaysInlineAttribute", opts.AlwaysInlineAttribute);
            dictionary.Add("NoInlineAttribute", opts.NoInlineAttribute);

            using (var stream = new MemoryStream())
            using (var jw = JsonReaderWriterFactory.CreateJsonWriter(stream)) {
                foreach (var pair in dictionary)
                    jw.WriteElementString(pair.Key, pair.Value);
            }

            return string.Empty;
        }

        private EmcOpts CreateOptions()
        {
            return new EmcOpts {
                OutputBaseName = OutputBaseName,
                CodeHeaderFile = HeaderFile,
                CodeSourceFile = SourceFile,
                MessageTableFile = MessageTableFile,
                EventTemplateFile = EventTemplateFile,
                UseCustomEventEnabledChecks = UseCustomEnabledChecks,
                SkipDefines = SkipDefines,
                CodeGenerator = CodeGenerator,
                LogNamespace = LogNamespace,
                EtwNamespace = EtwNamespace,
                AlwaysInlineAttribute = AlwaysInlineAttribute,
                NoInlineAttribute = NoInlineAttribute,
                LogCallPrefix = LogCallPrefix,
            };
        }

        protected override bool ExecuteAction()
        {
            string input = Source.ItemSpec;
            if (input.Length == 0)
                return false;

            var action = new EmcAction(diags, opts);
            bool success = action.Execute() == ExitCode.Success;
            if (success) {
                generatedFiles = new ITaskItem[] {
                    new TaskItem(HeaderFile),
                    new TaskItem(SourceFile),
                    new TaskItem(MessageTableFile),
                    new TaskItem(EventTemplateFile)
                };
            }

            return success && !Log.HasLoggedErrors;
        }

        void IDiagnosticConsumer.HandleDiagnostic(
            DiagnosticSeverity severity, SourceLocation location, string message)
        {
            switch (severity) {
                case DiagnosticSeverity.Ignored:
                    break;
                case DiagnosticSeverity.Warning:
                    Log.LogWarning(
                        null, null, null, location.FilePath, location.LineNumber,
                        location.ColumnNumber, location.LineNumber,
                        location.ColumnNumber, message);
                    break;
                case DiagnosticSeverity.Error:
                    Log.LogError(
                        null, null, null, location.FilePath, location.LineNumber,
                        location.ColumnNumber, location.LineNumber,
                        location.ColumnNumber, message);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(severity));
            }
        }
    }
}
