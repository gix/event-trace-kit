namespace EventTraceKit.VsExtension.Utilities
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    /// <summary>
    ///   Generic implementation of a fixed-size object pool.
    /// </summary>
    /// <remarks>
    ///   If there is no space in the pool, extra returned objects will be dropped.
    /// </remarks>
    public class ObjectPool<T> where T : class
    {
        [DebuggerDisplay("{Value,nq}")]
        private struct Element
        {
            public T Value;
        }

        // Storage for the pool objects. The first item is stored in a dedicated field because we
        // expect to be able to satisfy most requests from it.
        private readonly Element[] items;
        private T firstItem;

        private readonly Func<T> factory;

        public ObjectPool(Func<T> factory)
            : this(factory, Environment.ProcessorCount * 2)
        {
        }

        public ObjectPool(Func<T> factory, int size)
        {
            Debug.Assert(size >= 1);
            this.factory = factory;
            items = new Element[size - 1];
        }

        private T CreateInstance()
        {
            var instance = factory();
            return instance;
        }

        /// <summary>
        ///   Acquires an object from the pool.
        /// </summary>
        /// <remarks>
        ///   Search strategy is a simple linear probing which is chosen for it
        ///   cache-friendliness. Note that Free will try to store recycled objects
        ///   close to the start thus statistically reducing how far we will
        ///   typically search.
        /// </remarks>
        public T Acquire()
        {
            // Initial read is optimistically not synchronized. In the
            // worst case this may miss a recently returned object which is
            // not a big deal.
            T obj = firstItem;
            if (obj == null || obj != Interlocked.CompareExchange(ref firstItem, null, obj))
                obj = AllocateSlow();

            return obj;
        }

        private T AllocateSlow()
        {
            var items = this.items;
            for (int i = 0; i < items.Length; ++i) {
                // Initial read is optimistically not synchronized. In the
                // worst case this may miss a recently returned object which is
                // not a big deal.
                T obj = items[i].Value;
                if (obj != null) {
                    if (obj == Interlocked.CompareExchange(ref items[i].Value, null, obj))
                        return obj;
                }
            }

            return CreateInstance();
        }

        /// <summary>
        ///   Returns an object to the pool.
        /// </summary>
        /// <remarks>
        ///   Search strategy is a simple linear probing which is chosen for its
        ///   cache-friendliness. Note that Free will try to store recycled objects
        ///   close to the start thus statistically reducing how far we will
        ///   typically search in Allocate.
        /// </remarks>
        public void Release(T obj)
        {
            Validate(obj);

            if (firstItem == null) {
                // Intentionally not using interlocked here. In the worst case
                // two objects may be stored into same slot. This is very
                // unlikely to happen and will only mean that one of the objects
                // will get collected.
                firstItem = obj;
                return;
            }

            FreeSlow(obj);
        }

        private void FreeSlow(T obj)
        {
            var items = this.items;
            for (int i = 0; i < items.Length; ++i) {
                if (items[i].Value == null) {
                    // Intentionally not using interlocked here. In the worst case
                    // two objects may be stored into same slot. This is very
                    // unlikely to happen and will only mean that one of the objects
                    // will get collected.
                    items[i].Value = obj;
                    break;
                }
            }
        }

        [Conditional("DEBUG")]
        private void Validate(object obj)
        {
            Debug.Assert(obj != null, "freeing null?");
            Debug.Assert(firstItem != obj, "freeing twice?");

            var items = this.items;
            for (int i = 0; i < items.Length; ++i) {
                var value = items[i].Value;
                if (value == null)
                    return;

                Debug.Assert(value != obj, "freeing twice?");
            }
        }
    }
}
