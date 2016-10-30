namespace InstrManifestCompiler.Build.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using InstrManifestCompiler.Support;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using NOpt;

    /// <summary>Event Manifest Compiler task.</summary>
    public sealed class InprocImc : NOptTrackedTask, IDiagnosticConsumer
    {
        private readonly DiagnosticsEngine diags;
        private readonly ImcOpts opts = new ImcOpts();

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
        ///   Initializes a new instance of the <see cref="InprocImc"/> class.
        /// </summary>
        public InprocImc()
            : base(new ImcOptTable())
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
        protected override string ActionName => "imc";

        /// <inheritdoc/>
        protected override List<OptSpecifier> OptionOrder { get; }

        /// <inheritdoc/>
        protected override ITaskItem[] TrackedInputFiles => new[] { Source };

        /// <inheritdoc/>
        protected override string[] ReadTLogNames =>
            new[] { "imc.read.1.tlog", "imc.*.read.1.tlog" };

        /// <inheritdoc/>
        protected override string[] WriteTLogNames =>
            new[] { "imc.write.1.tlog", "imc.*.write.1.tlog" };

        /// <inheritdoc/>
        protected override string CommandTLogName => "imc.command.1.tlog";

        /// <summary>Gets or sets the input event manifest.</summary>
        [Required]
        public ITaskItem Source
        {
            get { return source; }
            set { source = value; }
        }

        public string OutputBaseName
        {
            get { return outputBaseName; }
            set { outputBaseName = value; }
        }

        public string HeaderFile
        {
            get { return codeHeaderFile; }
            set { codeHeaderFile = value; }
        }

        public string SourceFile
        {
            get { return codeSourceFile; }
            set { codeSourceFile = value; }
        }

        public string MessageTableFile
        {
            get { return messageTableFile; }
            set { messageTableFile = value; }
        }

        public string EventTemplateFile
        {
            get { return eventTemplateFile; }
            set { eventTemplateFile = value; }
        }

        public string CodeGenerator
        {
            get { return codeGenerator; }
            set { codeGenerator = value; }
        }

        public string LogNamespace
        {
            get { return logNamespace; }
            set { logNamespace = value; }
        }

        public string EtwNamespace
        {
            get { return etwNamespace; }
            set { etwNamespace = value; }
        }

        public string LogCallPrefix
        {
            get { return logCallPrefix; }
            set { logCallPrefix = value; }
        }

        public bool UseCustomEnabledChecks
        {
            get { return useCustomEnabledChecks.GetValueOrDefault(); }
            set { useCustomEnabledChecks = value; }
        }

        public bool SkipDefines
        {
            get { return skipDefines.GetValueOrDefault(); }
            set { skipDefines = value; }
        }

        public bool GenerateLogStubs
        {
            get { return generateLogStubs.GetValueOrDefault(); }
            set { generateLogStubs = value; }
        }

        public string AlwaysInlineAttribute
        {
            get { return alwaysInlineAttribute; }
            set { alwaysInlineAttribute = value; }
        }

        public string NoInlineAttribute
        {
            get { return noInlineAttribute; }
            set { noInlineAttribute = value; }
        }

        protected override string GenerateOptions()
        {
            var builder = new StringBuilder();
            var dictionary = new SortedDictionary<string, string>();

            var opts = CreateOptions();
            dictionary.Add("OutputBaseName", opts.OutputBaseName);
            dictionary.Add("CodeHeaderFile", opts.CodeHeaderFile);
            dictionary.Add("CodeSourceFile", opts.CodeSourceFile);
            dictionary.Add("MessageTableFile", opts.MessageTableFile);
            dictionary.Add("EventTemplateFile", opts.EventTemplateFile);
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
                foreach (var pair in dictionary) {
                    jw.WriteElementString(pair.Key, pair.Value);
                }
            }

            return string.Empty;
        }

        private ImcOpts CreateOptions()
        {
            return new ImcOpts {
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

            var action = new ImcAction(diags, opts);
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
