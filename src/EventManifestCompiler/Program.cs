namespace EventManifestCompiler
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using EventManifestCompiler.Support;
    using EventTraceKit.EventTracing.Support;
    using NOption;

    public static class Program
    {
        public static int Main(string[] args)
        {
            try {
                return Run(args);
            } catch (UserException) when (!Debugger.IsAttached) {
                // Any errors should have been reported through diagnostics.
                return ExitCode.UserError;
            } catch (Exception ex) when (!Debugger.IsAttached) {
                WriteInternalError(ex, args);
                return ExitCode.InternalError;
            }
        }

        private static int Run(string[] args)
        {
            var diagPrinter = new ConsoleDiagnosticPrinter(Console.Error);
            var diags = new DiagnosticsEngine(diagPrinter);

            var opts = new EmcCommandLineArguments();
            var optTable = new EmcOptTable();
            if (!ParseOptions(args, optTable, opts, diags)) {
                ShowBriefHelp();
                return ExitCode.UserError;
            }

            // Show help and exit.
            if (opts.ShowHelp) {
                ShowHelp(optTable);
                return ExitCode.Success;
            }

            IAction action;
            if (opts.DumpEventTemplate != null ||
                opts.DumpMessageTable != null)
                action = new DumpAction(diags, opts);
            else if (opts.OutputManifest != null)
                action = new GenerateManifestFromProviderAction(diags, opts);
            else
                action = new CompileAction(diags, opts);

            return action.Execute();
        }

        private static bool ParseOptions(
            string[] cliArgs, OptTable optTable, EmcCommandLineArguments arguments, DiagnosticsEngine diags)
        {
            IArgumentList args = optTable.ParseArgs(cliArgs, out var missing);

            bool success = true;

            if (missing.ArgCount != 0) {
                diags.ReportError(
                    "Missing {1} argument{2} after: {0}",
                    cliArgs[missing.ArgIndex],
                    missing.ArgCount,
                    missing.ArgCount == 1 ? string.Empty : "s");
                success = false;
            }

            // Report errors on unknown arguments.
            foreach (var arg in args.Matching(Opt.Unknown)) {
                diags.ReportError("Unknown argument: {0}", arg.GetAsString());
                success = false;
            }

            arguments.ShowHelp = args.HasArg(Opt.help) || args.HasArg(Opt.QuestionMark);
            arguments.ShowVersion = args.HasArg(Opt.version);
            arguments.DumpMessageTable = args.GetLastArgValue(Opt.dump_msg);
            arguments.DumpEventTemplate = args.GetLastArgValue(Opt.dump_wevt);
            arguments.OutputManifest = args.GetLastArgValue(Opt.gen_manifest);
            arguments.Verify = args.GetFlag(Opt.verify);

            var opts = arguments.CompilationOptions;
            opts.Inputs = args.GetAllArgValues(Opt.Input).ToList();
            opts.OutputBaseName = args.GetLastArgValue(Opt.out_eq);
            opts.GenerateResources = args.GetFlag(Opt.res, Opt.no_res, true);
            opts.MessageTableFile = args.GetLastArgValue(Opt.msg_file_eq);
            opts.EventTemplateFile = args.GetLastArgValue(Opt.wevt_file_eq);
            opts.ResourceFile = args.GetLastArgValue(Opt.rc_file_eq);

            opts.CodeGenOptions.GenerateCode = args.GetFlag(Opt.code, Opt.no_code, true);
            opts.CodeGenOptions.CodeHeaderFile = args.GetLastArgValue(Opt.header_file_eq);
            opts.CodeGenOptions.CodeSourceFile = args.GetLastArgValue(Opt.source_file_eq);
            opts.CodeGenOptions.CodeGenerator = args.GetLastArgValue(Opt.Ggenerator_eq, opts.CodeGenOptions.CodeGenerator);
            opts.CodeGenOptions.LogNamespace = args.GetLastArgValue(Opt.Glog_ns_eq, opts.CodeGenOptions.LogNamespace);
            opts.CodeGenOptions.EtwNamespace = args.GetLastArgValue(Opt.Getw_ns_eq, opts.CodeGenOptions.EtwNamespace);
            opts.CodeGenOptions.LogCallPrefix = args.GetLastArgValue(Opt.Glog_prefix_eq, opts.CodeGenOptions.LogCallPrefix);
            opts.CodeGenOptions.UseCustomEventEnabledChecks = args.GetFlag(Opt.Gcustom_enabled_checks, Opt.Gno_custom_enabled_checks, false);
            opts.CodeGenOptions.SkipDefines = args.GetFlag(Opt.Gskip_defines, Opt.Gno_skip_defines, true);
            opts.CodeGenOptions.GenerateStubs = args.GetFlag(Opt.Gstubs, Opt.Gno_stubs, false);
            opts.CodeGenOptions.AlwaysInlineAttribute = args.GetLastArgValue(Opt.Galways_inline_attr_eq, opts.CodeGenOptions.AlwaysInlineAttribute);
            opts.CodeGenOptions.NoInlineAttribute = args.GetLastArgValue(Opt.Gnoinline_attr_eq, opts.CodeGenOptions.NoInlineAttribute);

            if (opts.Inputs.Count == 1)
                arguments.DecompilationOptions.InputModule = opts.Inputs[0];
            else if (opts.Inputs.Count == 2) {
                arguments.DecompilationOptions.InputEventTemplate = opts.Inputs[0];
                arguments.DecompilationOptions.InputMessageTable = opts.Inputs[1];
            }
            arguments.DecompilationOptions.OutputManifest = arguments.OutputManifest;

            opts.CompatibilityLevel = args.GetLastArgValue(Opt.Gcompat_eq, "10.0");

            opts.InferUnspecifiedOutputFiles();

            if (arguments.Verify) {
                opts.CodeGenOptions.GenerateCode = false;
                opts.GenerateResources = false;
            }

            return success;
        }

        private static void WriteInternalError(Exception ex, string[] args)
        {
            using (new ConsoleColorScope()) {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Error.Write("{0}: ", GetExeName());
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("internal error:");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Error.WriteLine("{0}:", ex.GetType().Name);
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.Error.WriteLine("--- stack trace ---");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Error.WriteLine("  {0}", ex.StackTrace);
                Console.Error.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.Error.WriteLine("--- args ---");
                Console.ForegroundColor = ConsoleColor.Gray;
                for (int i = 0; i < args.Length; ++i)
                    Console.Error.WriteLine("{0}: {1}", i, args[i]);
            }
        }

        private static string GetExeName()
        {
            try {
                return Path.GetFileName(Assembly.GetExecutingAssembly().Location);
            } catch (Exception) {
                return "<unknown>";
            }
        }

        internal static void ShowBriefHelp()
        {
            Console.WriteLine("Usage: {0} [options] <input.man>", GetExeName());
            Console.WriteLine("Try '{0} -help' for more options.", GetExeName());
        }

        private static void ShowHelp(OptTable opts)
        {
            Console.WriteLine("Usage: {0} [options] <input.man>", GetExeName());
            Console.WriteLine();
            var settings = new WriteHelpSettings();
            settings.MaxLineLength = Console.WindowWidth;
            opts.WriteHelp(Console.Out, settings);
        }
    }

    internal static class ExitCode
    {
        public const int InternalError = -1;
        public const int Success = 0;
        public const int Error = 1;
        public const int UserError = 2;
    }
}
