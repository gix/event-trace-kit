namespace NOpt
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using NOpt.Collections;

    internal sealed class OptionHelpFormatter : IOptionHelpFormatter
    {
        private readonly TextWriter writer;
        private readonly WriteHelpSettings settings;

        public OptionHelpFormatter(TextWriter writer, WriteHelpSettings settings)
        {
            Contract.Requires<ArgumentNullException>(writer != null);
            Contract.Requires<ArgumentNullException>(settings != null);
            this.writer = writer;
            this.settings = settings;
        }

        public WriteHelpSettings Settings => settings;

        public void WriteHelp(IEnumerable<Option> options)
        {
            var groups = GroupOptions(options);
            int nameColumnWidth = GetMaxNameLength(groups);

            for (int i = 0; i < groups.Count; ++i) {
                if (i > 0)
                    writer.WriteLine();
                WriteGroup(groups[i], nameColumnWidth);
            }

            writer.Flush();
        }

        private sealed class OptionHelp
        {
            public OptionHelp(string name, string helpText)
            {
                Name = name;
                HelpText = helpText;
            }

            public string Name { get; private set; }
            public string HelpText { get; private set; }

            public void AddVariant(string name)
            {
                Name += ", " + name;
            }
        }

        private sealed class OptionGroup : List<OptionHelp>
        {
            public OptionGroup(string title)
            {
                Title = title;
            }

            public string Title { get; private set; }
        }

        private IList<OptionGroup> GroupOptions(IEnumerable<Option> options)
        {
            Contract.Ensures(Contract.Result<IList<OptionGroup>>() != null);

            var groups = new OrderedDictionary<string, OptionGroup>();
            var helpMap = new Dictionary<Option, OptionHelp>();

            foreach (var opt in options) {
                if (opt.Kind == OptionKind.Group)
                    continue;

                int flags = opt.Flags;
                if (settings.FlagsToInclude != 0 && (flags & settings.FlagsToInclude) == 0)
                    continue;
                if ((flags & settings.FlagsToExclude) != 0)
                    continue;

                // Ignore options without help text or which are aliases of an
                // option without help text.
                Option mainOpt;
                if (opt.HelpText != null)
                    mainOpt = opt;
                else if ((mainOpt = opt.Alias) != null && mainOpt.HelpText != null) {
                    // Empty
                } else
                    continue;

                string title = GetGroupTitle(mainOpt) ?? settings.DefaultHelpGroup;
                if (!groups.ContainsKey(title))
                    groups.Add(title, new OptionGroup(title));

                string name = opt.GetHelpName(Settings.DefaultMetaVar);
                if (!helpMap.TryGetValue(mainOpt, out OptionHelp help)) {
                    helpMap[mainOpt] = help = new OptionHelp(name, mainOpt.HelpText);
                    groups[title].Add(help);
                } else
                    help.AddVariant(name);
            }

            return groups.Values;
        }

        private int GetMaxNameLength(IEnumerable<OptionGroup> optionGroups)
        {
            int maxLength = 0;
            foreach (var group in optionGroups) {
                foreach (var entry in group) {
                    // Skip titles.
                    if (entry.HelpText == null)
                        continue;

                    // Limit the amount of padding we are willing to give up for alignment.
                    int length = entry.Name.Length;
                    if (length <= settings.NameColumnWidth)
                        maxLength = Math.Max(maxLength, length);
                }
            }
            return maxLength;
        }

        private static string GetGroupTitle(Option opt)
        {
            Option group = opt.Group;
            if (group == null)
                return null;

            if (group.HelpText != null)
                return group.HelpText;

            return GetGroupTitle(group);
        }

        private void WriteGroup(OptionGroup @group, int nameColumnWidth)
        {
            writer.Write(group.Title);
            writer.WriteLine(":");

            foreach (OptionHelp entry in group) {
                string helpName = entry.Name;
                string helpText = entry.HelpText;

                int padding = nameColumnWidth - helpName.Length;
                writer.Write(settings.IndentChars);
                writer.Write(helpName);

                // Break on long option names.
                if (padding < 0) {
                    writer.WriteLine();
                    padding = nameColumnWidth + settings.IndentChars.Length;
                }

                int totalWidth = settings.IndentChars.Length + nameColumnWidth + 1 + helpText.Length;
                if (totalWidth <= settings.MaxLineLength) {
                    writer.Write(new string(' ', padding + 1));
                    writer.WriteLine(helpText);
                } else {
                    writer.Write(new string(' ', padding));
                    WriteTextBlock(helpText, nameColumnWidth);
                }
            }
        }

        private void WriteTextBlock(string text, int optionFieldWidth)
        {
            string[] words = text.Split(' ');
            int lineLength = optionFieldWidth;
            foreach (var word in words) {
                if (lineLength + word.Length + 1 > settings.MaxLineLength) {
                    writer.WriteLine();
                    writer.Write(settings.IndentChars);
                    writer.Write(new string(' ', optionFieldWidth));
                    lineLength = optionFieldWidth;
                }
                writer.Write(' ');
                writer.Write(word);
                lineLength += word.Length + 1;
            }
            writer.WriteLine();
        }
    }
}
