namespace EventTraceKit.EventTracing.Tests.Schema
{
    using System.Xml.Linq;
    using EventTraceKit.EventTracing.Schema;
    using EventTraceKit.EventTracing.Support;
    using Xunit;

    public class EventValidationTest : ValidationTest
    {
        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        [InlineData(0xFFFF)]
        [InlineData("0x10")]
        [InlineData("0xFF")]
        public void Value_Valid(object value)
        {
            var @event = E("event", A("value", value));

            ParseInput(ref @event);

            Assert.Empty(diags.Errors);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0x10000)]
        [InlineData("0x10000")]
        [InlineData("foo")]
        public void Value_Invalid(object value)
        {
            var @event = E("event", A("value", value));

            ParseInput(ref @event);

            Assert.Single(diags.Errors);
            Assert.Equal(@event.Attribute("value").GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void Value_Missing()
        {
            var @event = E("event");

            ParseInput(ref @event);

            Assert.Single(diags.Errors);
            Assert.Equal(@event.GetLocation(), diags.Errors[0].Location);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        [InlineData(0xFF)]
        [InlineData("0x10")]
        [InlineData("0xFF")]
        public void Version_Valid(object version)
        {
            var @event = E("event", A("value", 16), A("version", version));

            ParseInput(ref @event);

            Assert.Empty(diags.Errors);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0x10000)]
        [InlineData("0x10000")]
        [InlineData("foo")]
        public void Version_Invalid(object version)
        {
            var @event = E("event", A("value", 16), A("version", version));

            ParseInput(ref @event);

            Assert.Single(diags.Errors);
            Assert.Equal(@event.Attribute("version").GetLocation(), diags.Errors[0].Location);
        }

        [Theory]
        [InlineData(10, 0, 10, 1)] // Different version
        [InlineData(10, 0, 11, 0)] // Different value
        [InlineData(10, 2, 11, 1)] // Different value and version
        public void ValueVersion_Unique(int value1, int version1, int value2, int version2)
        {
            var event1 = E("event", A("value", value1), A("version", version1));
            var event2 = E("event", A("value", value2), A("version", version2));

            ParseInput(ref event1, ref event2);

            Assert.Empty(diags.Errors);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 0)]
        [InlineData(12, 34)]
        public void ValueVersion_Duplicate(int value, int version)
        {
            var event1 = E("event", A("value", value), A("version", version));
            var event2 = E("event", A("value", value), A("version", version));

            ParseInput(ref event1, ref event2);

            Assert.Single(diags.Errors);
            Assert.Equal(event2.GetLocation(), diags.Errors[0].Location);
        }

        [Theory]
        [MemberData(nameof(ValidSymbolNames))]
        public void Symbol_Valid(string symbol)
        {
            var @event = E("event", A("value", 16), A("symbol", symbol));

            ParseInput(ref @event);

            Assert.Empty(diags.Errors);
        }

        [Theory]
        [MemberData(nameof(InvalidSymbolNames))]
        public void Symbol_Invalid(string symbol)
        {
            var @event = E("event", A("value", 16), A("symbol", symbol));

            ParseInput(ref @event);

            Assert.Single(diags.Errors);
            Assert.Equal(@event.Attribute("symbol").GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void Symbol_Duplicate()
        {
            var event1 = E("event", A("value", 16), A("symbol", "Sym1"));
            var event2 = E("event", A("value", 17), A("symbol", "Sym1"));

            ParseInput(ref event1, ref event2);

            Assert.Single(diags.Errors);
            Assert.Equal(event2.Attribute("symbol").GetValueLocation(), diags.Errors[0].Location);
        }

        [Theory]
        [MemberData(nameof(ValidMessageRefs))]
        public void Message_Valid(string message)
        {
            var @event = E("event", A("value", 16), A("message", message));

            ParseInput(ref @event);

            Assert.Empty(diags.Errors);
        }

        [Theory]
        [MemberData(nameof(InvalidMessageRefs))]
        public void Message_Invalid(string message)
        {
            var @event = E("event", A("value", 16), A("message", message));

            ParseInput(ref @event);

            Assert.Single(diags.Errors);
            Assert.Equal(@event.Attribute("message").GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void AdminChannelEvent_RequiresMessage()
        {
            var provider = CreateProvider(
                E("events", E("event", A("value", 1), A("channel", "Admin"), A("level", "win:Error"))),
                E("channels", E("channel", A("name", "Admin"), A("type", "Admin"))));

            var input = CreateInput(ref provider);
            var @event = provider.Element(EventManifestSchema.Namespace + "events").Element(EventManifestSchema.Namespace + "event");

            parser.ParseManifest(input, "<stdin>");

            Assert.Single(diags.Errors);
            Assert.Contains("admin channel", diags.Errors[0].FormattedMessage);
            Assert.Contains("must have a message", diags.Errors[0].FormattedMessage);
            Assert.Equal(@event.GetLocation(), diags.Errors[0].Location);
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("win:Critical", true)]
        [InlineData("win:Error", true)]
        [InlineData("win:Warning", true)]
        [InlineData("win:Informational", true)]
        [InlineData("win:Verbose", false)]
        [InlineData("CustomLevel", false)]
        public void AdminChannelEvent_MustNotBeVerbose(string level, bool valid)
        {
            var provider = CreateProvider(
                E("events",
                    E("event",
                        A("value", 1), A("channel", "Admin"), A("message", "$(string.msg.1)"),
                        level != null ? A("level", level) : null)),
                E("channels", E("channel", A("name", "Admin"), A("type", "Admin"))),
                E("levels", E("level", A("name", "CustomLevel"), A("value", "16"))));

            var input = CreateInput(ref provider);
            var @event = provider.Element(EventManifestSchema.Namespace + "events").Element(EventManifestSchema.Namespace + "event");

            parser.ParseManifest(input, "<stdin>");

            if (valid) {
                Assert.Empty(diags.Errors);
            } else {
                Assert.Single(diags.Errors);
                Assert.Contains("admin channel", diags.Errors[0].FormattedMessage);
                Assert.Contains("must have a level of", diags.Errors[0].FormattedMessage);
                Assert.Equal(@event.GetLocation(), diags.Errors[0].Location);
            }
        }

        private void ParseInput(ref XElement elem1)
        {
            parser.ParseManifest(CreateInput("events", ref elem1), "<stdin>");
        }

        private void ParseInput(ref XElement elem1, ref XElement elem2)
        {
            parser.ParseManifest(CreateInput("events", ref elem1, ref elem2), "<stdin>");
        }
    }
}
