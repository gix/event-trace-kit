namespace NOpt.Tests
{
    using Xunit;
    using Xunit.Extensions;

    public class OptTableBuilderTest
    {
        private readonly OptTableBuilder builder;

        public OptTableBuilderTest()
        {
            builder = new OptTableBuilder();
        }

        [Theory]
        [InlineData(1, "--", "name", null)]
        public void AddFlag(int id, string prefix, string name, string helpText)
        {
            var opt = builder.AddFlag(id, prefix, name, helpText).CreateTable().GetOption(1);

            Assert.Equal(id, opt.Id);
            Assert.Equal(prefix, opt.Prefix);
            Assert.Equal(name, opt.Name);
            Assert.Equal(prefix + name, opt.PrefixedName);
            Assert.Equal(null, opt.Alias);
            //Assert.Equal(helpText, opt.HelpText);
        }
    }
}
