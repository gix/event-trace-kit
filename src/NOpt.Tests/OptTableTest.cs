namespace NOpt.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;
    using Xunit.Extensions;

    public class OptTableTest
    {
        private readonly ArgEqualityComparer argComparer = new ArgEqualityComparer();

        private sealed class TestOption : Option
        {
            public TestOption(OptSpecifier id, string name)
                : base(id, "-", name)
            {
            }
        }

        private sealed class LocalOptTable : OptTable
        {
            public LocalOptTable(IEnumerable<Option> options)
                : base(options)
            {
            }
        }

        [Fact]
        public void RejectsDuplicateOptionId()
        {
            var options = new[] {
                new TestOption(1, "a"),
                new TestOption(1, "b")
            };

            Assert.Throws<InvalidOptTableException>(() => new LocalOptTable(options));
        }

        [Fact]
        public void RejectsDuplicateUnknownOption()
        {
            var optInfos = new[] {
                new UnknownOption(1), 
                new UnknownOption(2)
            };

            Assert.Throws<InvalidOptTableException>(() => new LocalOptTable(optInfos));
        }

        [Fact]
        public void RejectsDuplicateInputOption()
        {
            var optInfos = new[] {
                new InputOption(1),
                new InputOption(2)
            };

            Assert.Throws<InvalidOptTableException>(() => new LocalOptTable(optInfos));
        }

        [Fact]
        public void RejectsDuplicateOptionName()
        {
            var optInfos = new[] {
                new TestOption(1, "opt"),
                new TestOption(2, "opt")
            };

            Assert.Throws<InvalidOptTableException>(() => new LocalOptTable(optInfos));
        }

        [Theory]
        [InlineData("-", "a", "-a")]
        [InlineData("-", "1", "-1")]
        [InlineData("-", "xyz123", "-xyz123")]
        [InlineData("--", "a", "--a")]
        [InlineData("--", "1", "--1")]
        [InlineData("--", "xyz123", "--xyz123")]
        [InlineData("/", "a", "/a")]
        [InlineData("/", "1", "/1")]
        [InlineData("/", "xyz123", "/xyz123")]
        public void ParseFlag(string prefix, string name, string arg)
        {
            OptTable optTable = new OptTableBuilder()
                .AddFlag(1, prefix, name)
                .CreateTable();
            var args = new[] { arg };

            MissingArgs missing;
            IArgumentList al = optTable.ParseArgs(args, out missing);

            Assert.Equal(0, missing.ArgIndex);
            Assert.Equal(0, missing.ArgCount);
            Assert.Equal(1, al.Count);
            Assert.Equal(optTable.MakeArg(1, arg, 0), al[0], argComparer);
        }

        [Fact]
        public void ParseFlagMultiple()
        {
            OptTable optTable = new OptTableBuilder()
                .AddFlag(1, "-", "aaaa")
                .AddFlag(2, new[] { "-", "--" }, "aaa")
                .AddFlag(3, "-", "a")
                .AddFlag(4, "--", "aa")
                .CreateTable();
            var args = new[] { "--aaa", "--aa", "-aaaa", "-a", "-aaa" };

            MissingArgs missing;
            IArgumentList al = optTable.ParseArgs(args, out missing);

            Assert.Equal(0, missing.ArgIndex);
            Assert.Equal(0, missing.ArgCount);
            Assert.Equal(5, al.Count);
            Assert.Equal(optTable.MakeArg(2, "--aaa", 0), al[0], argComparer);
            Assert.Equal(optTable.MakeArg(4, "--aa", 1), al[1], argComparer);
            Assert.Equal(optTable.MakeArg(1, "-aaaa", 2), al[2], argComparer);
            Assert.Equal(optTable.MakeArg(3, "-a", 3), al[3], argComparer);
            Assert.Equal(optTable.MakeArg(2, "-aaa", 4), al[4], argComparer);
        }

        [Fact]
        public void ParseFlagMultiplePrefixes()
        {
            OptTable optTable = new OptTableBuilder()
                .AddFlag(1, new[] { "-", "--", "/" }, "opt")
                .CreateTable();
            var args = new[] { "--opt", "/opt", "-opt" };

            MissingArgs missing;
            IArgumentList al = optTable.ParseArgs(args, out missing);

            Assert.Equal(0, missing.ArgIndex);
            Assert.Equal(0, missing.ArgCount);
            Assert.Equal(3, al.Count);
            Assert.Equal(optTable.MakeArg(1, "--opt", 0), al[0], argComparer);
            Assert.Equal(optTable.MakeArg(1, "/opt", 1), al[1], argComparer);
            Assert.Equal(optTable.MakeArg(1, "-opt", 2), al[2], argComparer);
        }

        [Theory]
        [InlineData("-", "a", "a")]
        [InlineData("-", "a", "-b")]
        [InlineData("-", "a", "--a")]
        [InlineData("-", "a", "/a")]
        [InlineData("--", "a", "a")]
        [InlineData("--", "a", "--b")]
        [InlineData("--", "a", "-a")]
        [InlineData("--", "a", "/a")]
        [InlineData("/", "a", "a")]
        [InlineData("/", "a", "-a")]
        [InlineData("/", "a", "--a")]
        [InlineData("/", "a", "/b")]
        public void ParseFlagUnknown(string prefix, string name, string arg)
        {
            OptTable optTable = new OptTableBuilder()
                .AddUnknown(1)
                .AddFlag(2, prefix, name)
                .CreateTable();
            var args = new[] { arg };

            MissingArgs missing;
            IArgumentList al = optTable.ParseArgs(args, out missing);

            Assert.Equal(0, missing.ArgIndex);
            Assert.Equal(0, missing.ArgCount);
            Assert.Equal(1, al.Count);
            Assert.Equal(optTable.MakeArg(1, arg, 0, arg), al[0], argComparer);
        }

        [Theory]
        [InlineData("-", "opt=", "")]
        [InlineData("--", "opt=", "1")]
        [InlineData("/", "opt=", "xyz 123")]
        [InlineData("-", "opt:", "")]
        [InlineData("--", "opt:", "1")]
        [InlineData("/", "opt:", "xyz 123")]
        [InlineData("-", "O", "")]
        [InlineData("--", "O", "1")]
        [InlineData("/", "O", "xyz 123")]
        public void ParseJoined(string prefix, string name, string arg)
        {
            OptTable optTable = new OptTableBuilder()
                .AddJoined(1, prefix, name)
                .CreateTable();
            var args = new[] { prefix + name + arg };

            MissingArgs missing;
            IArgumentList al = optTable.ParseArgs(args, out missing);

            Assert.Equal(0, missing.ArgIndex);
            Assert.Equal(0, missing.ArgCount);
            Assert.Equal(1, al.Count);
            Assert.Equal(optTable.MakeArg(1, prefix + name, 0, arg), al[0], argComparer);
        }

        [Fact]
        public void ParseJoinedMultiplePrefixes()
        {
            OptTable optTable = new OptTableBuilder()
                .AddJoined(1, new[] { "-", "--", "/" }, "opt=")
                .CreateTable();
            var args = new[] { "--opt=value1", "/opt=value2", "-opt=value3" };

            MissingArgs missing;
            IArgumentList al = optTable.ParseArgs(args, out missing);

            Assert.Equal(0, missing.ArgIndex);
            Assert.Equal(0, missing.ArgCount);
            Assert.Equal(3, al.Count);
            Assert.Equal(optTable.MakeArg(1, "--opt=", 0, "value1"), al[0], argComparer);
            Assert.Equal(optTable.MakeArg(1, "/opt=", 1, "value2"), al[1], argComparer);
            Assert.Equal(optTable.MakeArg(1, "-opt=", 2, "value3"), al[2], argComparer);
        }

        [Fact]
        public void ParseJoinedMultiple()
        {
            OptTable optTable = new OptTableBuilder()
                .AddJoined(1, "-", "aaaa")
                .AddJoined(2, new[] { "-", "--" }, "aaa")
                .AddJoined(3, "-", "a")
                .AddJoined(4, "--", "aa")
                .CreateTable();
            var args = new[] { "--aaa1", "--aa2", "-aaaaa", "-a4", "-aaa5" };

            MissingArgs missing;
            IArgumentList al = optTable.ParseArgs(args, out missing);

            Assert.Equal(0, missing.ArgIndex);
            Assert.Equal(0, missing.ArgCount);
            Assert.Equal(5, al.Count);
            Assert.Equal(optTable.MakeArg(2, "--aaa", 0, "1"), al[0], argComparer);
            Assert.Equal(optTable.MakeArg(4, "--aa", 1, "2"), al[1], argComparer);
            Assert.Equal(optTable.MakeArg(1, "-aaaa", 2, "a"), al[2], argComparer);
            Assert.Equal(optTable.MakeArg(3, "-a", 3, "4"), al[3], argComparer);
            Assert.Equal(optTable.MakeArg(2, "-aaa", 4, "5"), al[4], argComparer);
        }

        [Theory]
        [InlineData("-", "a", "a=value")]
        [InlineData("-", "a", "-b=value")]
        [InlineData("-", "a", "--a=value")]
        [InlineData("-", "a", "/a=value")]
        [InlineData("--", "a", "a=value")]
        [InlineData("--", "a", "--b=value")]
        [InlineData("--", "a", "-a=value")]
        [InlineData("--", "a", "/a=value")]
        [InlineData("/", "a", "a=value")]
        [InlineData("/", "a", "-a=value")]
        [InlineData("/", "a", "--a=value")]
        [InlineData("/", "a", "/b=value")]
        public void ParseJoinedUnknown(string prefix, string name, string arg)
        {
            OptTable optTable = new OptTableBuilder()
                .AddUnknown(1)
                .AddFlag(2, prefix, name)
                .CreateTable();
            var args = new[] { arg };

            MissingArgs missing;
            IArgumentList al = optTable.ParseArgs(args, out missing);

            Assert.Equal(0, missing.ArgIndex);
            Assert.Equal(0, missing.ArgCount);
            Assert.Equal(1, al.Count);
            Assert.Equal(optTable.MakeArg(1, arg, 0, arg), al[0], argComparer);
        }

        [Theory]
        [InlineData("-", "opt", "abc")]
        [InlineData("--", "opt", "1")]
        [InlineData("/", "opt", "xyz 123")]
        [InlineData("-", "opt", "abc")]
        [InlineData("--", "opt", "1")]
        [InlineData("/", "opt", "xyz 123")]
        [InlineData("-", "O", "abc")]
        [InlineData("--", "O", "1")]
        [InlineData("/", "O", "xyz 123")]
        public void ParseSeparate(string prefix, string name, string arg)
        {
            OptTable optTable = new OptTableBuilder()
                .AddSeparate(1, prefix, name)
                .CreateTable();
            var args = new[] { prefix + name, arg };

            MissingArgs missing;
            IArgumentList al = optTable.ParseArgs(args, out missing);

            Assert.Equal(0, missing.ArgIndex);
            Assert.Equal(0, missing.ArgCount);
            Assert.Equal(1, al.Count);
            Assert.Equal(optTable.MakeArg(1, prefix + name, 0, arg), al[0], argComparer);
        }

        [Fact]
        public void ParseSeparateMissingArg()
        {
            OptTable optTable = new OptTableBuilder()
                .AddSeparate(1, "-", "opt")
                .CreateTable();
            var args = new[] { "-opt", "v1", "-opt" };

            MissingArgs missing;
            IArgumentList al = optTable.ParseArgs(args, out missing);

            Assert.Equal(2, missing.ArgIndex);
            Assert.Equal(1, missing.ArgCount);
            Assert.Equal(1, al.Count);
            Assert.Equal(optTable.MakeArg(1, "-opt", 0, "v1"), al[0], argComparer);
        }

        public static IEnumerable<object[]> ParseMultiArgData
        {
            get
            {
                yield return new object[] { "-", "opt1", new[] { "v1" } };
                yield return new object[] { "-", "opt2", new[] { "v1", "-", "v3" } };
                yield return new object[] { "--", "opt3", new[] { "v1", "v2", "v3", "v4", "v5", "v6", "v3" } };
                yield return new object[] { "/", "opt4", new[] { "v1" } };
            }
        }

        [Theory]
        [MemberData("ParseMultiArgData")]
        public void ParseMultiArg(string prefix, string name, string[] extraArgs)
        {
            OptTable optTable = new OptTableBuilder()
                .AddMultiArg(1, prefix, name, extraArgs.Length)
                .CreateTable();
            var args = new[] { prefix + name }.Concat(extraArgs).ToArray();

            MissingArgs missing;
            IArgumentList al = optTable.ParseArgs(args, out missing);

            Assert.Equal(0, missing.ArgIndex);
            Assert.Equal(0, missing.ArgCount);
            Assert.Equal(1, al.Count);
            Assert.Equal(optTable.MakeArg(1, prefix + name, 0, extraArgs), al[0], argComparer);
        }

        public static IEnumerable<object[]> ParseMultiArgMissingArgsData
        {
            get
            {
                yield return new object[] { "-", "opt1", 1, new string[0], new string[0] };
                yield return new object[] { "-", "opt1", 1, new[] { "unk1" }, new string[0] };
                yield return new object[] { "-", "opt1", 1, new[] { "unk1", "unk2" }, new string[0] };
                yield return new object[] { "-", "opt2", 2, new string[0], new string[0] };
                yield return new object[] { "-", "opt2", 2, new string[0], new[] { "v1" } };
                yield return new object[] { "-", "opt2", 2, new[] { "unk1" }, new string[0] };
                yield return new object[] { "-", "opt2", 2, new[] { "unk1" }, new[] { "v1" } };
                yield return new object[] { "/", "opt4", 4, new string[0], new string[0] };
                yield return new object[] { "/", "opt4", 4, new string[0], new[] { "v1", "v2" } };
                yield return new object[] { "/", "opt4", 4, new[] { "unk1", "unk2" }, new[] { "v1", "v2" } };
            }
        }

        [Theory]
        [MemberData("ParseMultiArgMissingArgsData")]
        public void ParseMultiArgMissingArgs(
            string prefix, string name, int argCount, string[] preArgs, string[] postArgs)
        {
            OptTable optTable = new OptTableBuilder()
                .AddUnknown(1)
                .AddMultiArg(2, prefix, name, argCount)
                .CreateTable();
            var args = preArgs.Concat(new[] { prefix + name }).Concat(postArgs).ToArray();

            MissingArgs missing;
            IArgumentList al = optTable.ParseArgs(args, out missing);

            Assert.Equal(preArgs.Length, missing.ArgIndex);
            Assert.Equal(argCount - postArgs.Length, missing.ArgCount);
            Assert.Equal(preArgs.Length, al.Count);
            Assert.True(al.All(a => a.Option.Id == 1));
        }

        [Theory]
        [InlineData("-", "")]
        [InlineData("--", "")]
        [InlineData("--", "args")]
        public void ParseRemainingArgs(string prefix, string name)
        {
            OptTable optTable = new OptTableBuilder()
                .AddRemainingArgs(1, prefix, name)
                .CreateTable();
            var args = new[] { prefix + name, prefix + name, "value", "-flag" };

            MissingArgs missing;
            IArgumentList al = optTable.ParseArgs(args, out missing);

            Assert.Equal(0, missing.ArgIndex);
            Assert.Equal(0, missing.ArgCount);
            Assert.Equal(1, al.Count);
            Assert.Equal(optTable.MakeArg(1, prefix + name, 0, new[] { prefix + name, "value", "-flag" }), al[0], argComparer);
        }

        [Fact]
        public void ParseMixed()
        {
            OptTable optTable = new OptTableBuilder()
                .AddFlag(1, "-", "a")
                .AddJoined(2, "-", "a=")
                .CreateTable();
            var args = new[] { "-a=123", "-a" };

            MissingArgs missing;
            IArgumentList al = optTable.ParseArgs(args, out missing);

            Assert.Equal(0, missing.ArgIndex);
            Assert.Equal(0, missing.ArgCount);
            Assert.Equal(2, al.Count);
            Assert.Equal(optTable.MakeArg(2, "-a=", 0, "123"), al[0], argComparer);
            Assert.Equal(optTable.MakeArg(1, "-a", 1), al[1], argComparer);
        }

        private sealed class StubOption : Option
        {
            public StubOption(
                OptSpecifier id, string prefix, string name, int argCount = 0)
                : base(id, prefix, name)
            {
                ArgCount = argCount;
                CreatedArgs = new List<Arg>();
            }

            public int ArgCount { get; private set; }
            public List<Arg> CreatedArgs { get; private set; }

            protected override Arg AcceptCore(IReadOnlyList<string> args, ref int argIndex, int argLen)
            {
                var values = args.Skip(argIndex).Take(ArgCount);
                var arg = new Arg(this, "spelling", argIndex++, values);
                CreatedArgs.Add(arg);
                return arg;
            }
        }

        [Theory]
        [InlineData("-", "", 1)]
        [InlineData("-", "opt", 1)]
        [InlineData("--", "opt", 1)]
        [InlineData("/", "opt", 1)]
        [InlineData("-", "opt2", 2)]
        [InlineData("--", "opt2", 3)]
        [InlineData("/", "opt2", 10)]
        public void ParseOption(string prefix, string name, int argCount)
        {
            var opt = new StubOption(1, prefix, name);
            OptTable optTable = new OptTableBuilder()
                .Add(opt)
                .CreateTable();
            var args = Enumerable.Repeat(prefix + name, argCount).ToList();

            MissingArgs missing;
            IArgumentList al = optTable.ParseArgs(args, out missing);

            Assert.Equal(new MissingArgs(), missing);
            Assert.Equal(args.Count, al.Count);
            Assert.Equal(opt.CreatedArgs, al);
        }
    }

    internal static class OptTableExtensions
    {
        public static Arg MakeArg(
            this OptTable optTable, int id, string spelling, int index, string value = null)
        {
            Option option = optTable.GetOption(id);
            if (option is JoinedOption) // FIXME
                value = value ?? string.Empty;
            if (value != null)
                return new Arg(option, spelling, index, value);
            return new Arg(option, spelling, index);
        }

        public static Arg MakeArg(
            this OptTable optTable, int id, string spelling, int index, params string[] values)
        {
            values = values ?? new string[1];

            Option option = optTable.GetOption(id);
            if (option is JoinedOption) // FIXME
                values[0] = values[0] ?? string.Empty;
            if (values.Length > 1)
                return new Arg(option, spelling, index, values);
            if (values[0] != null)
                return new Arg(option, spelling, index, values[0]);
            return new Arg(option, spelling, index);
        }
    }

    public class ArgEqualityComparer : IEqualityComparer<Arg>
    {
        public bool Equals(Arg x, Arg y)
        {
            return
                x.IsClaimed == y.IsClaimed &&
                x.Index == y.Index &&
                x.Spelling == y.Spelling &&
                x.Option.Id == y.Option.Id &&
                x.Values.SequenceEqual(y.Values);
        }

        public int GetHashCode(Arg obj)
        {
            return 0;
        }
    }
}
