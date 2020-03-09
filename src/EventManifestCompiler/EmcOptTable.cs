namespace EventManifestCompiler
{
    using System.Collections.Generic;
    using NOption;

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
        verify,
        res,
        no_res,
        code,
        no_code,

        // Output
        O_group,
        out_eq,
        h,
        header_file_eq,
        m,
        msg_file_eq,
        w,
        wevt_file_eq,
        r,
        rc_file_eq,

        // CodeGen
        G_group,
        generator_eq,
        g_eq,
        compat_eq,
        ext_eq,

        custom
    }

    internal sealed class EmcOptTable : OptTable
    {
        public EmcOptTable()
            : base(GetOptions())
        {
        }

        public static OptTableBuilder AddOptions(OptTableBuilder builder)
        {
            return builder
                .AddUnknown(Opt.Unknown)
                .AddInput(Opt.Input)
                .AddFlag(Opt.help, "-", "help", "Display available options")
                .AddFlag(Opt.QuestionMark, "-", "?", aliasId: Opt.help)
                .AddFlag(Opt.version, "-", "version", "Display version")
                .AddJoined(Opt.schema_eq, "-", "schema:", "Path to eventman.xsd", metaVar: "<path>")
                .AddJoined(Opt.winmeta_eq, "-", "winmeta:", "Path to winmeta.xml", metaVar: "<path>")
                .AddSeparate(Opt.dump_msg, "-", "dump-msg", "Dump message table", metaVar: "<file>")
                .AddSeparate(Opt.dump_wevt, "-", "dump-wevt", "Dump WEVT template", metaVar: "<file>")
                .AddSeparate(Opt.gen_manifest, "-", "gen-manifest", "Generate event manifest from binary provider and write to <file>", metaVar: "<file>")
                .AddFlag(Opt.verify, "-", "verify", "Only parse and check inputs.")
                .AddGroup(Opt.O_group, "<O group>", "Output")
                .AddJoined(Opt.out_eq, "-", "out:", "Base output filename", metaVar: "<file>", groupId: Opt.O_group)
                .AddFlag(Opt.res, "-", "res")
                .AddFlag(Opt.no_res, "-", "no-res", "Do not generate resources")
                .AddSeparate(Opt.m, "-", "m", metaVar: "<file>", groupId: Opt.O_group, aliasId: Opt.msg_file_eq)
                .AddSeparate(Opt.w, "-", "w", metaVar: "<file>", groupId: Opt.O_group, aliasId: Opt.wevt_file_eq)
                .AddSeparate(Opt.r, "-", "r", metaVar: "<file>", groupId: Opt.O_group, aliasId: Opt.rc_file_eq)
                .AddJoined(Opt.header_file_eq, "-", "header-file:", "Generated header filename", metaVar: "<file>", groupId: Opt.O_group)
                .AddJoined(Opt.msg_file_eq, "-", "msg-file:", "Write message table to <file>", metaVar: "<file>", groupId: Opt.O_group)
                .AddJoined(Opt.wevt_file_eq, "-", "wevt-file:", "Write ETW binary template to <file>", metaVar: "<file>", groupId: Opt.O_group)
                .AddJoined(Opt.rc_file_eq, "-", "rc-file:", "Write resource includes to <file>", metaVar: "<file>", groupId: Opt.O_group)
                .AddFlag(Opt.code, "-", "code")
                .AddFlag(Opt.no_code, "-", "no-code", "Do not generate logging code")
                .AddSeparate(Opt.h, "-", "h", metaVar: "<file>", groupId: Opt.O_group, aliasId: Opt.header_file_eq)
                .AddGroup(Opt.G_group, "<G group>", "CodeGen Options")
                .AddJoined(Opt.generator_eq, "-", "generator:", "Code generator to use (cxx, mc)", groupId: Opt.G_group)
                .AddJoined(Opt.g_eq, "-", "g:", aliasId: Opt.generator_eq)
                .AddJoined(Opt.compat_eq, "-", "compat:", "Binary compatibility with specified mc.exe version (Supported values: 8.1)", groupId: Opt.G_group, metaVar: "<version>")
                .AddJoined(Opt.ext_eq, "-", "ext:", "Assembly path to discover custom code generators", groupId: Opt.G_group, metaVar: "<path>");
        }

        private static IEnumerable<Option> GetOptions()
        {
            return AddOptions(new OptTableBuilder()).GetList();
        }
    }
}
