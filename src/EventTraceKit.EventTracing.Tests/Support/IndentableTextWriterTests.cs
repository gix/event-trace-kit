namespace EventTraceKit.EventTracing.Tests.Support
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using EventTraceKit.EventTracing.Support;
    using Xunit;

    public class IndentableTextWriterTests
    {
        private readonly StringWriter baseWriter = new StringWriter(CultureInfo.InvariantCulture);
        private readonly IndentableTextWriter writer;

        public IndentableTextWriterTests()
        {
            writer = new IndentableTextWriter(baseWriter);
        }

        private string GetOutput()
        {
            writer.Flush();
            return baseWriter.ToString();
        }

        public static IEnumerable<object[]> ValueCases
        {
            get
            {
                yield return new object[] { true, "True" };
                yield return new object[] { false, "False" };
                yield return new object[] { byte.MaxValue, "255" };
                yield return new object[] { ushort.MaxValue, "65535" };
                yield return new object[] { uint.MaxValue, "4294967295" };
                yield return new object[] { ulong.MaxValue, "18446744073709551615" };
                yield return new object[] { sbyte.MinValue, "-128" };
                yield return new object[] { sbyte.MaxValue, "127" };
                yield return new object[] { short.MinValue, "-32768" };
                yield return new object[] { short.MaxValue, "32767" };
                yield return new object[] { int.MinValue, "-2147483648" };
                yield return new object[] { int.MaxValue, "2147483647" };
                yield return new object[] { long.MinValue, "-9223372036854775808" };
                yield return new object[] { long.MaxValue, "9223372036854775807" };
                yield return new object[] { 1.5f, "1.5" };
                yield return new object[] { 1.5, "1.5" };
                yield return new object[] { 'A', "A" };
                yield return new object[] { "abc", "abc" };
            }
        }

        [Fact]
        public void Defaults()
        {
            Assert.Equal("    ", writer.IndentChars);
            Assert.Equal(0, writer.IndentLevel);
            Assert.Equal("\n", writer.NewLine);
        }

        [Theory]
        [MemberData(nameof(ValueCases))]
        public void WriteValue(object value, string expected)
        {
            writer.IndentChars = "|>";
            writer.IndentLevel = 3;

            writer.Write((dynamic)value);

            Assert.Equal($"|>|>|>{expected}", GetOutput());
        }

        [Theory]
        [MemberData(nameof(ValueCases))]
        public void WriteValueDoesNotEndLine(object value, string expected)
        {
            writer.IndentChars = ".";
            writer.IndentLevel = 2;

            writer.Write((dynamic)value);
            writer.Write("x");

            Assert.Equal($"..{expected}x", GetOutput());
        }

        [Theory]
        [MemberData(nameof(ValueCases))]
        public void WriteLineValue(object value, string expected)
        {
            writer.IndentLevel = 1;
            writer.IndentChars = "  ";

            writer.WriteLine((dynamic)value);

            Assert.Equal($"  {expected}\n", GetOutput());
        }

        [Theory]
        [MemberData(nameof(ValueCases))]
        public void WriteLineValueEndsLine(object value, string expected)
        {
            writer.IndentChars = ".";
            writer.IndentLevel = 2;

            writer.WriteLine((dynamic)value);
            writer.Write("x");

            Assert.Equal($"..{expected}\n..x", GetOutput());
        }

        [Theory]
        [InlineData(new[] { "\n" }, "\n")]
        [InlineData(new[] { "foo\n" }, "|>|>|>foo\n")]
        [InlineData(new[] { "\nfoo" }, "\n|>|>|>foo")]
        [InlineData(new[] { "\n\nfoo" }, "\n\n|>|>|>foo")]
        [InlineData(new[] { "foo\nbar" }, "|>|>|>foo\n|>|>|>bar")]
        [InlineData(new[] { "foo\n\nbar" }, "|>|>|>foo\n\n|>|>|>bar")]
        [InlineData(new[] { "foo\n", "bar" }, "|>|>|>foo\n|>|>|>bar")]
        [InlineData(new[] { "foo", "\nbar" }, "|>|>|>foo\n|>|>|>bar")]
        [InlineData(new[] { "foo", "\n", "bar" }, "|>|>|>foo\n|>|>|>bar")]
        public void WriteString(string[] inputs, string expected)
        {
            writer.IndentChars = "|>";
            writer.IndentLevel = 3;

            foreach (var input in inputs)
                writer.Write(input);

            Assert.Equal(expected, GetOutput());
        }

        [Theory]
        [InlineData("\r\n", "\n", "\n")]
        [InlineData("foo\r\n", "\n", "..foo\n")]
        [InlineData("\r\nfoo", "\n", "\n..foo")]
        [InlineData("\n", "\r\n", "\r\n")]
        [InlineData("foo\n", "\r\n", "..foo\r\n")]
        [InlineData("\nfoo", "\r\n", "\r\n..foo")]
        [InlineData("foo\r\nbar\r\n\r\nqux", "\n", "..foo\n..bar\n\n..qux")]
        public void WriteStringReplacesNewLines(string input, string newLine, string expected)
        {
            writer.IndentChars = ".";
            writer.IndentLevel = 2;
            writer.NewLine = newLine;

            writer.Write(input);

            Assert.Equal(expected, GetOutput());
        }

        [Theory]
        [InlineData("\n", "\n")]
        [InlineData("foo\n", "|>|>|>foo\n")]
        [InlineData("\nfoo", "\n|>|>|>foo\n")]
        [InlineData("\n\nfoo", "\n\n|>|>|>foo\n")]
        [InlineData("foo\nbar", "|>|>|>foo\n|>|>|>bar\n")]
        [InlineData("foo\n\nbar", "|>|>|>foo\n\n|>|>|>bar\n")]
        public void WriteLineString(string input, string expected)
        {
            writer.IndentChars = "|>";
            writer.IndentLevel = 3;

            writer.WriteLine(input);

            Assert.Equal(expected, GetOutput());
        }
    }
}
