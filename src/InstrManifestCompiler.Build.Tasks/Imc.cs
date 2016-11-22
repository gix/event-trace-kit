namespace InstrManifestCompiler.Build.Tasks
{
    using System.Collections.Generic;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using NOpt;

    /// <summary>Event Manifest Compiler task.</summary>
    public sealed class Imc : NOptTrackedToolTask
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="Imc"/> class.
        /// </summary>
        public Imc()
            : base(new ImcOptTable())
        {
            OptionOrder = new List<OptSpecifier> {
                Opt.out_eq,
                Opt.header_file_eq,
                Opt.source_file_eq,
                Opt.msg_file_eq,
                Opt.wevt_file_eq,
                Opt.rc_file_eq,
                Opt.schema_eq,
                Opt.winmeta_eq,
                Opt.res, Opt.no_res,
                Opt.code, Opt.no_code,
                Opt.Ggenerator_eq,
                Opt.Glog_ns_eq,
                Opt.Getw_ns_eq,
                Opt.Glog_prefix_eq,
                Opt.Gcustom_enabled_checks, Opt.Gno_custom_enabled_checks,
                Opt.Gskip_defines, Opt.Gno_skip_defines,
                Opt.Gstubs, Opt.Gno_stubs,
                Opt.Galways_inline_attr_eq,
                Opt.Gnoinline_attr_eq,
                Opt.Input,
            };
        }

        /// <inheritdoc/>
        protected override string ToolName => ImcToolPath ?? "imc.exe";

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

        protected override ExecutableType? ToolType => ExecutableType.Managed32Bit;

        public string ImcToolPath { get; set; }

        /// <summary>Gets or sets the input event manifest.</summary>
        [Required]
        public ITaskItem Source
        {
            get { return GetTaskItem(Opt.Input); }
            set { SetTaskItem(Opt.Input, value); }
        }

        public string EventmanPath
        {
            get { return GetString(Opt.schema_eq); }
            set { SetString(Opt.schema_eq, value); }
        }

        public string WinmetaPath
        {
            get { return GetString(Opt.winmeta_eq); }
            set { SetString(Opt.winmeta_eq, value); }
        }

        public string OutputBaseName
        {
            get { return GetString(Opt.out_eq); }
            set { SetString(Opt.out_eq, value); }
        }

        public string HeaderFile
        {
            get { return GetString(Opt.header_file_eq); }
            set { SetString(Opt.header_file_eq, value); }
        }

        public string SourceFile
        {
            get { return GetString(Opt.source_file_eq); }
            set { SetString(Opt.source_file_eq, value); }
        }

        public string MessageTableFile
        {
            get { return GetString(Opt.msg_file_eq); }
            set { SetString(Opt.msg_file_eq, value); }
        }

        public string EventTemplateFile
        {
            get { return GetString(Opt.wevt_file_eq); }
            set { SetString(Opt.wevt_file_eq, value); }
        }

        public string ResourceFile
        {
            get { return GetString(Opt.rc_file_eq); }
            set { SetString(Opt.rc_file_eq, value); }
        }

        public bool GenerateResources
        {
            get { return GetBool(Opt.res, Opt.no_res); }
            set { SetBool(Opt.res, Opt.no_res, value); }
        }

        public bool GenerateCode
        {
            get { return GetBool(Opt.code, Opt.no_code); }
            set { SetBool(Opt.code, Opt.no_code, value); }
        }

        public string CodeGenerator
        {
            get { return GetString(Opt.Ggenerator_eq); }
            set { SetString(Opt.Ggenerator_eq, value); }
        }

        public string LogNamespace
        {
            get { return GetString(Opt.Glog_ns_eq); }
            set { SetString(Opt.Glog_ns_eq, value); }
        }

        public string EtwNamespace
        {
            get { return GetString(Opt.Getw_ns_eq); }
            set { SetString(Opt.Getw_ns_eq, value); }
        }

        public string LogCallPrefix
        {
            get { return GetString(Opt.Glog_prefix_eq); }
            set { SetString(Opt.Glog_prefix_eq, value); }
        }

        public bool UseCustomEnabledChecks
        {
            get { return GetBool(Opt.Gcustom_enabled_checks, Opt.Gno_custom_enabled_checks); }
            set { SetBool(Opt.Gcustom_enabled_checks, Opt.Gno_custom_enabled_checks, value); }
        }

        public bool SkipDefines
        {
            get { return GetBool(Opt.Gskip_defines, Opt.Gno_skip_defines); }
            set { SetBool(Opt.Gskip_defines, Opt.Gno_skip_defines, value); }
        }

        public bool GenerateStubs
        {
            get { return GetBool(Opt.Gstubs, Opt.Gno_stubs); }
            set { SetBool(Opt.Gstubs, Opt.Gno_stubs, value); }
        }

        public string AlwaysInlineAttribute
        {
            get { return GetString(Opt.Galways_inline_attr_eq); }
            set { SetString(Opt.Galways_inline_attr_eq, value); }
        }

        public string NoinlineAttribute
        {
            get { return GetString(Opt.Gnoinline_attr_eq); }
            set { SetString(Opt.Gnoinline_attr_eq, value); }
        }

        protected override string GenerateCommandLineCommands()
        {
            return base.GenerateResponseFileCommands();
        }

        protected override string GenerateResponseFileCommands()
        {
            return string.Empty;
        }

        protected override void PostProcessOptions()
        {
            // Work around a shortcoming in MSBuild because it cannot distinguish
            // between not-set and empty property values. We want to be able to
            // override the default log prefix with an empty string but the
            // normal property setter is never called in such a case. If the log
            // prefix option is not set we know that it should be interpreted as
            // an empty string because imc.props sets a non-empty default value
            // for LoggingMacroPrefix.
            if (!IsOptionSet(Opt.Glog_prefix_eq))
                LogCallPrefix = string.Empty;
        }
    }
}
