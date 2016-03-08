namespace NOpt
{
    using System;

    internal static class StringUtils
    {
        /// <summary>
        ///   Compares two specified <see cref="string"/> objects using the specified
        ///   rules, and returns an integer that indicates their relative position
        ///   in the sort order. If one string prefixes the other, the longer
        ///   string is ordered first.
        /// </summary>
        /// <param name="left">
        ///   The first string to compare.
        /// </param>
        /// <param name="right">
        ///   The second string to compare.
        /// </param>
        /// <param name="comparison">
        ///   One of the enumeration values that specifies the rules to use in
        ///   the comparison.
        /// </param>
        /// <returns>
        ///   A 32-bit signed integer that indicates the lexical relationship
        ///   between the two comparands.
        ///   <list type="table">
        ///     <listheader>
        ///       <term>Value</term>
        ///       <description>Condition</description>
        ///     </listheader>
        ///     <item>
        ///       <term>Less than zero</term>
        ///       <description>
        ///         <paramref name="left"/> is less than <paramref name="right"/>.
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>Less than zero</term>
        ///       <description>
        ///         <paramref name="left"/> equals <paramref name="right"/>.
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>Greater than zero</term>
        ///       <description>
        ///         <paramref name="left"/> is greater than <paramref name="right"/>.
        ///       </description>
        ///     </item>
        ///   </list>
        /// </returns>
        public static int CompareLongest(
            string left, string right, StringComparison comparison)
        {
            int leftLength = left != null ? left.Length : 0;
            int rightLength = right != null ? right.Length : 0;

            int length = Math.Min(leftLength, rightLength);
            int cmp = string.Compare(left, 0, right, 0, length, comparison);
            if (cmp != 0)
                return cmp;

            if (leftLength < rightLength) return +1;
            if (leftLength > rightLength) return -1;
            return 0;
        }
    }
}
