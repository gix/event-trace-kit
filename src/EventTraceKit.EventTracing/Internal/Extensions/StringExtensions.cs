namespace EventTraceKit.EventTracing.Internal.Extensions
{
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
    }
}
