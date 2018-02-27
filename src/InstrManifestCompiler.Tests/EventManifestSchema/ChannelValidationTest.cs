namespace InstrManifestCompiler.Tests.EventManifestSchema
{
    using System.Collections.Generic;
    using System.Xml.Linq;
    using InstrManifestCompiler.Extensions;
    using InstrManifestCompiler.Support;
    using Xunit;

    public class ChannelValidationTest : ValidationTest
    {
        public static IEnumerable<object[]> ValidChannelNames
        {
            get
            {
                yield return new object[] { "Name1" };
                yield return new object[] { "a" };
                yield return new object[] { "A" };
                yield return new object[] { "_Name_1" };
                yield return new object[] { "Ab0123456789_" };
                yield return new object[] { "Company-Product-Component/Channel Name1" };
            }
        }

        public static IEnumerable<object[]> InvalidChannelNames
        {
            get
            {
                for (int i = 0; i < 32; ++i)
                    yield return new object[] { "foo" + (char)i };

                foreach (var invalid in new[] { '>', '<', '&', '"', '|', '\\', ':', '\'', '?', '*' })
                    yield return new object[] { "foo" + invalid };

                yield return new object[] { string.Empty };
                yield return new object[] { new string('a', 256) };
            }
        }

        [Theory]
        [MemberData(nameof(ValidChannelNames))]
        public void Name_Valid(object name)
        {
            var channel = E("channel", A("name", name), A("type", "Operational"));

            ParseInput(ref channel);

            Assert.Equal(new DiagnosticsCollector.Diagnostic[0], diags.Errors);
        }

        [Theory]
        [MemberData(nameof(InvalidChannelNames))]
        public void Name_Invalid(object name)
        {
            var channel = E("channel", A("name", name), A("type", "Operational"));

            ParseInput(ref channel);

            Assert.Equal(1, diags.Errors.Count);
            var location1 = channel.Attribute("name").GetLocation();
            var location2 = new SourceLocation(
                location1.FilePath,
                location1.LineNumber,
                location1.ColumnNumber + "name=\"foo&#x".Length);

            Assert.True(diags.Errors[0].Location == location1 ||
                        diags.Errors[0].Location == location2,
                        string.Format("({0} || {1}) != {2}", location1, location2, diags.Errors[0].Location));
        }

        [Fact]
        public void Name_Missing()
        {
            var channel = E("channel", A("type", "Operational"));

            ParseInput(ref channel);

            Assert.Equal(1, diags.Errors.Count);
            Assert.Equal(channel.GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void Name_Duplicate()
        {
            var channel1 = E("channel", A("name", "Channel1"), A("type", "Operational"));
            var channel2 = E("channel", A("name", "Channel1"), A("type", "Operational"));

            ParseInput(ref channel1, ref channel2);

            Assert.Equal(1, diags.Errors.Count);
            Assert.Equal(channel2.Attribute("name").GetLocation(), diags.Errors[0].Location);
        }

        [Theory]
        [InlineData("Admin")]
        [InlineData("Operational")]
        [InlineData("Analytic")]
        [InlineData("Debug")]
        public void Type_Valid(string type)
        {
            var channel = E("channel", A("name", "Channel1"), A("type", type));

            ParseInput(ref channel);

            Assert.Equal(new DiagnosticsCollector.Diagnostic[0], diags.Errors);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("Custom")]
        public void Type_Invalid(string type)
        {
            var channel = E("channel", A("name", "Channel1"), A("type", type));

            ParseInput(ref channel);

            Assert.Equal(1, diags.Errors.Count);
            Assert.Equal(channel.Attribute("type").GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void Type_Missing()
        {
            var channel = E("channel", A("name", "Channel1"));

            ParseInput(ref channel);

            Assert.Equal(1, diags.Errors.Count);
            Assert.Equal(channel.GetLocation(), diags.Errors[0].Location);
        }

        [Theory]
        [InlineData(16)]
        [InlineData(100)]
        [InlineData(255)]
        [InlineData("0x10")]
        [InlineData("0xFF")]
        public void Value_Valid(object value)
        {
            var channel = E("channel", A("name", "Channel1"), A("type", "Operational"), A("value", value));

            ParseInput(ref channel);

            Assert.Equal(new DiagnosticsCollector.Diagnostic[0], diags.Errors);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(14)]
        [InlineData(15)]
        [InlineData(256)]
        [InlineData(0x10000)]
        public void Value_Invalid(object value)
        {
            var channel = E("channel", A("name", "Channel1"), A("type", "Operational"), A("value", value));

            ParseInput(ref channel);

            Assert.Equal(1, diags.Errors.Count);
            Assert.Equal(channel.Attribute("value").GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void Value_Duplicate()
        {
            var channel1 = E("channel", A("name", "Channel1"), A("type", "Operational"), A("value", 16));
            var channel2 = E("channel", A("name", "Channel2"), A("type", "Operational"), A("value", 16));

            ParseInput(ref channel1, ref channel2);

            Assert.Equal(1, diags.Errors.Count);
            Assert.Equal(channel2.Attribute("value").GetLocation(), diags.Errors[0].Location);
        }

        [Theory]
        [InlineData("Channel1")]
        [InlineData("ch-1")]
        [InlineData("ch/-_1")]
        public void ChannelId_Valid(string chid)
        {
            var channel = E("channel", A("name", "Channel1"), A("type", "Operational"), A("chid", chid));

            ParseInput(ref channel);

            Assert.Equal(new DiagnosticsCollector.Diagnostic[0], diags.Errors);
        }

        [Theory]
        [InlineData("")]
        public void ChannelId_Invalid(string chid)
        {
            var channel = E("channel", A("name", "Channel1"), A("type", "Operational"), A("chid", chid));

            ParseInput(ref channel);

            Assert.Equal(1, diags.Errors.Count);
            Assert.Equal(channel.Attribute("chid").GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void ChannelId_Duplicate()
        {
            var channel1 = E("channel", A("name", "Channel1"), A("type", "Operational"), A("chid", "Id1"));
            var channel2 = E("channel", A("name", "Channel2"), A("type", "Operational"), A("chid", "Id1"));

            ParseInput(ref channel1, ref channel2);

            Assert.Equal(1, diags.Errors.Count);
            Assert.Equal(channel2.Attribute("chid").GetLocation(), diags.Errors[0].Location);
        }

        [Theory]
        [MemberData(nameof(ValidSymbolNames))]
        public void Symbol_Valid(string symbol)
        {
            var channel = E("channel", A("name", "Channel1"), A("type", "Operational"), A("symbol", symbol));

            ParseInput(ref channel);

            Assert.Equal(new DiagnosticsCollector.Diagnostic[0], diags.Errors);
        }

        [Theory]
        [MemberData(nameof(InvalidSymbolNames))]
        public void Symbol_Invalid(string symbol)
        {
            var channel = E("channel", A("name", "Channel1"), A("type", "Operational"), A("symbol", symbol));

            ParseInput(ref channel);

            Assert.Equal(1, diags.Errors.Count);
            Assert.Equal(channel.Attribute("symbol").GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void Symbol_Duplicate()
        {
            var channel1 = E("channel", A("name", "Channel1"), A("type", "Operational"), A("symbol", "Sym1"));
            var channel2 = E("channel", A("name", "Channel2"), A("type", "Operational"), A("symbol", "Sym1"));

            ParseInput(ref channel1, ref channel2);

            Assert.Equal(1, diags.Errors.Count);
            Assert.Equal(channel2.Attribute("symbol").GetLocation(), diags.Errors[0].Location);
        }

        [Theory]
        [MemberData(nameof(ValidMessageRefs))]
        public void Message_Valid(string message)
        {
            var channel = E("channel", A("name", "Channel1"), A("type", "Operational"), A("message", message));

            ParseInput(ref channel);

            Assert.Equal(new DiagnosticsCollector.Diagnostic[0], diags.Errors);
        }

        [Theory]
        [MemberData(nameof(InvalidMessageRefs))]
        public void Message_Invalid(string message)
        {
            var channel = E("channel", A("name", "Channel1"), A("type", "Operational"), A("message", message));

            ParseInput(ref channel);

            Assert.Equal(1, diags.Errors.Count);
            Assert.Equal(channel.Attribute("message").GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void Access_Valid()
        {
            var channel = E("channel", A("name", "Channel1"), A("type", "Operational"),
                                       A("access", "O:BAG:SYD:"));

            ParseInput(ref channel);

            Assert.Equal(new DiagnosticsCollector.Diagnostic[0], diags.Errors);
        }

        [Fact]
        public void Access_Invalid()
        {
            var channel = E("channel", A("name", "Channel1"), A("type", "Operational"),
                                       A("access", "foo"));

            ParseInput(ref channel);

            Assert.Equal(1, diags.Errors.Count);
            Assert.Equal(channel.Attribute("access").GetLocation(), diags.Errors[0].Location);
        }

        [Theory]
        [InlineData("Application")]
        [InlineData("System")]
        [InlineData("Custom")]
        public void Isolation_Valid(string isolation)
        {
            var channel = E("channel", A("name", "Channel1"), A("type", "Operational"),
                                       A("isolation", isolation));

            ParseInput(ref channel);

            Assert.Equal(new DiagnosticsCollector.Diagnostic[0], diags.Errors);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("Other")]
        public void Isolation_Invalid(string isolation)
        {
            var channel = E("channel", A("name", "Channel1"), A("type", "Operational"),
                                       A("isolation", isolation));

            ParseInput(ref channel);

            Assert.Equal(1, diags.Errors.Count);
            Assert.Equal(channel.Attribute("isolation").GetLocation(), diags.Errors[0].Location);
        }

        [Theory]
        [InlineData("true")]
        [InlineData("false")]
        [InlineData("1")]
        [InlineData("0")]
        public void Enabled_Valid(string enabled)
        {
            var channel = E("channel", A("name", "Channel1"), A("type", "Operational"),
                                       A("enabled", enabled));

            ParseInput(ref channel);

            Assert.Equal(new DiagnosticsCollector.Diagnostic[0], diags.Errors);
        }

        [Theory]
        [InlineData("")]
        [InlineData("2")]
        [InlineData("True")]
        [InlineData("False")]
        public void Enabled_Invalid(string enabled)
        {
            var channel = E("channel", A("name", "Channel1"), A("type", "Operational"),
                                       A("enabled", enabled));

            ParseInput(ref channel);

            Assert.Equal(1, diags.Errors.Count);
            Assert.Equal(channel.Attribute("enabled").GetLocation(), diags.Errors[0].Location);
        }

        private void ParseInput(ref XElement elem1)
        {
            parser.ParseManifest(CreateInput("channels", ref elem1), "<stdin>");
        }

        private void ParseInput(ref XElement elem1, ref XElement elem2)
        {
            parser.ParseManifest(CreateInput("channels", ref elem1, ref elem2), "<stdin>");
        }
    }
}
