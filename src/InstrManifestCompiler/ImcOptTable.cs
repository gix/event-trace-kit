namespace InstrManifestCompiler
{
    using System.Collections.Generic;
    using NOption;
    using NOption.Options;

    internal enum Opt
    {
        Unknown = 1,
        Input,
        help,
        QuestionMark,
        version,
        schema_eq,
        winmeta_eq,
        dump_msg,
        dump_wevt,
        gen_manifest,
        res,
        no_res,
        code,
        no_code,

        // Output
        O_group,
        out_eq,
        h,
        header_file_eq,
        s,
        source_file_eq,
        m,
        msg_file_eq,
        w,
        wevt_file_eq,
        r,
        rc_file_eq,

        // CodeGen
        G_group,
        Ggenerator_eq,
        Galways_inline_attr_eq,
        Gnoinline_attr_eq,
        Glog_prefix_eq,
        Gcustom_enabled_checks,
        Gno_custom_enabled_checks,
        Gskip_defines,
        Gno_skip_defines,
        Gstubs,
        Gno_stubs,
        Glog_ns_eq,
        Getw_ns_eq,
    }

    internal sealed class ImcOptTable : OptTable
    {
        public ImcOptTable()
            : base(GetOptions())
        {
        }

        private static IEnumerable<Option> GetOptions()
        {
            return new OptTableBuilder()
                .AddUnknown(Opt.Unknown)
                .AddInput(Opt.Input)
                .AddFlag(Opt.help, "-", "help", "Display available options")
                .AddFlag(Opt.QuestionMark, "-", "?", aliasId: Opt.help)
                .AddFlag(Opt.version, "-", "version", "Display version")
                .AddJoined(Opt.schema_eq, "-", "schema=", "Path to eventman.xsd", metaVar: "<path>")
                .AddJoined(Opt.winmeta_eq, "-", "winmeta=", "Path to winmeta.xml", metaVar: "<path>")
                .AddSeparate(Opt.dump_msg, "-", "dump-msg", "Dump message table", metaVar: "<file>")
                .AddSeparate(Opt.dump_wevt, "-", "dump-wevt", "Dump WEVT template", metaVar: "<file>")
                .AddSeparate(Opt.gen_manifest, "-", "gen-manifest", "Generate event manifest from binary provider and write to <file>", metaVar: "<file>")
                .AddGroup(Opt.O_group, "<O group>", "Output")
                .AddJoined(Opt.out_eq, "-", "out=", "Base output filename", metaVar: "<file>", groupId: Opt.O_group)
                .AddFlag(Opt.res, "-", "res")
                .AddFlag(Opt.no_res, "-", "no-res", "Do not generate resources")
                .AddSeparate(Opt.m, "-", "m", metaVar: "<file>", groupId: Opt.O_group, aliasId: Opt.msg_file_eq)
                .AddSeparate(Opt.w, "-", "w", metaVar: "<file>", groupId: Opt.O_group, aliasId: Opt.wevt_file_eq)
                .AddSeparate(Opt.r, "-", "r", metaVar: "<file>", groupId: Opt.O_group, aliasId: Opt.rc_file_eq)
                .AddJoined(Opt.header_file_eq, "-", "header-file=", "Generated header filename", metaVar: "<file>", groupId: Opt.O_group)
                //.AddJoined(Opt.source_file_eq, "-", "source-file=", "Generated source filename", metaVar: "<file>", groupId: Opt.O_group)
                .AddJoined(Opt.msg_file_eq, "-", "msg-file=", "Write message table to <file>", metaVar: "<file>", groupId: Opt.O_group)
                .AddJoined(Opt.wevt_file_eq, "-", "etwbin-file=", "Write ETW binary template to <file>", metaVar: "<file>", groupId: Opt.O_group)
                .AddJoined(Opt.rc_file_eq, "-", "rc-file=", "Write resource includes to <file>", metaVar: "<file>", groupId: Opt.O_group)
                .AddFlag(Opt.code, "-", "code")
                .AddFlag(Opt.no_code, "-", "no-code", "Do not generate logging code")
                .AddSeparate(Opt.h, "-", "h", metaVar: "<file>", groupId: Opt.O_group, aliasId: Opt.header_file_eq)
                .AddSeparate(Opt.s, "-", "s", metaVar: "<file>", groupId: Opt.O_group, aliasId: Opt.source_file_eq)
                .AddGroup(Opt.G_group, "<G group>", "CodeGen Options")
                .AddJoined(Opt.Ggenerator_eq, "-", "Ggenerator=", "Code generator to use (cxx, mc)", groupId: Opt.G_group)
                .AddJoined(Opt.Glog_prefix_eq, "-", "Glog-prefix=", "Log call prefix", groupId: Opt.G_group)
                .AddJoined(Opt.Glog_ns_eq, "-", "Glog-ns=", "Namespace where generated code is placed. Use '.' as separator (e.g. Company.Product.Tracing)", groupId: Opt.G_group)
                .AddJoined(Opt.Getw_ns_eq, "-", "Getw-ns=", "Namespace where common ETW code is placed. Use '.' as separator (e.g. Company.Product.ETW)", groupId: Opt.G_group)
                .AddFlag(Opt.Gcustom_enabled_checks, "-", "Gcustom-enabled-checks", "Use custom code to check whether events are enabled", groupId: Opt.G_group)
                .AddFlag(Opt.Gno_custom_enabled_checks, "-", "Gno-custom-enabled-checks", "Use EventEnabled() to check whether events are enabled", groupId: Opt.G_group)
                .AddFlag(Opt.Gskip_defines, "-", "Gskip-defines", "Skip code definitions for non-essential resources", groupId: Opt.G_group)
                .AddFlag(Opt.Gno_skip_defines, "-", "Gno-skip-defines", "Do not skip definitions", groupId: Opt.G_group)
                .AddFlag(Opt.Gstubs, "-", "Gstubs", "Generate logging functions as stubs", groupId: Opt.G_group)
                .AddFlag(Opt.Gno_stubs, "-", "Gno-stubs", groupId: Opt.G_group)
                .AddJoined(Opt.Galways_inline_attr_eq, "-", "Galways-inline-attr=", "String used to mark always-inline functions", groupId: Opt.G_group)
                .AddJoined(Opt.Gnoinline_attr_eq, "-", "Gnoinline-attr=", "String used to mark no-inline functions", groupId: Opt.G_group)
                .GetList();
        }
    }
}
