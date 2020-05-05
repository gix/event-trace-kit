namespace EventManifestCompiler.Build.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using NOption;

    /// <summary>Event Manifest Compiler task.</summary>
    public sealed class Emc : NOptionTrackedToolTask
    {
        private enum ExtOpt
        {
            cuse_prefix = Opt.custom,
            cprefix_eq,
            cdefines,
            cno_defines,
            clog_ns_eq,
            cetw_ns_eq,
        }

        private class ExtendedOptTable : OptTable
        {
            public ExtendedOptTable()
                : base(GetOptions())
            {
            }

            private static IEnumerable<Option> GetOptions()
            {
                var builder = new OptTableBuilder();
                EmcOptTable.AddOptions(builder);
                builder
                    .AddFlag(ExtOpt.cuse_prefix, "-", "cuse-prefix", "Use a prefix for generated logging functions", groupId: Opt.G_group)
                    .AddJoined(ExtOpt.cprefix_eq, "-", "cprefix:", "Prefix for generated logging functions", groupId: Opt.G_group)
                    .AddJoined(ExtOpt.clog_ns_eq, "-", "clog-ns:", "Namespace where generated code is placed. Use '.' as separator (e.g. Company.Product.Tracing)", groupId: Opt.G_group)
                    .AddJoined(ExtOpt.cetw_ns_eq, "-", "cetw-ns:", "Namespace where common ETW code is placed. Use '.' as separator (e.g. Company.Product.ETW)", groupId: Opt.G_group)
                    .AddFlag(ExtOpt.cdefines, "-", "cdefines", "Generate code definitions for non-essential resources", groupId: Opt.G_group)
                    .AddFlag(ExtOpt.cno_defines, "-", "cno-defines", "Do not generate definitions", groupId: Opt.G_group);
                return builder.GetList();
            }
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="Emc"/> class.
        /// </summary>
        public Emc()
            : base(new ExtendedOptTable())
        {
            OptionOrder = new List<OptSpecifier> {
                Opt.out_eq,
                Opt.header_file_eq,
                Opt.msg_file_eq,
                Opt.wevt_file_eq,
                Opt.rc_file_eq,
                Opt.schema_eq,
                Opt.winmeta_eq,
                Opt.resgen_manifest_eq,
                Opt.res, Opt.no_res,
                Opt.code, Opt.no_code,
                Opt.ext_eq,
                Opt.generator_eq,
                ExtOpt.clog_ns_eq,
                ExtOpt.cetw_ns_eq,
                ExtOpt.cuse_prefix,
                ExtOpt.cprefix_eq,
                ExtOpt.cdefines, ExtOpt.cno_defines,
                Opt.Input,
            };
        }

        /// <inheritdoc/>
        protected override string ToolName => EmcToolPath ?? "emc.exe";

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

        protected override ExecutableType? ToolType => ExecutableType.Managed32Bit;

        public string EmcToolPath { get; set; }

        /// <summary>Gets or sets the input event manifest.</summary>
        public ITaskItem Source
        {
            get => GetTaskItem(Opt.Input);
            set => SetTaskItem(Opt.Input, value);
        }

        public ITaskItem[] ResourceGenOnlySources
        {
            get => GetTaskItemList(Opt.resgen_manifest_eq);
            set => SetTaskItemList(Opt.resgen_manifest_eq, value);
        }

        public string EventmanPath
        {
            get => GetString(Opt.schema_eq);
            set => SetString(Opt.schema_eq, value);
        }

        public string WinmetaPath
        {
            get => GetString(Opt.winmeta_eq);
            set => SetString(Opt.winmeta_eq, value);
        }

        public string OutputBaseName
        {
            get => GetString(Opt.out_eq);
            set => SetString(Opt.out_eq, value);
        }

        public string HeaderFile
        {
            get => GetString(Opt.header_file_eq);
            set => SetString(Opt.header_file_eq, value);
        }

        public string MessageTableFile
        {
            get => GetString(Opt.msg_file_eq);
            set => SetString(Opt.msg_file_eq, value);
        }

        public string EventTemplateFile
        {
            get => GetString(Opt.wevt_file_eq);
            set => SetString(Opt.wevt_file_eq, value);
        }

        public string ResourceFile
        {
            get => GetString(Opt.rc_file_eq);
            set => SetString(Opt.rc_file_eq, value);
        }

        public bool GenerateResources
        {
            get => GetBool(Opt.res, Opt.no_res);
            set => SetBool(Opt.res, Opt.no_res, value);
        }

        public bool GenerateCode
        {
            get => GetBool(Opt.code, Opt.no_code);
            set => SetBool(Opt.code, Opt.no_code, value);
        }

        public string CodeGenerator
        {
            get => GetString(Opt.generator_eq);
            set => SetString(Opt.generator_eq, value);
        }

        public string LogNamespace
        {
            get => GetString(ExtOpt.clog_ns_eq);
            set => SetString(ExtOpt.clog_ns_eq, value);
        }

        public string EtwNamespace
        {
            get => GetString(ExtOpt.cetw_ns_eq);
            set => SetString(ExtOpt.cetw_ns_eq, value);
        }

        public bool UseLoggingPrefix
        {
            get => GetBool(ExtOpt.cuse_prefix);
            set => SetBool(ExtOpt.cuse_prefix, value);
        }

        public string LoggingPrefix
        {
            get => GetString(ExtOpt.cprefix_eq);
            set => SetString(ExtOpt.cprefix_eq, value);
        }

        public bool GenerateDefines
        {
            get => GetBool(ExtOpt.cdefines, ExtOpt.cno_defines);
            set => SetBool(ExtOpt.cdefines, ExtOpt.cno_defines, value);
        }

        public string[] Extensions
        {
            get => GetStringList(Opt.ext_eq);
            set => SetStringList(Opt.ext_eq, value);
        }

        protected override string GenerateCommandLineCommands()
        {
            return base.GenerateResponseFileCommands();
        }

        protected override string GenerateResponseFileCommands()
        {
            return string.Empty;
        }
    }
}
