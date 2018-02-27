namespace NOpt.Extensions
{
    using System;
    using System.Diagnostics.Contracts;

    internal static class StringExtensions
    {
        /// <summary>
        ///   Determines whether the substring of this <paramref name="string"/>
        ///   instance starting at a specified <paramref name="index"/> matches
        ///   the specified <paramref name="prefix"/>.
        /// </summary>
        /// <param name="string">
        ///   The string to compare against.
        /// </param>
        /// <param name="prefix">
        ///   The string to compare.
        /// </param>
        /// <param name="index">
        ///   The index of <paramref name="string"/> at which to start the comparison.
        /// </param>
        /// <param name="comparisonType">
        ///   One of the enumeration values that determines how <paramref name="string"/>
        ///   and <paramref name="prefix"/> are compared.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="prefix"/> matches the
        ///   string beginning at <paramref name="index"/>; otherwise,
        ///   <see langword="false"/>.
        /// </returns>
        public static bool StartsWith(
            this string @string, string prefix, int index, StringComparison comparisonType)
        {
            Contract.Requires<ArgumentNullException>(@string != null);
            if (prefix == null)
                return @string.Length >= index;
            return
                @string.Length >= prefix.Length + index &&
                string.Compare(@string, index, prefix, 0, prefix.Length, comparisonType) == 0;
        }
    }
}
