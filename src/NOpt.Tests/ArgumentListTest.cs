namespace NOpt.Tests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;
    using Xunit.Extensions;

    public class ArgumentListTest
    {
        private const int OptA = 2;
        private const int OptB = 3;
        private const int OptC = 4;
        private const int OptD = 5;
        private const int OptE = 6;
        private const int OptF = 7;
        private readonly OptTable optTable;

        public ArgumentListTest()
        {
            optTable = new OptTableBuilder()
                .AddFlag(OptA, "-", "a")
                .AddFlag(OptB, "-", "b")
                .AddJoined(OptC, "-", "c=")
                .AddJoined(OptD, "-", "d=")
                .AddFlag(OptE, "-", "e")
                .AddFlag(OptF, "-", "f")
                .CreateTable();
        }

        [Theory]
        [InlineData(typeof(IEnumerable))]
        [InlineData(typeof(IEnumerable<Arg>))]
        [InlineData(typeof(IReadOnlyCollection<Arg>))]
        [InlineData(typeof(IReadOnlyList<Arg>))]
        [InlineData(typeof(ICollection<Arg>))]
        [InlineData(typeof(IList<Arg>))]
        [InlineData(typeof(IArgumentList))]
        public void Implements(Type interfaceType)
        {
            Assert.True(typeof(ArgumentList).ImplementsInterface(interfaceType));
        }

        [Fact]
        public void get_Item()
        {
            var args = new[] {
                new Arg(optTable.GetOption(OptA), "-a", 0),
                new Arg(optTable.GetOption(OptB), "-b", 1),
                new Arg(optTable.GetOption(OptA), "-a", 2),
            };
            var al = CreateArgumentList(args);

            Assert.Same(args[0], al[0]);
            Assert.Same(args[1], al[1]);
            Assert.Same(args[2], al[2]);
        }

        [Theory]
        [InlineData(0, -1)]
        [InlineData(0, 0)]
        [InlineData(1, -1)]
        [InlineData(1, 1)]
        [InlineData(1, -1)]
        [InlineData(2, 2)]
        public void get_Item_Preconditions(int count, int index)
        {
            var al = CreateArgumentList();
            for (int i = 0; i < count; ++i)
                al.Add(new Arg(optTable.GetOption(OptA), "-a", 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => al[index]);
        }

        [Theory]
        [InlineData(0, -1)]
        [InlineData(0, 0)]
        [InlineData(1, -1)]
        [InlineData(1, 1)]
        [InlineData(1, -1)]
        [InlineData(2, 2)]
        public void set_Item_Preconditions(int count, int index)
        {
            var al = CreateArgumentList();
            for (int i = 0; i < count; ++i)
                al.Add(new Arg(optTable.GetOption(OptA), "-a", 0));

            var newArg = new Arg(optTable.GetOption(OptA), "-a", 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => al[index] = newArg);
        }

        [Fact]
        public void set_Item_Preconditions_Value()
        {
            var al = CreateArgumentList(new Arg(optTable.GetOption(OptA), "-a", 0));
            Assert.Throws<ArgumentNullException>(() => al[0] = null);
            Assert.Throws<ArgumentNullException>(() => ((IArgumentList)al)[0] = null);
            Assert.Throws<ArgumentNullException>(() => ((IList<Arg>)al)[0] = null);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(10)]
        public void Count(int count)
        {
            var al = CreateArgumentList();
            for (int i = 0; i < count; ++i)
                al.Add(new Arg(optTable.GetOption(OptA), "-a", 0));
            Assert.Equal(count, al.Count);
        }

        [Fact]
        public void Matching()
        {
            var args = new[] {
                new Arg(optTable.GetOption(OptA), "-a", 0),
                new Arg(optTable.GetOption(OptB), "-b", 1),
                new Arg(optTable.GetOption(OptA), "-a", 2),
            };
            var al = CreateArgumentList(args);

            var actual = al.Matching(OptA).ToArray();

            Assert.Same(args[0], actual[0]);
            Assert.Same(args[2], actual[1]);

            Assert.Equal(new Arg[0], al.Matching(OptC).ToArray());
        }

        [Fact]
        public void Add_Preconditions()
        {
            Assert.Throws<ArgumentNullException>(() => CreateArgumentList().Add(null));
            Assert.Throws<ArgumentNullException>(() => ((IArgumentList)CreateArgumentList()).Add(null));
            Assert.Throws<ArgumentNullException>(() => ((IList<Arg>)CreateArgumentList()).Add(null));
            Assert.Throws<ArgumentNullException>(() => ((ICollection<Arg>)CreateArgumentList()).Add(null));
        }

        [Fact]
        public void HasArg()
        {
            var al = CreateArgumentList(
                new Arg(optTable.GetOption(OptA), "-a", 0),
                new Arg(optTable.GetOption(OptB), "-b", 1),
                new Arg(optTable.GetOption(OptA), "-a", 2));

            Assert.True(al.HasArg(OptA));
            Assert.True(al[0].IsClaimed);
            Assert.False(al[1].IsClaimed);
            Assert.True(al[2].IsClaimed);

            Assert.True(al.HasArg(OptB));
            Assert.True(al[0].IsClaimed);
            Assert.True(al[1].IsClaimed);
            Assert.True(al[2].IsClaimed);

            Assert.False(al.HasArg(OptC));
            Assert.False(al.HasArg(-1));
            Assert.False(al.HasArg(1000));
            Assert.True(al[0].IsClaimed);
            Assert.True(al[1].IsClaimed);
            Assert.True(al[2].IsClaimed);
        }

        [Fact]
        public void HasArgNoClaim()
        {
            var al = CreateArgumentList(
                new Arg(optTable.GetOption(OptA), "-a", 0),
                new Arg(optTable.GetOption(OptB), "-b", 1),
                new Arg(optTable.GetOption(OptA), "-a", 2));

            Assert.True(al.HasArgNoClaim(OptA));
            Assert.False(al[0].IsClaimed);
            Assert.False(al[1].IsClaimed);
            Assert.False(al[2].IsClaimed);

            Assert.True(al.HasArgNoClaim(OptB));
            Assert.False(al[0].IsClaimed);
            Assert.False(al[1].IsClaimed);
            Assert.False(al[2].IsClaimed);

            Assert.False(al.HasArgNoClaim(OptC));
            Assert.False(al.HasArgNoClaim(-1));
            Assert.False(al.HasArgNoClaim(1000));
            Assert.False(al[0].IsClaimed);
            Assert.False(al[1].IsClaimed);
            Assert.False(al[2].IsClaimed);
        }

        public static IEnumerable<object[]> GetFlagData
        {
            get
            {
                var optTable = new OptTableBuilder()
                    .AddFlag(OptA, "-", "a")
                    .AddFlag(OptB, "-", "b")
                    .AddJoined(OptC, "-", "c=")
                    .AddJoined(OptD, "-", "d=")
                    .CreateTable();

                foreach (var defaultValue in new[] { true, false }) {
                    yield return new object[] {
                        true, defaultValue, new[] {
                            new Arg(optTable.GetOption(OptA), "-a", 0),
                        }
                    };
                    yield return new object[] {
                        false, defaultValue, new[] {
                            new Arg(optTable.GetOption(OptB), "-b", 0),
                        }
                    };
                    yield return new object[] {
                        false, defaultValue, new[] {
                            new Arg(optTable.GetOption(OptA), "-a", 0),
                            new Arg(optTable.GetOption(OptB), "-b", 1),
                        }
                    };
                    yield return new object[] {
                        true, defaultValue, new[] {
                            new Arg(optTable.GetOption(OptB), "-b", 1),
                            new Arg(optTable.GetOption(OptA), "-a", 0),
                        }
                    };
                    yield return new object[] {
                        true, defaultValue, new[] {
                            new Arg(optTable.GetOption(OptA), "-a", 0),
                            new Arg(optTable.GetOption(OptB), "-b", 1),
                            new Arg(optTable.GetOption(OptA), "-a", 2),
                        }
                    };
                }
            }
        }

        [Theory]
        [PropertyData("GetFlagData")]
        [InlineData(true, false, null)]
        public void GetFlag(bool expected, bool defaultValue, Arg[] args)
        {
            var al = CreateArgumentList(args ?? new Arg[0]);

            Assert.Equal(expected, al.GetFlag(OptA, OptB));
            Assert.True(al.All(a => a.IsClaimed));
        }

        [Theory]
        [PropertyData("GetFlagData")]
        [InlineData(true, true, null)]
        [InlineData(false, false, null)]
        public void GetFlagDefault(bool expected, bool defaultValue, Arg[] args)
        {
            var al = CreateArgumentList(args ?? new Arg[0]);

            Assert.Equal(expected, al.GetFlag(OptA, OptB, defaultValue));
            Assert.True(al.All(a => a.IsClaimed));
        }

        [Theory]
        [PropertyData("GetFlagData")]
        [InlineData(true, false, null)]
        public void GetFlagNoClaim(bool expected, bool defaultValue, Arg[] args)
        {
            var al = CreateArgumentList(args ?? new Arg[0]);

            Assert.Equal(expected, al.GetFlagNoClaim(OptA, OptB));
            Assert.True(al.All(a => !a.IsClaimed));
        }

        [Fact]
        public void GetLastArg()
        {
            var args = new[] {
                new Arg(optTable.GetOption(OptA), "-a", 0),
                new Arg(optTable.GetOption(OptB), "-b", 1),
                new Arg(optTable.GetOption(OptA), "-a", 2),
            };
            var al = CreateArgumentList(args);

            Assert.Same(args[2], al.GetLastArg(OptA));
            Assert.True(al[0].IsClaimed);
            Assert.False(al[1].IsClaimed);
            Assert.True(al[2].IsClaimed);

            Assert.Null(al.GetLastArg(OptC));
            Assert.True(al[0].IsClaimed);
            Assert.False(al[1].IsClaimed);
            Assert.True(al[2].IsClaimed);
        }

        [Fact]
        public void GetLastArg2()
        {
            var args = new[] {
                new Arg(optTable.GetOption(OptA), "-a", 0),
                new Arg(optTable.GetOption(OptB), "-b", 1),
                new Arg(optTable.GetOption(OptC), "-c=", 2, "2"),
                new Arg(optTable.GetOption(OptA), "-a", 4),
            };
            var al = CreateArgumentList(args);

            Assert.Null(al.GetLastArg(OptD, OptE));
            Assert.False(al[0].IsClaimed);
            Assert.False(al[1].IsClaimed);
            Assert.False(al[2].IsClaimed);
            Assert.False(al[3].IsClaimed);

            Assert.Same(args[3], al.GetLastArg(OptA, OptB));
            Assert.True(al[0].IsClaimed);
            Assert.True(al[1].IsClaimed);
            Assert.False(al[2].IsClaimed);
            Assert.True(al[3].IsClaimed);

            Assert.Same(args[2], al.GetLastArg(OptC, OptD));
            Assert.True(al[0].IsClaimed);
            Assert.True(al[1].IsClaimed);
            Assert.True(al[2].IsClaimed);
            Assert.True(al[3].IsClaimed);
        }

        [Fact]
        public void GetLastArgN()
        {
            var args = new[] {
                new Arg(optTable.GetOption(OptA), "-a", 0),
                new Arg(optTable.GetOption(OptB), "-b", 1),
                new Arg(optTable.GetOption(OptC), "-c=", 2, "2"),
                new Arg(optTable.GetOption(OptA), "-a", 4),
            };
            var al = CreateArgumentList(args);

            Assert.Null(al.GetLastArg(OptD, OptE, OptF));
            Assert.False(al[0].IsClaimed);
            Assert.False(al[1].IsClaimed);
            Assert.False(al[2].IsClaimed);
            Assert.False(al[3].IsClaimed);

            Assert.Same(args[3], al.GetLastArg(OptA, OptC, OptD));
            Assert.True(al[0].IsClaimed);
            Assert.False(al[1].IsClaimed);
            Assert.True(al[2].IsClaimed);
            Assert.True(al[3].IsClaimed);
        }

        [Fact]
        public void GetLastArgNoClaim()
        {
            var args = new[] {
                new Arg(optTable.GetOption(OptA), "-a", 0),
                new Arg(optTable.GetOption(OptB), "-b", 1),
                new Arg(optTable.GetOption(OptA), "-a", 2),
            };
            var al = CreateArgumentList(args);

            Assert.Same(args[2], al.GetLastArgNoClaim(OptA));
            Assert.False(al[0].IsClaimed);
            Assert.False(al[1].IsClaimed);
            Assert.False(al[2].IsClaimed);

            Assert.Null(al.GetLastArgNoClaim(OptC));
            Assert.False(al[0].IsClaimed);
            Assert.False(al[1].IsClaimed);
            Assert.False(al[2].IsClaimed);
        }

        [Fact]
        public void GetLastArgNoClaim2()
        {
            var args = new[] {
                new Arg(optTable.GetOption(OptA), "-a", 0),
                new Arg(optTable.GetOption(OptB), "-b", 1),
                new Arg(optTable.GetOption(OptC), "-c=", 2, "2"),
                new Arg(optTable.GetOption(OptA), "-a", 4),
            };
            var al = CreateArgumentList(args);

            Assert.Null(al.GetLastArgNoClaim(OptD, OptE));
            Assert.False(al[0].IsClaimed);
            Assert.False(al[1].IsClaimed);
            Assert.False(al[2].IsClaimed);
            Assert.False(al[3].IsClaimed);

            Assert.Same(args[3], al.GetLastArgNoClaim(OptA, OptB));
            Assert.False(al[0].IsClaimed);
            Assert.False(al[1].IsClaimed);
            Assert.False(al[2].IsClaimed);
            Assert.False(al[3].IsClaimed);

            Assert.Same(args[2], al.GetLastArgNoClaim(OptC, OptD));
            Assert.False(al[0].IsClaimed);
            Assert.False(al[1].IsClaimed);
            Assert.False(al[2].IsClaimed);
            Assert.False(al[3].IsClaimed);
        }

        [Fact]
        public void GetLastArgValue()
        {
            var args = new[] {
                new Arg(optTable.GetOption(OptA), "-a", 0),
                new Arg(optTable.GetOption(OptC), "-c=", 1, "2"),
                new Arg(optTable.GetOption(OptA), "-a", 4),
                new Arg(optTable.GetOption(OptC), "-c=", 5, "1"),
            };
            var al = CreateArgumentList(args);

            Assert.Equal("1", al.GetLastArgValue(OptC));
            Assert.False(al[0].IsClaimed);
            Assert.True(al[1].IsClaimed);
            Assert.False(al[2].IsClaimed);
            Assert.True(al[3].IsClaimed);

            Assert.Null(al.GetLastArgValue(OptA));
            Assert.Equal("def", al.GetLastArgValue(OptB, "def"));
        }

        [Theory]
        [InlineData(OptA, new string[0])]
        [InlineData(OptB, new string[0])]
        [InlineData(OptC, new[] { "xyz", "abc" })]
        [InlineData(OptD, new[] { "123" })]
        public void GetAllArgValues(int id, string[] expected)
        {
            var args = new[] {
                new Arg(optTable.GetOption(OptA), "-a", 0),
                new Arg(optTable.GetOption(OptC), "-c=", 1, "xyz"),
                new Arg(optTable.GetOption(OptA), "-a", 4),
                new Arg(optTable.GetOption(OptD), "-d=", 5, "123"),
                new Arg(optTable.GetOption(OptC), "-c=", 6, "abc"),
            };
            var al = CreateArgumentList(args);

            IList<string> actual = al.GetAllArgValues(id);

            Assert.Equal(expected, actual.AsEnumerable());
        }

        [Fact]
        public void ClaimAllArgs()
        {
            var args = new[] {
                new Arg(optTable.GetOption(OptA), "-a", 0),
                new Arg(optTable.GetOption(OptB), "-b", 1),
                new Arg(optTable.GetOption(OptA), "-a", 2),
            };
            var al = CreateArgumentList(args);

            al.ClaimAllArgs();

            Assert.Equal(args, al.AsEnumerable());
            Assert.True(al.All(a => a.IsClaimed));
        }

        [Fact]
        public void ClaimAllArgsMatching()
        {
            var args = new[] {
                new Arg(optTable.GetOption(OptA), "-a", 0),
                new Arg(optTable.GetOption(OptB), "-b", 1),
                new Arg(optTable.GetOption(OptA), "-a", 2),
            };
            var al = CreateArgumentList(args);

            al.ClaimAllArgs(OptC);
            Assert.Equal(args, al.AsEnumerable());
            Assert.True(al.All(a => !a.IsClaimed));

            al.ClaimAllArgs(OptA);
            Assert.Equal(args, al.AsEnumerable());
            Assert.True(al[0].IsClaimed);
            Assert.False(al[1].IsClaimed);
            Assert.True(al[2].IsClaimed);
        }

        [Fact]
        public void Clear()
        {
            var args = new[] {
                new Arg(optTable.GetOption(OptA), "-a", 0),
                new Arg(optTable.GetOption(OptB), "-b", 1),
                new Arg(optTable.GetOption(OptA), "-a", 2),
            };
            var al = CreateArgumentList(args);

            al.Clear();

            Assert.Equal(0, al.Count);
            Assert.Equal(new Arg[0], al.AsEnumerable());
        }

        [Fact]
        public void RemoveAllArgs()
        {
            var args = new[] {
                new Arg(optTable.GetOption(OptA), "-a", 0),
                new Arg(optTable.GetOption(OptB), "-b", 1),
                new Arg(optTable.GetOption(OptA), "-a", 2),
            };
            var al = CreateArgumentList(args);

            al.RemoveAllArgs(OptA);
            Assert.Equal(1, al.Count);
            Assert.Same(args[1], al[0]);

            al.RemoveAllArgs(OptD);
            Assert.Equal(1, al.Count);
            Assert.Same(args[1], al[0]);

            al.RemoveAllArgs(OptB);
            Assert.Equal(0, al.Count);
            Assert.Equal(new Arg[0], al.AsEnumerable());
        }

        private IArgumentList CreateArgumentList(params Arg[] args)
        {
            var al = new ArgumentList();
            foreach (var arg in args)
                al.Add(arg);
            return al;
        }
    }
}
