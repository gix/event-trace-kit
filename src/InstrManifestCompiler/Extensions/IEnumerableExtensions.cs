namespace InstrManifestCompiler.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal static class IEnumerableExtensions
    {
        public static TValue Coalesce<T, TValue>(
            this IEnumerable<T> enumerable, Func<T, TValue> selector)
            where TValue : class
        {
            return enumerable.Select(selector).FirstOrDefault(value => value != null);
        }
    }
}
