namespace EventManifestFramework.Tests.Schema
{
    using System.Xml.Linq;
    using EventManifestFramework.Support;
    using Xunit;

    public class FilterValidationTest : ValidationTest
    {
        [Theory]
        [MemberData(nameof(ValidQNames))]
        public void Name_Valid(object name)
        {
            var filter = E("filter", A("name", name), A("value", 16));

            ParseInput(ref filter);

            Assert.Empty(diags.Errors);
        }

        [Theory]
        [MemberData(nameof(InvalidQNames))]
        public void Name_Invalid(object name)
        {
            var filter = E("filter", A("name", name), A("value", 16));

            ParseInput(ref filter);

            Assert.Single(diags.Errors);
            Assert.Equal(filter.Attribute("name").GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void Name_Missing()
        {
            var filter = E("filter", A("value", 16));

            ParseInput(ref filter);

            Assert.Single(diags.Errors);
            Assert.Equal(filter.GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void Name_Duplicate()
        {
            var filter1 = E("filter", A("name", "Filter1"), A("value", 16));
            var filter2 = E("filter", A("name", "Filter1"), A("value", 17));

            ParseInput(ref filter1, ref filter2);

            Assert.Single(diags.Errors);
            Assert.Equal(filter2.Attribute("name").GetLocation(), diags.Errors[0].Location);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(16)]
        [InlineData(100)]
        [InlineData(255)]
        [InlineData("0x10")]
        [InlineData("0xFF")]
        public void Value_Valid(object value)
        {
            var filter = E("filter", A("name", "Filter1"), A("value", value));

            ParseInput(ref filter);

            Assert.Empty(diags.Errors);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(256)]
        [InlineData(0x10000)]
        [InlineData("0x100")]
        public void Value_Invalid(object value)
        {
            var filter = E("filter", A("name", "Filter1"), A("value", value));

            ParseInput(ref filter);

            Assert.Single(diags.Errors);
            Assert.Equal(filter.Attribute("value").GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void Value_Missing()
        {
            var filter = E("filter", A("name", "Filter1"));

            ParseInput(ref filter);

            Assert.Single(diags.Errors);
            Assert.Equal(filter.GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void Value_Duplicate()
        {
            var filter1 = E("filter", A("name", "Filter1"), A("value", 16));
            var filter2 = E("filter", A("name", "Filter2"), A("value", 16));

            ParseInput(ref filter1, ref filter2);

            Assert.Single(diags.Errors);
            Assert.Equal(filter2.GetLocation(), diags.Errors[0].Location);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        [InlineData(0xFF)]
        [InlineData("0x10")]
        [InlineData("0xFF")]
        public void Version_Valid(object version)
        {
            var filter = E("filter", A("name", "Filter1"), A("value", 16), A("version", version));

            ParseInput(ref filter);

            Assert.Empty(diags.Errors);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0x10000)]
        [InlineData("0x10000")]
        [InlineData("foo")]
        public void Version_Invalid(object version)
        {
            var filter = E("filter", A("name", "Filter1"), A("value", 16), A("version", version));

            ParseInput(ref filter);

            Assert.Single(diags.Errors);
            Assert.Equal(filter.Attribute("version").GetLocation(), diags.Errors[0].Location);
        }

        [Theory]
        [InlineData(10, 0, 10, 1)] // Different version
        [InlineData(10, 0, 11, 0)] // Different value
        [InlineData(10, 2, 11, 1)] // Different value and version
        public void ValueVersion_Unique(int value1, int version1, int value2, int version2)
        {
            var filter1 = E("filter", A("name", "Filter1"), A("value", value1), A("version", version1));
            var filter2 = E("filter", A("name", "Filter2"), A("value", value2), A("version", version2));

            ParseInput(ref filter1, ref filter2);

            Assert.Empty(diags.Errors);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 0)]
        [InlineData(12, 34)]
        public void ValueVersion_Duplicate(int value, int version)
        {
            var filter1 = E("filter", A("name", "Filter1"), A("value", value), A("version", version));
            var filter2 = E("filter", A("name", "Filter2"), A("value", value), A("version", version));

            ParseInput(ref filter1, ref filter2);

            Assert.Single(diags.Errors);
            Assert.Equal(filter2.GetLocation(), diags.Errors[0].Location);
        }

        [Theory]
        [MemberData(nameof(ValidSymbolNames))]
        public void Symbol_Valid(string symbol)
        {
            var filter = E("filter", A("name", "Filter1"), A("value", 16), A("symbol", symbol));

            ParseInput(ref filter);

            Assert.Empty(diags.Errors);
        }

        [Theory]
        [MemberData(nameof(InvalidSymbolNames))]
        public void Symbol_Invalid(string symbol)
        {
            var filter = E("filter", A("name", "Filter1"), A("value", 16), A("symbol", symbol));

            ParseInput(ref filter);

            Assert.Single(diags.Errors);
            Assert.Equal(filter.Attribute("symbol").GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void Symbol_Duplicate()
        {
            var filter1 = E("filter", A("name", "Filter1"), A("value", 16), A("symbol", "Sym1"));
            var filter2 = E("filter", A("name", "Filter2"), A("value", 17), A("symbol", "Sym1"));

            ParseInput(ref filter1, ref filter2);

            Assert.Single(diags.Errors);
            Assert.Equal(filter2.Attribute("symbol").GetLocation(), diags.Errors[0].Location);
        }

        [Theory]
        [InlineData("TestTemplate1")]
        [InlineData("TestTemplate2")]
        public void TemplateId_Valid(string id)
        {
            var filter = E("filter", A("name", "Filter1"), A("value", 16), A("tid", id));

            ParseInput(ref filter);

            Assert.Empty(diags.Errors);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("Undefined")]
        public void TemplateId_Invalid(string id)
        {
            var filter = E("filter", A("name", "Filter1"), A("value", 16), A("tid", id));

            ParseInput(ref filter);

            Assert.Single(diags.Errors);
            Assert.Equal(filter.Attribute("tid").GetLocation(), diags.Errors[0].Location);
        }

        [Theory]
        [MemberData(nameof(ValidMessageRefs))]
        public void Message_Valid(string message)
        {
            var filter = E("filter", A("name", "Filter1"), A("value", 16), A("message", message));

            ParseInput(ref filter);

            Assert.Empty(diags.Errors);
        }

        [Theory]
        [MemberData(nameof(InvalidMessageRefs))]
        public void Message_Invalid(string message)
        {
            var filter = E("filter", A("name", "Filter1"), A("value", 16), A("message", message));

            ParseInput(ref filter);

            Assert.Single(diags.Errors);
            Assert.Equal(filter.Attribute("message").GetLocation(), diags.Errors[0].Location);
        }

        private void ParseInput(ref XElement elem1)
        {
            parser.ParseManifest(CreateInput("filters", ref elem1), "<stdin>");
        }

        private void ParseInput(ref XElement elem1, ref XElement elem2)
        {
            parser.ParseManifest(CreateInput("filters", ref elem1, ref elem2), "<stdin>");
        }
    }
}
