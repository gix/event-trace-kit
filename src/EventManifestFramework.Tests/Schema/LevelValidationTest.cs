namespace EventManifestFramework.Tests.Schema
{
    using System.Xml.Linq;
    using EventManifestFramework.Support;
    using Xunit;

    public class LevelValidationTest : ValidationTest
    {
        [Theory]
        [MemberData(nameof(ValidQNames))]
        public void Name_Valid(object name)
        {
            var level = E("level", A("name", name), A("value", 16));

            ParseInput(ref level);

            Assert.Empty(diags.Errors);
        }

        [Theory]
        [MemberData(nameof(InvalidQNames))]
        public void Name_Invalid(object name)
        {
            var level = E("level", A("name", name), A("value", 16));

            ParseInput(ref level);

            Assert.Single(diags.Errors);
            Assert.Equal(level.Attribute("name").GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void Name_Missing()
        {
            var level = E("level", A("value", 16));

            ParseInput(ref level);

            Assert.Single(diags.Errors);
            Assert.Equal(level.GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void Name_Duplicate()
        {
            var level1 = E("level", A("name", "Level1"), A("value", 16));
            var level2 = E("level", A("name", "Level1"), A("value", 17));

            ParseInput(ref level1, ref level2);

            Assert.Single(diags.Errors);
            Assert.Equal(level2.Attribute("name").GetLocation(), diags.Errors[0].Location);
        }

        [Theory]
        [InlineData(16)]
        [InlineData(100)]
        [InlineData(255)]
        [InlineData("0x10")]
        [InlineData("0xFF")]
        public void Value_Valid(object value)
        {
            var level = E("level", A("name", "Level1"), A("value", value));

            ParseInput(ref level);

            Assert.Empty(diags.Errors);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(14)]
        [InlineData(15)]
        [InlineData(256)]
        [InlineData(0x10000)]
        public void Value_Invalid(object value)
        {
            var level = E("level", A("name", "Level1"), A("value", value));

            ParseInput(ref level);

            Assert.Single(diags.Errors);
            Assert.Equal(level.Attribute("value").GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void Value_Missing()
        {
            var level = E("level", A("name", "Level1"));

            ParseInput(ref level);

            Assert.Single(diags.Errors);
            Assert.Equal(level.GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void Value_Duplicate()
        {
            var level1 = E("level", A("name", "Level1"), A("value", 16));
            var level2 = E("level", A("name", "Level2"), A("value", 16));

            ParseInput(ref level1, ref level2);

            Assert.Single(diags.Errors);
            Assert.Equal(level2.Attribute("value").GetLocation(), diags.Errors[0].Location);
        }

        [Theory]
        [MemberData(nameof(ValidSymbolNames))]
        public void Symbol_Valid(string symbol)
        {
            var level = E("level", A("name", "Level1"), A("value", 16), A("symbol", symbol));

            ParseInput(ref level);

            Assert.Empty(diags.Errors);
        }

        [Theory]
        [MemberData(nameof(InvalidSymbolNames))]
        public void Symbol_Invalid(string symbol)
        {
            var level = E("level", A("name", "Level1"), A("value", 16), A("symbol", symbol));

            ParseInput(ref level);

            Assert.Single(diags.Errors);
            Assert.Equal(level.Attribute("symbol").GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void Symbol_Duplicate()
        {
            var level1 = E("level", A("name", "Level1"), A("value", 16), A("symbol", "Sym1"));
            var level2 = E("level", A("name", "Level2"), A("value", 17), A("symbol", "Sym1"));

            ParseInput(ref level1, ref level2);

            Assert.Single(diags.Errors);
            Assert.Equal(level2.Attribute("symbol").GetLocation(), diags.Errors[0].Location);
        }

        [Theory]
        [MemberData(nameof(ValidMessageRefs))]
        public void Message_Valid(string message)
        {
            var level = E("level", A("name", "Level1"), A("value", 16), A("message", message));

            ParseInput(ref level);

            Assert.Empty(diags.Errors);
        }

        [Theory]
        [MemberData(nameof(InvalidMessageRefs))]
        public void Message_Invalid(string message)
        {
            var level = E("level", A("name", "Level1"), A("value", 16), A("message", message));

            ParseInput(ref level);

            Assert.Single(diags.Errors);
            Assert.Equal(level.Attribute("message").GetLocation(), diags.Errors[0].Location);
        }

        private void ParseInput(ref XElement elem1)
        {
            parser.ParseManifest(CreateInput("levels", ref elem1), "<stdin>");
        }

        private void ParseInput(ref XElement elem1, ref XElement elem2)
        {
            parser.ParseManifest(CreateInput("levels", ref elem1, ref elem2), "<stdin>");
        }
    }
}
