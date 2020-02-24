namespace Xunit.Sdk
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using EventTraceKit.EventTracing.Tests.Compilation.TestSupport;

    internal class SequenceEqualException : EqualException
    {
        private static readonly Dictionary<char, string> Encodings = new Dictionary<char, string> {
                { '\r', "\\r" },
                { '\n', "\\n" },
                { '\t', "\\t" },
                { '\0', "\\0" }
            };

        private string message;
        private readonly BufferedEnumerable actualSequence;
        private readonly BufferedEnumerable expectedSequence;

        public SequenceEqualException(BufferedEnumerable expected, BufferedEnumerable actual, int expectedIndex, int actualIndex)
           : base(expected.Sequence, actual.Sequence)
        {
            actualSequence = actual;
            expectedSequence = expected;
            ActualIndex = actualIndex;
            ExpectedIndex = expectedIndex;
        }

        public new int ActualIndex { get; }
        public new int ExpectedIndex { get; }

        public override string Message
        {
            get
            {
                if (message == null)
                    message = CreateMessage();

                return message;
            }
        }

        private string CreateMessage()
        {
            if (ExpectedIndex == -1)
                return base.Message;

            Tuple<string, string> printedExpected = ShortenAndEncode(expectedSequence, ExpectedIndex, '↓');
            Tuple<string, string> printedActual = ShortenAndEncode(actualSequence, ActualIndex, '↑');

            return string.Format(
                CultureInfo.CurrentCulture,
                "{1}{0}          {2}{0}Expected: {3}{0}Actual:   {4}{0}          {5}",
                Environment.NewLine,
                UserMessage,
                printedExpected.Item2,
                printedExpected.Item1 ?? "(null)",
                printedActual.Item1 ?? "(null)",
                printedActual.Item2);
        }

        private static string ConvertToSimpleTypeName(TypeInfo typeInfo)
        {
            if (!typeInfo.IsGenericType)
                return typeInfo.Name;

            var simpleNames = typeInfo.GenericTypeArguments.Select(type => ConvertToSimpleTypeName(type.GetTypeInfo()));
            var backTickIdx = typeInfo.Name.IndexOf('`');
            if (backTickIdx < 0)
                backTickIdx = typeInfo.Name.Length;  // F# doesn't use backticks for generic type names

            return string.Format("{0}<{1}>", typeInfo.Name.Substring(0, backTickIdx), string.Join(", ", simpleNames));
        }

        private static Tuple<string, string> ShortenAndEncode(BufferedEnumerable value, int position, char pointer)
        {
            int start = value.GetStartIndex(position);
            int end = value.GetEndIndex(position);
            var printedValue = new StringBuilder(100);
            var printedPointer = new StringBuilder(100);

            var simpleTypeName = ConvertToSimpleTypeName(value.Sequence.GetType().GetTypeInfo());
            printedValue.AppendFormat("{0} [", simpleTypeName);
            printedPointer.Append(' ', simpleTypeName.Length + 2);

            if (start > 0) {
                printedValue.Append("···, ");
                printedPointer.Append("     ");
            }

            for (int idx = start; idx < end; ++idx) {
                var item = value[idx];
                int paddingLength = 1;

                if (idx != start) {
                    printedValue.Append(", ");
                    printedPointer.Append("  ");
                }

                if (item is char c && Encodings.TryGetValue(c, out string encoding)) {
                    printedValue.Append(encoding);
                    paddingLength = encoding.Length;
                } else {
                    var str = item?.ToString() ?? "(null)";
                    printedValue.Append(str);
                    paddingLength = str.Length;
                }

                if (idx < position)
                    printedPointer.Append(' ', paddingLength);
                else if (idx == position)
                    printedPointer.AppendFormat("{0} (pos {1})", pointer, position);
            }

            if (value.Count == position)
                printedPointer.AppendFormat("  {0} (pos {1})", pointer, position);

            if (end < value.Count)
                printedValue.Append(", ···");

            printedValue.Append("]");
            printedPointer.Append(" ");

            return new Tuple<string, string>(printedValue.ToString(), printedPointer.ToString());
        }
    }
}
