namespace NOpt
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using NOpt.Collections;
    using NOpt.Extensions;

    public struct MissingArgs
    {
        public MissingArgs(int argIndex, int argCount, Option option)
            : this()
        {
            ArgIndex = argIndex;
            ArgCount = argCount;
            Option = option;
        }

        public int ArgIndex { get; }
        public int ArgCount { get; }
        public Option Option { get; }

        public override string ToString()
        {
            if (Option == null)
                return "No args missing";
            return "Option " + Option.Id + " is missing " + ArgCount + " arg(s) after arg " + ArgIndex;
        }
    }

    public abstract class OptTable
    {
        private readonly HashSet<string> prefixes = new HashSet<string>();
        private readonly List<Option> options;
        private readonly OrderedDictionary<int, Option> optionMap = new OrderedDictionary<int, Option>();
        private readonly char[] prefixChars;
        private readonly int unknownOptionId;
        private readonly int inputOptionId;
        private readonly int firstOptionIdx;

        protected OptTable(IEnumerable<Option> options)
        {
            Contract.Requires<ArgumentNullException>(options != null);

            this.options = new List<Option>(options.Where(x => x != null));
            foreach (var option in this.options)
                option.InitializeOwner(this);
            VerifyOptions(this.options);

            foreach (var opt in this.options) {
                prefixes.AddRange(opt.Prefixes);
                optionMap.Add(opt.Id, opt);

                if (opt.Kind == OptionKind.Unknown)
                    unknownOptionId = opt.Id;
                else if (opt.Kind == OptionKind.Input)
                    inputOptionId = opt.Id;
            }

            this.options.StableSort(new OptionComparer());

            firstOptionIdx = 0;
            if (unknownOptionId != 0)
                ++firstOptionIdx;
            if (inputOptionId != 0)
                ++firstOptionIdx;

            var chars = new HashSet<char>();
            foreach (var prefix in prefixes)
                chars.AddRange(prefix);
            prefixChars = chars.ToArray();
        }

        /// <summary>
        ///   Gets the <see cref="Option"/> for the specified id.
        /// </summary>
        /// <param name="id">
        ///   The id of the option to get.
        /// </param>
        /// <returns>
        ///   The specified <see cref="Option"/> or <see langword="null"/> if no
        ///   such option exists.
        /// </returns>
        public Option GetOption(OptSpecifier id)
        {
            if (!id.IsValid)
                return null;
            if (!optionMap.TryGetValue(id.Id, out var opt))
                return null;
            return opt;
        }

        public void WriteHelp(IOptionHelpFormatter formatter)
        {
            Contract.Requires<ArgumentNullException>(formatter != null);

            formatter.WriteHelp(optionMap.Values);
        }

        public void WriteHelp(TextWriter writer)
        {
            Contract.Requires<ArgumentNullException>(writer != null);

            var formatter = new OptionHelpFormatter(writer, new WriteHelpSettings());
            WriteHelp(formatter);
        }

        public void WriteHelp(TextWriter writer, WriteHelpSettings settings)
        {
            Contract.Requires<ArgumentNullException>(writer != null);
            Contract.Requires<ArgumentNullException>(settings != null);

            var formatter = new OptionHelpFormatter(writer, settings);
            WriteHelp(formatter);
        }

        public IArgumentList ParseArgs(
            IReadOnlyList<string> args, out MissingArgs missing)
        {
            missing = new MissingArgs();

            var list = new ArgumentList();
            if (args == null)
                return list;

            for (int idx = 0; idx < args.Count;) {
                int prev = idx;
                Arg arg = ParseArg(args, ref idx, out int missingArgOptIndex);
                if (arg == null) {
                    missing = new MissingArgs(
                        prev,
                        idx - args.Count,
                        options[missingArgOptIndex]);
                    break;
                }

                list.Add(arg);
            }

            return list;
        }

        private Arg ParseArg(
            IReadOnlyList<string> args, ref int argIndex, out int missingArgOptIndex)
        {
            Contract.Requires(argIndex >= 0 && argIndex < args.Count);

            missingArgOptIndex = 0;

            string argStr = args[argIndex];
            if (argStr == null)
                return null;

            if (IsInputArg(argStr)) {
                int id = inputOptionId > 0 ? inputOptionId : unknownOptionId;
                return new Arg(GetOption(id), argStr, argIndex++, argStr);
            }

            string name = argStr.TrimStart(prefixChars);
            int firstInfo = options.WeakPredecessor(
                firstOptionIdx, options.Count - firstOptionIdx, name, CompareNameIgnoreCase);
            int prevArgIndex = argIndex;

            for (int idx = firstInfo; idx < options.Count; ++idx) {
                Option opt = options[idx];
                Arg arg = opt.Accept(args, ref argIndex);
                if (arg != null)
                    return arg;

                // Check if this option is missing values.
                if (prevArgIndex != argIndex) {
                    missingArgOptIndex = idx;
                    return null;
                }

                // Restore old argIndex.
                argIndex = prevArgIndex;
            }

            return new Arg(GetOption(unknownOptionId), argStr, argIndex++, argStr);
        }

        private static void VerifyOptions(IEnumerable<Option> options)
        {
            int unknownId = 0;
            int inputId = 0;
            int firstRealId = 0;
            var ids = new HashSet<int>();
            var names = new HashSet<string>();
            foreach (var option in options) {
                if (option.Id <= 0)
                    throw new InvalidOptTableException($"Option id '{option.Id}' must be greater than 0");

                if (option.Kind == OptionKind.Unknown) {
                    if (unknownId != 0)
                        throw new InvalidOptTableException("Duplicate option with OptionKind.Unknown");
                    unknownId = option.Id;
                } else if (option.Kind == OptionKind.Input) {
                    if (inputId != 0)
                        throw new InvalidOptTableException("Duplicate option with OptionKind.Input");
                    inputId = option.Id;
                } else if (firstRealId == 0)
                    firstRealId = option.Id;

                if (ids.Contains(option.Id))
                    throw new InvalidOptTableException($"Duplicate option id '{option.Id}'");
                ids.Add(option.Id);

                if (names.Contains(option.Name))
                    throw new InvalidOptTableException($"Duplicate option name '{option.Name}'");
                names.Add(option.Name);
            }

            //if (firstRealId < unknownId || firstRealId < inputId)
            //    throw new OptTableException("OptKind.Unknown and OptKind.Input must precede all other options.");
        }

        private static int CompareNameCase(string left, string right)
        {
            return StringUtils.CompareLongest(left, right, StringComparison.Ordinal);
        }

        private static int CompareNameIgnoreCase(string left, string right)
        {
            return StringUtils.CompareLongest(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareNameCase(Option opt, string name)
        {
            return CompareNameCase(opt.Name, name);
        }

        private static int CompareNameIgnoreCase(Option opt, string name)
        {
            return CompareNameIgnoreCase(opt.Name, name);
        }

        private bool IsInputArg(string argStr)
        {
            return prefixes.All(p => !argStr.StartsWith(p));
        }

        private sealed class OptionComparer : IComparer<Option>
        {
            public int Compare(Option x, Option y)
            {
                if (x.Kind != y.Kind) {
                    if (x.Kind == OptionKind.Unknown)
                        return -1;
                    if (y.Kind == OptionKind.Unknown)
                        return 1;
                    if (x.Kind == OptionKind.Input)
                        return -1;
                    if (y.Kind == OptionKind.Input)
                        return 1;
                }

                return CompareNameIgnoreCase(x.Name, y.Name);
            }
        }
    }
}
