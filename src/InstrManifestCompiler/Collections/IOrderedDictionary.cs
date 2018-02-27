namespace InstrManifestCompiler.Collections
{
    using System.Collections.Generic;

    /// <summary>
    ///   Represents a generic indexed collection of key/value pairs.
    /// </summary>
    /// <typeparam name="TKey">
    ///   The type of the keys in the dictionary.
    /// </typeparam>
    /// <typeparam name="TValue">
    ///   The type of the values in the dictionary.
    /// </typeparam>
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
}
