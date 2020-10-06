namespace EventTraceKit.EventTracing.Tests.Schema
{
    using System.Linq;
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

        [Theory]
        [InlineData("abc %1!")]
        [InlineData("abc %1!sss")]
        [InlineData("%1 %2!")]
        [InlineData("%3 %2!  ")]
        public void Message_UnterminatedFormatSpec(string message)
        {
            XNamespace ns = EventManifestSchema.Namespace;

            var provider = CreateProvider();
            AddEvent(provider, message, 1,
                E("data", A("name", "p1"), A("inType", "win:Int32")),
                E("data", A("name", "p2"), A("inType", "win:Int32")),
                E("data", A("name", "p3"), A("inType", "win:Int32")));

            var input = CreateInput(ref provider);
            var localizedString = provider.Document.Element(ns + "instrumentationManifest")
                .Element(ns + "localization")
                .Element(ns + "resources")
                .Element(ns + "stringTable")
                .Elements(ns + "string")
                .Single(x => x.Attribute("id")?.Value == "str.1");

            parser.ParseManifest(input, "<stdin>");

            Assert.Single(diags.Errors);
            Assert.Contains("unterminated format specification", diags.Errors[0].FormattedMessage);
            Assert.Equal(localizedString.GetLocation(), diags.Errors[0].Location);
        }

        [Theory]
        [InlineData("abc %1%!")]
        [InlineData("abc %1%!!")]
        public void Message_EscapedFormatSpec(string message)
        {
            var provider = CreateProvider();
            AddEvent(provider, message, 1,
                E("data", A("name", "p1"), A("inType", "win:Int32")),
                E("data", A("name", "p2"), A("inType", "win:Int32")),
                E("data", A("name", "p3"), A("inType", "win:Int32")));

            var input = CreateInput(ref provider);
            parser.ParseManifest(input, "<stdin>");

            Assert.Empty(diags.Errors);
        }

        [Fact]
        public void Message_OutOfBoundsPlaceholder()
        {
            XNamespace ns = EventManifestSchema.Namespace;

            var provider = CreateProvider();
            AddEvent(provider, "abc %2 %% %3", 1,
                E("data", A("name", "threadId"), A("inType", "win:Int32")));

            var input = CreateInput(ref provider);
            var localizedString = provider.Document.Element(ns + "instrumentationManifest")
                .Element(ns + "localization")
                .Element(ns + "resources")
                .Element(ns + "stringTable")
                .Elements(ns + "string")
                .Single(x => x.Attribute("id")?.Value == "str.1");

            parser.ParseManifest(input, "<stdin>");

            Assert.Equal(2, diags.Errors.Count);
            Assert.Contains("references non-existent property", diags.Errors[0].FormattedMessage);
            Assert.Contains("references non-existent property", diags.Errors[1].FormattedMessage);
            Assert.Equal(localizedString.GetLocation(), diags.Errors[0].Location);
            Assert.Equal(localizedString.GetLocation(), diags.Errors[1].Location);
        }

        [Fact]
        public void Message_OutOfBoundsPlaceholder_NoTemplate()
        {
            XNamespace ns = EventManifestSchema.Namespace;

            var provider = CreateProvider();
            AddEvent(provider, "abc %2 %%", 1);

            var input = CreateInput(ref provider);
            var localizedString = provider.Document.Element(ns + "instrumentationManifest")
                .Element(ns + "localization")
                .Element(ns + "resources")
                .Element(ns + "stringTable")
                .Elements(ns + "string")
                .Single(x => x.Attribute("id")?.Value == "str.1");

            parser.ParseManifest(input, "<stdin>");

            Assert.Single(diags.Errors);
            Assert.Contains("references non-existent property", diags.Errors[0].FormattedMessage);
            Assert.Equal(localizedString.GetLocation(), diags.Errors[0].Location);
        }

        private static void AddEvent(
            XElement provider, string message, int eventId, params object[] templateFields)
        {
            XNamespace ns = EventManifestSchema.Namespace;

            string stringId = $"str.{eventId}";
            string templateId = $"EventTemplate{eventId}";
            var events = provider.GetOrCreateElement(ns + "events");
            bool useTemplate = templateFields != null && templateFields.Length != 0;

            events.Add(
                E("event",
                    A("value", eventId),
                    useTemplate ? A("template", templateId) : null,
                    A("message", $"$(string.{stringId})"))
                );

            if (useTemplate) {
                var templates = provider.GetOrCreateElement(ns + "templates");
                templates.Add(E("template", A("tid", templateId), templateFields));
            }

            var stringTable = provider.Document.Element(ns + "instrumentationManifest")
                .Element(ns + "localization")
                .Element(ns + "resources")
                .Element(ns + "stringTable");

            stringTable.Add(new XElement(ns + "string",
                    new XAttribute("id", stringId),
                    new XAttribute("value", message)));
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
