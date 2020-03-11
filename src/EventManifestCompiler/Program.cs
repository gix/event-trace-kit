namespace EventManifestCompiler
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using EventManifestCompiler.Support;
    using EventTraceKit.EventTracing.Compilation.CodeGen;
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
            OptTable optTable = new EmcOptTable();
            if (!ParseOptions(args, optTable, opts, diags)) {
                ShowBriefHelp();
                return ExitCode.UserError;
            }

            if (diags.ErrorOccurred)
                return ExitCode.UserError;

            // Show help and exit.
            if (opts.ShowHelp) {
                ShowHelp(optTable, opts.GeneratorOpts);
                return ExitCode.Success;
            }

            IAction action;
            if (opts.DumpEventTemplate != null ||
                opts.DumpMessageTable != null)
                action = new DumpAction(diags, opts);
            else if (opts.OutputManifest != null)
                action = new GenerateManifestFromProviderAction(diags, opts);
            else
                action = new CompileAction(diags, opts.CompilationOptions);

            return action.Execute();
        }

        private class CodeGeneratorInfo
        {
            private readonly List<OptionInfo> infos = new List<OptionInfo>();
            private readonly string name;
            private readonly object optionBag;

            public CodeGeneratorInfo(string name, ICodeGeneratorProvider provider)
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("Name must not be null, empty or whitespace.", nameof(name));
                if (provider is null)
                    throw new ArgumentNullException(nameof(provider));
                this.name = name;

                optionBag = provider.CreateOptions();
                OptTable = ReflectOptTable(optionBag?.GetType());
            }

            public OptTable OptTable { get; }

            public object ParseOptions(IReadOnlyList<string> args, ISet<int> claimedArgs, IDiagnostics diags)
            {
                if (OptTable is null)
                    return null;

                var argList = OptTable.ParseArgs(args, out var missing);

                foreach (var info in infos)
                    info.PopulateValue(optionBag, argList, diags);

                foreach (var arg in argList) {
                    if (arg.IsClaimed)
                        claimedArgs.Add(arg.Index);
                }

                return optionBag;
            }

            private OptTable ReflectOptTable(Type optionType)
            {
                if (optionType is null)
                    return null;

                int optIdx = 0;
                var groupId = Opt.custom + optIdx++;

                var builder = new OptTableBuilder()
                    .AddUnknown(Opt.Unknown)
                    .AddGroup(groupId, name, helpText: $"CodeGen Options (Generator {name})");

                foreach (var property in optionType.GetTypeInfo().DeclaredProperties) {
                    var optionAttribute = property.GetCustomAttributes<OptionAttribute>().FirstOrDefault();
                    if (optionAttribute is null)
                        continue;

                    var helpText = optionAttribute.HelpText ?? optionAttribute.Name;
                    var optionId = Opt.custom + optIdx++;

                    switch (optionAttribute) {
                        case JoinedOptionAttribute x:
                            builder.AddJoined(optionId, "-", $"c{optionAttribute.Name}:", helpText: helpText, groupId: groupId);
                            infos.Add(new JoinedOptionInfo(x, optionId, property));
                            break;
                        case FlagOptionAttribute x:
                            builder.AddFlag(optionId, "-", $"c{optionAttribute.Name}", helpText: helpText, groupId: groupId);
                            infos.Add(new FlagOptionInfo(x, optionId, property));
                            break;
                    }
                }

                return builder.CreateTable();
            }

            private sealed class JoinedOptionInfo : OptionInfo
            {
                private readonly JoinedOptionAttribute attribute;

                public JoinedOptionInfo(JoinedOptionAttribute attribute, OptSpecifier optionId, PropertyInfo property)
                    : base(optionId, property)
                {
                    this.attribute = attribute;
                }

                public override void PopulateValue(object target, IArgumentList args, IDiagnostics diags)
                {
                    var arg = args.GetLastArg(OptionId);
                    var valueString = arg?.Value ?? attribute.DefaultValue;
                    if (valueString is null)
                        return;

                    var converter = TypeDescriptor.GetConverter(Property.PropertyType);
                    if (!converter.CanConvertFrom(typeof(string))) {
                        diags.ReportError(
                            "Type converter for option '{0}' cannot convert from '{1}'.",
                            arg.Option.PrefixedName, typeof(string).Name);
                        return;
                    }

                    object value;
                    try {
                        value = converter.ConvertFromInvariantString(valueString);
                    } catch (NotSupportedException) {
                        diags.ReportError("Invalid value '{1}' in '{0}{1}'", arg.Spelling, valueString);
                        return;
                    }

                    Property.SetValue(target, value);
                }
            }

            private sealed class FlagOptionInfo : OptionInfo
            {
                private readonly FlagOptionAttribute attribute;

                public FlagOptionInfo(FlagOptionAttribute attribute, OptSpecifier optionId, PropertyInfo property)
                    : base(optionId, property)
                {
                    this.attribute = attribute;
                }

                public override void PopulateValue(object target, IArgumentList args, IDiagnostics diags)
                {
                    if (!args.HasArg(OptionId) && !attribute.HasDefaultValue)
                        return;

                    bool value = args.GetFlag(OptionId, attribute.DefaultValue);
                    Property.SetValue(target, value);
                }
            }

            private abstract class OptionInfo
            {
                protected OptionInfo(OptSpecifier optionId, PropertyInfo property)
                {
                    OptionId = optionId;
                    Property = property;
                }

                public OptSpecifier OptionId { get; }
                public PropertyInfo Property { get; }

                public abstract void PopulateValue(object target, IArgumentList args, IDiagnostics diags);
            }
        }

        private static bool ParseOptions(
            string[] cliArgs, OptTable optTable, EmcCommandLineArguments arguments, DiagnosticsEngine diags)
        {
            var catalog = new AggregateCatalog();
            catalog.Catalogs.Add(new AssemblyCatalog(Assembly.GetExecutingAssembly()));
            catalog.Catalogs.Add(new AssemblyCatalog(typeof(ICodeGeneratorProvider).Assembly));

            IArgumentList args = optTable.ParseArgs(cliArgs, out var missing);

            var extensions = args.GetAllArgValues(Opt.ext_eq);
            if (extensions.Count != 0) {
                foreach (var path in extensions) {
                    if (!File.Exists(path)) {
                        diags.Report(DiagnosticSeverity.Warning, "Ignoring extension assembly '{0}' because it does not exist.", path);
                        continue;
                    }
                    catalog.Catalogs.Add(new AssemblyCatalog(path));
                }
            }

            arguments.GeneratorOpts = new Dictionary<string, OptTable>();
            var codeGenerator = args.GetLastArgValue(Opt.generator_eq, arguments.CompilationOptions.CodeGenOptions.CodeGenerator);

            var container = new CompositionContainer(catalog);
            container.ComposeParts(diags);

            var codeGeneratorProviders = container.GetExportedValues<ICodeGeneratorProvider>().ToList();

            var claimedArgs = new HashSet<int>();
            object customCodeGenOpts = null;
            foreach (var provider in codeGeneratorProviders) {
                var info = new CodeGeneratorInfo(provider.Name, provider);
                if (info.OptTable != null)
                    arguments.GeneratorOpts[provider.Name] = info.OptTable;

                if (codeGenerator == provider.Name) {
                    customCodeGenOpts = info.ParseOptions(cliArgs, claimedArgs, diags);
                }
            }

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
                if (claimedArgs.Contains(arg.Index) || IsExtensionArgument(arg))
                    continue;

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
            opts.ResourceGenOnlyInputs = args.GetAllArgValues(Opt.resgen_manifest_eq).ToList();
            opts.OutputBaseName = args.GetLastArgValue(Opt.out_eq);
            opts.GenerateResources = args.GetFlag(Opt.res, Opt.no_res, true);
            opts.MessageTableFile = args.GetLastArgValue(Opt.msg_file_eq);
            opts.EventTemplateFile = args.GetLastArgValue(Opt.wevt_file_eq);
            opts.ResourceFile = args.GetLastArgValue(Opt.rc_file_eq);

            opts.CodeGenOptions.GenerateCode = args.GetFlag(Opt.code, Opt.no_code, true);
            opts.CodeGenOptions.CodeHeaderFile = args.GetLastArgValue(Opt.header_file_eq);
            opts.CodeGenOptions.CodeGenerator = codeGenerator;

            if (opts.Inputs.Count == 1)
                arguments.DecompilationOptions.InputModule = opts.Inputs[0];
            else if (opts.Inputs.Count == 2) {
                arguments.DecompilationOptions.InputEventTemplate = opts.Inputs[0];
                arguments.DecompilationOptions.InputMessageTable = opts.Inputs[1];
            }
            arguments.DecompilationOptions.OutputManifest = arguments.OutputManifest;

            var compatLevelString = args.GetLastArgValue(Opt.compat_eq, "10.0.16299");
            if (Version.TryParse(compatLevelString, out Version compatibilityLevel)) {
                opts.CompatibilityLevel = compatibilityLevel;
            } else {
                diags.ReportError("Invalid compatibility level: {0}", compatLevelString);
                success = false;
            }

            opts.InferUnspecifiedOutputFiles();

            if (arguments.Verify) {
                opts.CodeGenOptions.GenerateCode = false;
                opts.GenerateResources = false;
            }

            if (opts.CodeGenOptions.CodeGenerator != null) {
                var selectedCodeGen = codeGeneratorProviders.FirstOrDefault(x => x.Name == opts.CodeGenOptions.CodeGenerator);
                if (selectedCodeGen == null) {
                    diags.ReportError("No code generator found with name '{0}'.", opts.CodeGenOptions.CodeGenerator);
                    diags.Report(
                        DiagnosticSeverity.Note,
                        "Available generators: {0}",
                        string.Join(", ", codeGeneratorProviders.Select(x => x.Name)));
                    success = false;
                } else {
                    opts.CodeGenOptions.CodeGeneratorFactory = () => selectedCodeGen.CreateGenerator(customCodeGenOpts);
                }
            }

            return success;
        }

        private static bool IsExtensionArgument(Arg arg)
        {
            return arg.Spelling.StartsWith("-c");
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

        private static void ShowHelp(OptTable opts, IReadOnlyDictionary<string, OptTable> codeGenOpts)
        {
            Console.WriteLine("Usage: {0} [options] <input.man>", GetExeName());
            Console.WriteLine();
            var settings = new WriteHelpSettings();
            settings.MaxLineLength = Console.WindowWidth;
            opts.WriteHelp(Console.Out, settings);

            foreach (var pair in codeGenOpts.OrderBy(x => x.Key)) {
                Console.WriteLine();
                pair.Value.WriteHelp(Console.Out, settings);
            }
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
