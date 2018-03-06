namespace EventManifestFramework.Tests.Schema
{
    using System.Xml.Linq;
    using EventManifestFramework.Support;
    using Xunit;

    public class TaskValidationTest : ValidationTest
    {
        [Theory]
        [MemberData(nameof(ValidQNames))]
        public void Name_Valid(object name)
        {
            var task = E("task", A("name", name), A("value", 16));

            ParseInput(ref task);

            Assert.Empty(diags.Errors);
        }

        [Theory]
        [MemberData(nameof(InvalidQNames))]
        public void Name_Invalid(object name)
        {
            var task = E("task", A("name", name), A("value", 16));

            ParseInput(ref task);

            Assert.Single(diags.Errors);
            Assert.Equal(task.Attribute("name").GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void Name_Missing()
        {
            var task = E("task", A("value", 16));

            ParseInput(ref task);

            Assert.Single(diags.Errors);
            Assert.Equal(task.GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void Name_Duplicate()
        {
            var task1 = E("task", A("name", "Task1"), A("value", 16));
            var task2 = E("task", A("name", "Task1"), A("value", 17));

            ParseInput(ref task1, ref task2);

            Assert.Single(diags.Errors);
            Assert.Equal(task2.Attribute("name").GetLocation(), diags.Errors[0].Location);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        [InlineData(0xFFFF)]
        [InlineData("0xA")]
        [InlineData("0xffff")]
        public void Value_Valid(object value)
        {
            var task = E("task", A("name", "Task1"), A("value", value));

            ParseInput(ref task);

            Assert.Empty(diags.Errors);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0x10000)]
        public void Value_Invalid(object value)
        {
            var task = E("task", A("name", "Task1"), A("value", value));

            ParseInput(ref task);

            Assert.Single(diags.Errors);
            Assert.Equal(task.Attribute("value").GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void Value_Missing()
        {
            var task = E("task", A("name", "Task1"));

            ParseInput(ref task);

            Assert.Single(diags.Errors);
            Assert.Equal(task.GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void Value_Duplicate()
        {
            var task1 = E("task", A("name", "Task1"), A("value", 16));
            var task2 = E("task", A("name", "Task2"), A("value", 16));

            ParseInput(ref task1, ref task2);

            Assert.Single(diags.Errors);
            Assert.Equal(task2.Attribute("value").GetLocation(), diags.Errors[0].Location);
        }

        [Theory]
        [MemberData(nameof(ValidSymbolNames))]
        public void Symbol_Valid(string symbol)
        {
            var task = E("task", A("name", "Task1"), A("value", 16), A("symbol", symbol));

            ParseInput(ref task);

            Assert.Empty(diags.Errors);
        }

        [Theory]
        [MemberData(nameof(InvalidSymbolNames))]
        public void Symbol_Invalid(string symbol)
        {
            var task = E("task", A("name", "Task1"), A("value", 16), A("symbol", symbol));

            ParseInput(ref task);

            Assert.Single(diags.Errors);
            Assert.Equal(task.Attribute("symbol").GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void Symbol_Duplicate()
        {
            var task1 = E("task", A("name", "Task1"), A("value", 1), A("symbol", "Symbol1"));
            var task2 = E("task", A("name", "Task2"), A("value", 2), A("symbol", "Symbol1"));

            ParseInput(ref task1, ref task2);

            Assert.Single(diags.Errors);
            Assert.Equal(task2.Attribute("symbol").GetLocation(), diags.Errors[0].Location);
        }

        [Theory]
        [InlineData("{00000000-0000-0000-0000-000000000000}")]
        [InlineData("{00000000-0000-0000-0000-000000000001}")]
        public void EventGuid_Valid(string eventGuid)
        {
            var task = E("task", A("name", "Task1"), A("value", 16), A("eventGUID", eventGuid));

            ParseInput(ref task);

            Assert.Empty(diags.Errors);
        }

        [Theory]
        [InlineData("00000000-0000-0000-0000-000000000001")]
        [InlineData(" {00000000-0000-0000-0000-000000000001} ")]
        public void EventGuid_Invalid(string eventGuid)
        {
            var task = E("task", A("name", "Task1"), A("value", 16), A("eventGUID", eventGuid));

            ParseInput(ref task);

            Assert.Single(diags.Errors);
            Assert.Equal(task.Attribute("eventGUID").GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void EventGuid_Unique()
        {
            var task1 = E("task", A("name", "Task1"), A("value", 1), A("eventGUID", "{00000000-0000-0000-0000-000000000001}"));
            var task2 = E("task", A("name", "Task2"), A("value", 2), A("eventGUID", "{00000000-0000-0000-0000-000000000002}"));

            ParseInput(ref task1, ref task2);

            Assert.Empty(diags.Errors);
        }

        [Fact]
        public void EventGuid_Empty()
        {
            var task1 = E("task", A("name", "Task1"), A("value", 1), A("eventGUID", "{00000000-0000-0000-0000-000000000000}"));
            var task2 = E("task", A("name", "Task2"), A("value", 2), A("eventGUID", "{00000000-0000-0000-0000-000000000000}"));

            ParseInput(ref task1, ref task2);

            Assert.Empty(diags.Errors);
        }

        [Fact]
        public void EventGuid_Duplicate()
        {
            var task1 = E("task", A("name", "Task1"), A("value", 1), A("eventGUID", "{00000000-0000-0000-0000-000000000001}"));
            var task2 = E("task", A("name", "Task2"), A("value", 2), A("eventGUID", "{00000000-0000-0000-0000-000000000001}"));

            ParseInput(ref task1, ref task2);

            Assert.Single(diags.Errors);
            Assert.Equal(task2.Attribute("eventGUID").GetLocation(), diags.Errors[0].Location);
        }

        [Theory]
        [MemberData(nameof(ValidMessageRefs))]
        public void Message_Valid(string message)
        {
            var task = E("task", A("name", "Task1"), A("value", 16), A("message", message));

            ParseInput(ref task);

            Assert.Empty(diags.Errors);
        }

        [Theory]
        [MemberData(nameof(InvalidMessageRefs))]
        public void Message_Invalid(string message)
        {
            var task = E("task", A("name", "Task1"), A("value", 16), A("message", message));

            ParseInput(ref task);

            Assert.Single(diags.Errors);
            Assert.Equal(task.Attribute("message").GetLocation(), diags.Errors[0].Location);
        }

        private void ParseInput(ref XElement elem1)
        {
            parser.ParseManifest(CreateInput("tasks", ref elem1), "<stdin>");
        }

        private void ParseInput(ref XElement elem1, ref XElement elem2)
        {
            parser.ParseManifest(CreateInput("tasks", ref elem1, ref elem2), "<stdin>");
        }
    }
}
