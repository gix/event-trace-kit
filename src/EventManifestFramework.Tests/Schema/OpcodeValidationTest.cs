namespace EventManifestFramework.Tests.Schema
{
    using System.Xml.Linq;
    using EventManifestFramework.Support;
    using Xunit;

    public class OpcodeValidationTest : ValidationTest
    {
        [Theory]
        [MemberData(nameof(ValidQNames))]
        public void Name_Valid(object name)
        {
            var opcode = E("opcode", A("name", name), A("value", 16));

            ParseInput(ref opcode);

            Assert.Empty(diags.Errors);
        }

        [Theory]
        [MemberData(nameof(InvalidQNames))]
        public void Name_Invalid(object name)
        {
            var opcode = E("opcode", A("name", name), A("value", 16));

            ParseInput(ref opcode);

            Assert.Single(diags.Errors);
            Assert.Equal(opcode.Attribute("name").GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void Name_Missing()
        {
            var opcode = E("opcode", A("value", 16));

            ParseInput(ref opcode);

            Assert.Single(diags.Errors);
            Assert.Equal(opcode.GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void Name_Duplicate()
        {
            var opcode1 = E("opcode", A("name", "Opcode1"), A("value", 16));
            var opcode2 = E("opcode", A("name", "Opcode1"), A("value", 17));

            ParseInput(ref opcode1, ref opcode2);

            Assert.Single(diags.Errors);
            Assert.Equal(opcode2.Attribute("name").GetLocation(), diags.Errors[0].Location);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(239)]
        [InlineData("0xA")]
        [InlineData("0xef")]
        public void Value_Valid(object value)
        {
            var opcode = E("opcode", A("name", "Opcode1"), A("value", value));

            ParseInput(ref opcode);

            Assert.Empty(diags.Errors);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(9)]
        [InlineData(240)]
        [InlineData(0x10000)]
        public void Value_Invalid(object value)
        {
            var opcode = E("opcode", A("name", "Opcode1"), A("value", value));

            ParseInput(ref opcode);

            Assert.Single(diags.Errors);
            Assert.Equal(opcode.Attribute("value").GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void Value_Missing()
        {
            var opcode = E("opcode", A("name", "Opcode1"));

            ParseInput(ref opcode);

            Assert.Single(diags.Errors);
            Assert.Equal(opcode.GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void Value_Duplicate()
        {
            var opcode1 = E("opcode", A("name", "Opcode1"), A("value", 16));
            var opcode2 = E("opcode", A("name", "Opcode2"), A("value", 16));

            ParseInput(ref opcode1, ref opcode2);

            Assert.Single(diags.Errors);
            Assert.Equal(opcode2.Attribute("value").GetLocation(), diags.Errors[0].Location);
        }

        [Theory]
        [MemberData(nameof(ValidSymbolNames))]
        public void Symbol_Valid(string symbol)
        {
            var opcode = E("opcode", A("name", "Opcode1"), A("value", 16), A("symbol", symbol));

            ParseInput(ref opcode);

            Assert.Empty(diags.Errors);
        }

        [Theory]
        [MemberData(nameof(InvalidSymbolNames))]
        public void Symbol_Invalid(string symbol)
        {
            var opcode = E("opcode", A("name", "Opcode1"), A("value", 16), A("symbol", symbol));

            ParseInput(ref opcode);

            Assert.Single(diags.Errors);
            Assert.Equal(opcode.Attribute("symbol").GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void Symbol_Duplicate()
        {
            var opcode1 = E("opcode", A("name", "Opcode1"), A("value", 16), A("symbol", "Sym1"));
            var opcode2 = E("opcode", A("name", "Opcode2"), A("value", 17), A("symbol", "Sym1"));

            ParseInput(ref opcode1, ref opcode2);

            Assert.Single(diags.Errors);
            Assert.Equal(opcode2.Attribute("symbol").GetLocation(), diags.Errors[0].Location);
        }

        [Theory]
        [MemberData(nameof(ValidMessageRefs))]
        public void Message_Valid(string message)
        {
            var opcode = E("opcode", A("name", "Opcode1"), A("value", 16), A("message", message));

            ParseInput(ref opcode);

            Assert.Empty(diags.Errors);
        }

        [Theory]
        [MemberData(nameof(InvalidMessageRefs))]
        public void Message_Invalid(string message)
        {
            var opcode = E("opcode", A("name", "Opcode1"), A("value", 16), A("message", message));

            ParseInput(ref opcode);

            Assert.Single(diags.Errors);
            Assert.Equal(opcode.Attribute("message").GetLocation(), diags.Errors[0].Location);
        }

        private void ParseInput(ref XElement elem1)
        {
            parser.ParseManifest(CreateInput("opcodes", ref elem1), "<stdin>");
        }

        private void ParseInput(ref XElement elem1, ref XElement elem2)
        {
            parser.ParseManifest(CreateInput("opcodes", ref elem1, ref elem2), "<stdin>");
        }
    }
}
