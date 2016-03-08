namespace NOpt.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;

    /// <summary>
    ///   Represents a generic indexed collection of key/value pairs.
    /// </summary>
    /// <typeparam name="TKey">
    ///   The type of the keys in the dictionary.
    /// </typeparam>
    /// <typeparam name="TValue">
    ///   The type of the values in the dictionary.
    /// </typeparam>
    [ContractClass(typeof(IOrderedDictionaryContract<,>))]
    public interface IOrderedDictionary<TKey, TValue>
        : IDictionary<TKey, TValue>
    {
        /// <summary>
        ///   Gets an <see cref="IList{T}"/> containing the keys of the
        ///   <see cref="IOrderedDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <returns>
        ///   An <see cref="IList{T}"/> containing the keys of the object that
        ///   implements <see cref="IOrderedDictionary{TKey,TValue}"/>.
        /// </returns>
        new IList<TKey> Keys { get; }

        /// <summary>
        ///   Gets an <see cref="IList{T}"/> containing the values in the
        ///   <see cref="IOrderedDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <returns>
        ///   An <see cref="IList{T}"/> containing the values in the object that
        ///   implements <see cref="IOrderedDictionary{TKey,TValue}"/>.
        /// </returns>
        new IList<TValue> Values { get; }

        /// <summary>
        ///   Gets the element at the specified index.
        /// </summary>
        /// <returns>
        ///   The element with the specified key.
        /// </returns>
        /// <param name="index">
        ///   The index of the element to get or set.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="index"/> is not in [0, <see cref="P:Count"/>).
        /// </exception>
        TValue GetAt(int index);

        /// <summary>
        ///   Sets the element at the specified index.
        /// </summary>
        /// <returns>
        ///   The element with the specified key.
        /// </returns>
        /// <param name="index">
        ///   The index of the element to get or set.
        /// </param>
        /// <param name="value">
        ///   The new value.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="index"/> is not in [0, <see cref="P:Count"/>).
        /// </exception>
        void SetAt(int index, TValue value);

        /// <summary>
        ///   Adds an element with the provided key and value to the
        ///   <see cref="IOrderedDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="index">
        ///   The zero-based index at which the element should be inserted.
        /// </param>
        /// <param name="key">
        ///   The object to use as the key of the element to add.
        /// </param>
        /// <param name="value">
        ///   The object to use as the value of the element to add.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="index"/> is less than 0 or greater than
        ///   <see cref="P:Count"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   An element with the same key already exists in the
        ///   <see cref="IOrderedDictionary{TKey,TValue}"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///   The <see cref="IOrderedDictionary{TKey,TValue}"/> is read-only.
        /// </exception>
        void Insert(int index, TKey key, TValue value);

        /// <summary>
        ///   Determines whether the <see cref="IOrderedDictionary{TKey,TValue}"/>
        ///   contains a specific value.
        /// </summary>
        /// <param name="value">
        ///   The value to locate in the <see cref="IOrderedDictionary{TKey,TValue}"/>.
        ///   The value can be null for reference types.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the <see cref="IOrderedDictionary{TKey,TValue}"/>
        ///   contains an element with the specified value; otherwise,
        ///   <see langword="false"/>.
        /// </returns>
        /// <remarks>
        ///   <para>
        ///     This method determines equality using the default equality comparer
        ///     <see cref="EqualityComparer{T}.Default"/> for
        ///     <typeparamref name="TValue"/>, the type of values in the dictionary.
        ///   </para>
        ///   <para>
        ///     This method performs a linear search; therefore, the average
        ///     execution time is proportional to <see cref="P:Count"/>. That
        ///     is, this method is an <c>O(n)</c> operation, where <c>n</c> is
        ///     <see cref="P:Count"/>.
        ///   </para>
        /// </remarks>
        bool ContainsValue(TValue value);
    }

    /// <summary>
    ///   Contract for <see cref="IOrderedDictionary{TKey,TValue}"/>.
    /// </summary>
    [ContractClassFor(typeof(IOrderedDictionary<,>))]
    internal abstract class IOrderedDictionaryContract<TKey, TValue> : IOrderedDictionary<TKey, TValue>
    {
        IList<TKey> IOrderedDictionary<TKey, TValue>.Keys
        {
            get
            {
                Contract.Ensures(Contract.Result<IList<TKey>>() != null);
                return default(IList<TKey>);
            }
        }

        IList<TValue> IOrderedDictionary<TKey, TValue>.Values
        {
            get
            {
                Contract.Ensures(Contract.Result<IList<TValue>>() != null);
                return default(IList<TValue>);
            }
        }

        TValue IOrderedDictionary<TKey, TValue>.GetAt(int index)
        {
            Contract.Requires<ArgumentOutOfRangeException>(index >= 0 && index < Count);
            return default(TValue);
        }

        void IOrderedDictionary<TKey, TValue>.SetAt(int index, TValue value)
        {
            Contract.Requires<ArgumentOutOfRangeException>(index >= 0 && index < Count);
        }

        void IOrderedDictionary<TKey, TValue>.Insert(int index, TKey key, TValue value)
        {
            Contract.Requires<ArgumentOutOfRangeException>(index >= 0 && index <= Count);
        }

        bool IOrderedDictionary<TKey, TValue>.ContainsValue(TValue value)
        {
            return default(bool);
        }

        public abstract IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public abstract void Add(KeyValuePair<TKey, TValue> item);

        public abstract void Clear();

        public abstract bool Contains(KeyValuePair<TKey, TValue> item);

        public abstract void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex);

        public abstract bool Remove(KeyValuePair<TKey, TValue> item);

        public abstract int Count { get; }
        public abstract bool IsReadOnly { get; }

        public abstract bool ContainsKey(TKey key);

        public abstract void Add(TKey key, TValue value);

        public abstract bool Remove(TKey key);

        public abstract bool TryGetValue(TKey key, out TValue value);

        public abstract TValue this[TKey key] { get; set; }

        public abstract ICollection<TKey> Keys { get; }

        public abstract ICollection<TValue> Values { get; }
    }
}
