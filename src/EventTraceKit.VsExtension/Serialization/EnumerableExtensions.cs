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
                switch (list.Count) {
                    case 1:
                        return list[0];
                    default:
                        return default;
                }
            }

            if (source is IReadOnlyList<TSource> readOnlyList) {
                switch (readOnlyList.Count) {
                    case 1:
                        return readOnlyList[0];
                    default:
                        return default;
                }
            }

            using (var enumerator = source.GetEnumerator()) {
                if (!enumerator.MoveNext())
                    return default;

                TSource current = enumerator.Current;
                if (!enumerator.MoveNext())
                    return current;
            }

            return default;
        }
    }
}
