namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;

    public static class CollectionExtensions
    {
        public static int IndexOf<TSource>(
            this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            int index = 0;
            foreach (var item in source) {
                if (predicate(item))
                    return index;
                ++index;
            }

            return -1;
        }
    }
}