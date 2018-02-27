namespace NOpt.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    public class ArgumentTest
    {
        private readonly OptTableImpl optTable;

        private sealed class OptTableImpl : OptTable
        {
            public OptTableImpl()
                : base(GetOptions())
            {
            }

            private static IEnumerable<Option> GetOptions()
            {
                return new[] { new JoinedOption(1, "-", "opt1=") };
            }
        }

        public ArgumentTest()
        {
            optTable = new OptTableImpl();
        }

        [Fact]
        public void Properties()
        {
            var arg = new Arg(optTable.GetOption(1), "opt1=", 0, "value1");

            Assert.Equal(false, arg.IsClaimed);
            Assert.Equal(0, arg.Index);
            Assert.Equal("opt1=", arg.Spelling);
            Assert.Equal("value1", arg.Value);
            Assert.Equal(new[] { "value1" }, arg.Values.AsEnumerable());
        }

        [Fact]
        public void Claim()
        {
            var arg = new Arg(optTable.GetOption(1), "opt1=", 0, "value1");

            arg.Claim();

            Assert.Equal(true, arg.IsClaimed);
            Assert.Equal(0, arg.Index);
            Assert.Equal("opt1=", arg.Spelling);
            Assert.Equal("value1", arg.Value);
            Assert.Equal(new[] { "value1" }, arg.Values.AsEnumerable());
        }
    }
}
