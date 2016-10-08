namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;

    /// <summary>Provides a thread-safe object pool.</summary>
    /// <typeparam name="T">Specifies the type of the elements stored in the pool.</typeparam>
    [DebuggerDisplay("Count={Count}")]
    public sealed class ObjectPool<T>
    {
        private readonly IProducerConsumerCollection<T> collection;
        private readonly Func<T> generator;

        /// <summary>Initializes an instance of the ObjectPool class.</summary>
        /// <param name="generator">The function used to create items when no items exist in the pool.</param>
        public ObjectPool(Func<T> generator)
            : this(generator, new ConcurrentQueue<T>()) { }

        /// <summary>Initializes an instance of the ObjectPool class.</summary>
        /// <param name="generator">The function used to create items when no items exist in the pool.</param>
        /// <param name="collection">The collection used to store the elements of the pool.</param>
        public ObjectPool(Func<T> generator, IProducerConsumerCollection<T> collection)
        {
            if (generator == null)
                throw new ArgumentNullException(nameof(generator));
            this.generator = generator;
            this.collection = collection;
        }
        /// <summary>Gets the number of elements contained in the collection.</summary>
        public int Count => collection.Count;

        /// <summary>Adds the provided item into the pool.</summary>
        /// <param name="item">The item to be added.</param>
        public void PutObject(T item)
        {
            collection.TryAdd(item);
        }

        /// <summary>Gets an item from the pool.</summary>
        /// <returns>The removed or created item.</returns>
        /// <remarks>If the pool is empty, a new item will be created and returned.</remarks>
        public T GetObject()
        {
            T value;
            return collection.TryTake(out value) ? value : generator();
        }
    }

    public sealed class ValueChangedEventArgs<T> : EventArgs
    {
        private static readonly ConcurrentBag<ValueChangedEventArgs<T>> pool;

        private T newValue;
        private T oldValue;

        static ValueChangedEventArgs()
        {
            pool = new ConcurrentBag<ValueChangedEventArgs<T>>();
        }

        private ValueChangedEventArgs(T oldValue, T newValue)
        {
            this.oldValue = oldValue;
            this.newValue = newValue;
        }

        public static ValueChangedEventArgs<T> Get(T oldValue, T newValue)
        {
            ValueChangedEventArgs<T> args;
            if (pool.TryTake(out args)) {
                args.oldValue = oldValue;
                args.newValue = newValue;
                return args;
            }

            return new ValueChangedEventArgs<T>(oldValue, newValue);
        }

        public void Return()
        {
            Return(this);
        }

        private static void Return(ValueChangedEventArgs<T> args)
        {
            args.oldValue = default(T);
            args.newValue = default(T);
            pool.Add(args);
        }

        public T NewValue => newValue;
        public T OldValue => oldValue;
    }
}
