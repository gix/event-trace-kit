namespace NOpt.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using NOpt.Attributes;
    using Xunit;
    using Xunit.Extensions;

    public class CallbackTest
    {
        [Fact]
        public void FactMethodName()
        {
            bool? help = null;
            string output = null;

            var opts = new OptionSet {
                { "?|h|help", "displays this message", v => { help = true; } },
                { "o=|out=", "output base name", v => output = v },
            };

            var args = new[] { "-unknown", "-?", "-out=foo" };

            List<string> extra = opts.Parse(args);

            Assert.Equal(true, help);
            Assert.Equal("foo", output);
            Assert.Equal(new[] { "-unknown" }, extra.AsEnumerable());
        }
    }

    public class OptionSet : KeyedCollection<string, Option>
    {
        private const int UnknownId = 1;
        private const int InputId = 2;

        private readonly OptTableBuilder builder;
        private readonly Dictionary<int, Tuple<Action<string>>> actions =
            new Dictionary<int, Tuple<Action<string>>>();
        private int nextOptionId = 3;

        public OptionSet()
        {
            builder = new OptTableBuilder();
            builder.AddUnknown(UnknownId);
            builder.AddInput(InputId);
        }

        protected override string GetKeyForItem(Option item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            return item.Name;
        }

        public OptionSet Add(string prototype, string description, Action<string> action)
        {
            return Add(prototype, description, action, false);
        }

        public OptionSet Add(string prototype, string description, Action<string> action, bool hidden)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            string[] names = prototype.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            int? mainId = null;
            foreach (var name in names) {
                int id = nextOptionId++;

                if (name.EndsWith("="))
                    builder.AddJoined(id, "-", name, helpText: description, aliasId: mainId);
                else
                    builder.AddFlag(id, "-", name, helpText: description, aliasId: mainId);

                mainId = mainId ?? id;
                actions.Add(id, Tuple.Create(action));
            }

            //Option p = new ActionOption(prototype, description, 1,
            //        delegate(OptionValueCollection v) { action(v[0]); }, hidden);
            //base.Add(p);

            return this;
        }

        public List<string> Parse(IReadOnlyList<string> arguments)
        {
            var optTable = builder.CreateTable();

            MissingArgs missing;
            IArgumentList al = optTable.ParseArgs(arguments, out missing);

            foreach (var arg in al) {
                Tuple<Action<string>> tuple;
                if (!actions.TryGetValue(arg.Option.Id, out tuple))
                    continue;

                tuple.Item1(arg.Value);
            }

            return al.Matching(UnknownId).Select(a => a.Value).ToList();
        }
    }

    public class DeclTest
    {
        class Options
        {
            [InputOption(HelpText = "foo")]
            public string Input { get; set; }

            [FlagOption("-flag1", DefaultValue = false)]
            public bool Flag1 { get; set; }

            [FlagOption("-flag2", HelpText = "foo", DefaultValue = true)]
            public object Flag2 { get; set; }
        }

        [Theory]
        [InlineData("")]
        public void FlagOptions(string arg)
        {
            var args = new[] { "-flag1" };

            var options = new Options();
            var parser = new DeclarativeCommandLineParser(options);
            parser.Builder.AddFlag(100, "-", "flag3");
            parser.Parse(args);

            Assert.Equal(true, options.Flag1);
            Assert.Equal(true, options.Flag2);
        }
    }
}
