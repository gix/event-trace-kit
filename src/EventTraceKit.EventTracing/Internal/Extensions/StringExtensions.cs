namespace EventTraceKit.EventTracing.Internal.Extensions
{
    using System;
    using System.Globalization;

    internal static class StringExtensions
    {
        public static string ToStringInvariant(this int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        ///   Escapes the string so that it is safe to use as a formatting
        ///   string with <see cref="string.Format(string,object)"/> and zero
        ///   args.
        /// </summary>
        /// <param name="str">The string to escape.</param>
        /// <returns>
        ///   The escaped string with formatting-relevant characters ({, })
        ///   escaped.
        /// </returns>
        public static string EscapeFormatting(this string str)
        {
            return str.Replace("{", "{{").Replace("}", "}}");
        }

        /// <summary>
        ///   Returns the longest common prefix of both specified strings.
        /// </summary>
        /// <param name="a">The first string.</param>
        /// <param name="b">The second string.</param>
        /// <returns>
        ///   The longest common prefix of both strings (which may be empty).
        ///   Also returns empty if any input string is <see langword="null"/>.
        /// </returns>
        public static string LongestCommonPrefix(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
                return string.Empty;

            int length = Math.Min(a.Length, b.Length);
            int i;
            for (i = 0; i < length; ++i) {
                if (a[i] != b[i])
                    break;
            }

            return a.Substring(0, i);
        }
    }
}
