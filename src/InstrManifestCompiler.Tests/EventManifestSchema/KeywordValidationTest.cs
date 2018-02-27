namespace InstrManifestCompiler.Tests.EventManifestSchema
{
    using System.Collections.Generic;
    using System.Xml.Linq;
    using InstrManifestCompiler.Extensions;
    using Xunit;

    public class KeywordValidationTest : ValidationTest
    {
        [Theory]
        [MemberData(nameof(ValidQNames))]
        public void Name_Valid(object name)
        {
            var keyword = E("keyword", A("name", name), A("mask", "0x1"));

            ParseInput(ref keyword);

            Assert.Equal(new DiagnosticsCollector.Diagnostic[0], diags.Errors);
        }

        [Theory]
        [MemberData(nameof(InvalidQNames))]
        public void Name_Invalid(object name)
        {
            var keyword = E("keyword", A("name", name), A("mask", "0x1"));

            ParseInput(ref keyword);

            Assert.Equal(1, diags.Errors.Count);
            Assert.Equal(keyword.Attribute("name").GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void Name_Missing()
        {
            var keyword = E("keyword", A("mask", "0x1"));

            ParseInput(ref keyword);

            Assert.Equal(1, diags.Errors.Count);
            Assert.Equal(keyword.GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void Name_Duplicate()
        {
            var keyword1 = E("keyword", A("name", "Keyword1"), A("mask", "0x1"));
            var keyword2 = E("keyword", A("name", "Keyword1"), A("mask", "0x2"));

            ParseInput(ref keyword1, ref keyword2);

            Assert.Equal(1, diags.Errors.Count);
            Assert.Equal(keyword2.Attribute("name").GetLocation(), diags.Errors[0].Location);
        }

        public static IEnumerable<object[]> ValidMasks
        {
            get
            {
                for (int i = 0; i < 64; ++i)
                    yield return new object[] { "0x" + ((ulong)1 << i).ToString("X") };
            }
        }

        [Theory]
        [MemberData(nameof(ValidMasks))]
        public void Mask_Valid(object mask)
        {
            var keyword = E("keyword", A("name", "Keyword1"), A("mask", mask));

            ParseInput(ref keyword);

            Assert.Equal(new DiagnosticsCollector.Diagnostic[0], diags.Errors);
        }

        [Theory]
        [InlineData("")] // empty
        [InlineData(" ")] // whitespace
        [InlineData("16")] // integer
        [InlineData("0x11")] // multiple bits set
        public void Mask_Invalid(object mask)
        {
            var keyword = E("keyword", A("name", "Keyword1"), A("mask", mask));

            ParseInput(ref keyword);

            Assert.Equal(1, diags.Errors.Count);
            Assert.Equal(keyword.Attribute("mask").GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void Mask_Missing()
        {
            var keyword = E("keyword", A("name", "Keyword1"));

            ParseInput(ref keyword);

            Assert.Equal(1, diags.Errors.Count);
            Assert.Equal(keyword.GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void Mask_Duplicate()
        {
            var keyword1 = E("keyword", A("name", "Keyword1"), A("mask", "0x1"));
            var keyword2 = E("keyword", A("name", "Keyword2"), A("mask", "0x1"));

            ParseInput(ref keyword1, ref keyword2);

            Assert.Equal(1, diags.Errors.Count);
            Assert.Equal(keyword2.Attribute("mask").GetLocation(), diags.Errors[0].Location);
        }

        [Theory]
        [MemberData(nameof(ValidSymbolNames))]
        public void Symbol_Valid(string symbol)
        {
            var keyword = E("keyword", A("name", "Keyword1"), A("mask", "0x1"), A("symbol", symbol));

            ParseInput(ref keyword);

            Assert.Equal(new DiagnosticsCollector.Diagnostic[0], diags.Errors);
        }

        [Theory]
        [MemberData(nameof(InvalidSymbolNames))]
        public void Symbol_Invalid(string symbol)
        {
            var keyword = E("keyword", A("name", "Keyword1"), A("mask", "0x1"), A("symbol", symbol));

            ParseInput(ref keyword);

            Assert.Equal(1, diags.Errors.Count);
            Assert.Equal(keyword.Attribute("symbol").GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void Symbol_Duplicate()
        {
            var keyword1 = E("keyword", A("name", "Keyword1"), A("mask", "0x1"), A("symbol", "Sym1"));
            var keyword2 = E("keyword", A("name", "Keyword2"), A("mask", "0x2"), A("symbol", "Sym1"));

            ParseInput(ref keyword1, ref keyword2);

            Assert.Equal(1, diags.Errors.Count);
            Assert.Equal(keyword2.Attribute("symbol").GetLocation(), diags.Errors[0].Location);
        }

        [Theory]
        [MemberData(nameof(ValidMessageRefs))]
        public void Message_Valid(string message)
        {
            var keyword = E("keyword", A("name", "Keyword1"), A("mask", "0x1"), A("message", message));

            ParseInput(ref keyword);

            Assert.Equal(new DiagnosticsCollector.Diagnostic[0], diags.Errors);
        }

        [Theory]
        [MemberData(nameof(InvalidMessageRefs))]
        public void Message_Invalid(string message)
        {
            var keyword = E("keyword", A("name", "Keyword1"), A("mask", "0x1"), A("message", message));

            ParseInput(ref keyword);

            Assert.Equal(1, diags.Errors.Count);
            Assert.Equal(keyword.Attribute("message").GetLocation(), diags.Errors[0].Location);
        }

        private void ParseInput(ref XElement elem1)
        {
            parser.ParseManifest(CreateInput("keywords", ref elem1), "<stdin>");
        }

        private void ParseInput(ref XElement elem1, ref XElement elem2)
        {
            parser.ParseManifest(CreateInput("keywords", ref elem1, ref elem2), "<stdin>");
        }
    }
}
