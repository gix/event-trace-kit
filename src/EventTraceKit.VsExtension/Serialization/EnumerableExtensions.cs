namespace EventTraceKit.VsExtension.Serialization
{
    using System;
    using System.Collections.Generic;

    public static class EnumerableExtensions
    {
        public static TSource TryGetSingleOrDefault<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (source is IList<TSource> list) {
                return list.Count switch
                {
                    1 => list[0],
                    _ => default,
                };
            }

            if (source is IReadOnlyList<TSource> readOnlyList) {
                return readOnlyList.Count switch
                {
                    1 => readOnlyList[0],
                    _ => default,
                };
            }

            using var enumerator = source.GetEnumerator();
            if (!enumerator.MoveNext())
                return default;

            TSource current = enumerator.Current;
            if (!enumerator.MoveNext())
                return current;

            return default;
        }
    }
}
