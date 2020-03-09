namespace EventTraceKit.EventTracing.Compilation.CodeGen
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    internal static class CodeGenUtils
    {
        public static IEnumerable<int> CodePoints(this string str)
        {
            for (int i = 0; i < str.Length;) {
                int cp;
                if (char.IsSurrogate(str, i)) {
                    cp = char.ConvertToUtf32(str, i);
                    i += 2;
                } else {
                    cp = str[i];
                    ++i;
                }

                yield return cp;
            }
        }

        public static string ToCStringLiteral(this string str, out int length)
        {
            length = 0;

            var builder = new StringBuilder();
            var buffer = new byte[4];
            foreach (int cp in str.CodePoints()) {
                if (IsPrintableAscii(cp)) {
                    builder.Append((char)cp);
                    ++length;
                    continue;
                }

                string s = char.ConvertFromUtf32(cp);
                int len = Encoding.UTF8.GetBytes(s, 0, s.Length, buffer, 0);
                for (int i = 0; i < len; ++i) {
                    builder.AppendFormat("\\{0:D03}", Convert.ToString(buffer[i], 8));
                    ++length;
                }
            }

            return builder.ToString();
        }

        public static string ToCStringLiteral(this byte[] bytes)
        {
            return string.Concat(bytes.Select(x => $"\\x{x:X2}"));
        }

        public static bool IsPrintableAscii(int cp)
        {
            return cp >= 0x20 && cp <= 0x7E;
        }
    }
}
