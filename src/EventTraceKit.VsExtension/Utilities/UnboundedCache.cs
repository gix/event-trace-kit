namespace EventTraceKit.VsExtension.Utilities
{
    using System;
    using System.Collections.Concurrent;

    public class UnboundedCache<TKey, TValue>
    {
        private readonly ConcurrentDictionary<TKey, TValue> cache =
            new ConcurrentDictionary<TKey, TValue>();
        private readonly Func<TKey, TValue> constructor;

        public UnboundedCache(Func<TKey, TValue> constructor)
        {
            this.constructor = constructor;
        }

        public TValue this[TKey key] => cache.GetOrAdd(key, constructor);
    }
}
