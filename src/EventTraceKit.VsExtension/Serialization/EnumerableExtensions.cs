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

            var list = source as IList<TSource>;
            if (list != null) {
                switch (list.Count) {
                    case 1:
                        return list[0];
                    default:
                        return default(TSource);
                }
            }

            var readOnlyList = source as IReadOnlyList<TSource>;
            if (readOnlyList != null) {
                switch (readOnlyList.Count) {
                    case 1:
                        return readOnlyList[0];
                    default:
                        return default(TSource);
                }
            }

            using (var enumerator = source.GetEnumerator()) {
                if (!enumerator.MoveNext())
                    return default(TSource);

                TSource current = enumerator.Current;
                if (!enumerator.MoveNext())
                    return current;
            }

            return default(TSource);
        }
    }
}
