namespace NOpt.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Security;
    using System.Threading;
    using NOpt.Extensions;

    /// <summary>
    ///   Represents a generic indexed collection of key/value pairs.
    /// </summary>
    /// <typeparam name="TKey">
    ///   The type of the keys in the dictionary.
    /// </typeparam>
    /// <typeparam name="TValue">
    ///   The type of the values in the dictionary.
    /// </typeparam>
    [Serializable]
    [ComVisible(false)]
    [DebuggerTypeProxy(typeof(OrderedDictionary<,>.DebuggerProxy))]
    [DebuggerDisplay("Count = {Count}")]
    public sealed class OrderedDictionary<TKey, TValue>
        : IOrderedDictionary<TKey, TValue>,
          IOrderedDictionary,
          ISerializable
    {
        private readonly Dictionary<TKey, TValue> objectsTable;
        private readonly List<KeyValuePair<TKey, TValue>> objectsArray;
        private readonly IEqualityComparer<TKey> comparer;
        private object syncRoot;

        /// <summary>
        ///   Initializes a new instance of the
        ///   <see cref="OrderedDictionary{TKey,TValue}"/> class that is empty,
        ///   has the default initial capacity, and uses the default equality
        ///   comparer for the key type.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Every key in a <see cref="OrderedDictionary{TKey,TValue}"/> must
        ///     be unique according to the default equality comparer.
        ///   </para>
        ///   <para>
        ///     <see cref="OrderedDictionary{TKey,TValue}"/> requires an equality
        ///     implementation to determine whether keys are equal. This constructor
        ///     uses the default generic equality comparer,
        ///     <see cref="EqualityComparer{T}.Default"/>. If type
        ///     <typeparamref name="TKey"/> implements the <see cref="IEquatable{T}"/>
        ///     generic interface, the default equality comparer uses that
        ///     implementation. Alternatively, you can specify an implementation
        ///     of the <see cref="IEqualityComparer{T}"/> generic interface by
        ///     using a constructor that accepts a comparer parameter.
        ///   </para>
        /// </remarks>
        public OrderedDictionary()
            : this(0, null)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the
        ///   <see cref="OrderedDictionary{TKey,TValue}"/> class that is empty,
        ///   has the specified initial capacity, and uses the default equality
        ///   comparer for the key type.
        /// </summary>
        /// <param name="capacity">
        ///   The initial number of elements that the
        ///   <see cref="OrderedDictionary{TKey,TValue}"/> can contain.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="capacity"/> is less than 0.
        /// </exception>
        /// <remarks>
        ///   <para>
        ///     Every key in a <see cref="OrderedDictionary{TKey,TValue}"/> must
        ///     be unique according to the default equality comparer.
        ///   </para>
        ///   <para>
        ///     The capacity of a <see cref="OrderedDictionary{TKey,TValue}"/>
        ///     is the number of elements that can be added to the
        ///     <see cref="OrderedDictionary{TKey,TValue}"/> before resizing is
        ///     necessary. As elements are added to a
        ///     <see cref="OrderedDictionary{TKey,TValue}"/>, the capacity is
        ///     automatically increased as required by reallocating the internal
        ///     array.
        ///   </para>
        ///   <para>
        ///     If the size of the collection can be estimated, specifying the
        ///     initial capacity eliminates the need to perform a number of resizing
        ///     operations while adding elements to the
        ///     <see cref="OrderedDictionary{TKey,TValue}"/>.
        ///   </para>
        ///   <para>
        ///     <see cref="OrderedDictionary{TKey,TValue}"/> requires an equality
        ///     implementation to determine whether keys are equal. This constructor
        ///     uses the default generic equality comparer,
        ///     <see cref="EqualityComparer{T}.Default"/>. If type
        ///     <typeparamref name="TKey"/> implements the <see cref="IEquatable{T}"/>
        ///     generic interface, the default equality comparer uses that
        ///     implementation. Alternatively, you can specify an implementation
        ///     of the <see cref="IEqualityComparer{T}"/> generic interface by
        ///     using a constructor that accepts a comparer parameter.
        ///   </para>
        /// </remarks>
        public OrderedDictionary(int capacity)
            : this(capacity, null)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the
        ///   <see cref="OrderedDictionary{TKey,TValue}"/> class that is empty,
        ///   has the default initial capacity, and uses the specified
        ///   <see cref="IEqualityComparer{T}"/>.
        /// </summary>
        /// <param name="comparer">
        ///   The <see cref="IEqualityComparer{T}"/> implementation to use when
        ///   comparing keys, or <see langword="null"/> to use the default
        ///   <see cref="EqualityComparer{T}"/> for the type of the key.
        /// </param>
        /// <remarks>
        ///   <para>
        ///     Every key in a <see cref="OrderedDictionary{TKey,TValue}"/> must
        ///     be unique according to the default equality comparer.
        ///   </para>
        ///   <para>
        ///     The capacity of a <see cref="OrderedDictionary{TKey,TValue}"/>
        ///     is the number of elements that can be added to the
        ///     <see cref="OrderedDictionary{TKey,TValue}"/> before resizing is
        ///     necessary. As elements are added to a
        ///     <see cref="OrderedDictionary{TKey,TValue}"/>, the capacity is
        ///     automatically increased as required by reallocating the internal
        ///     array.
        ///   </para>
        ///   <para>
        ///     <see cref="OrderedDictionary{TKey,TValue}"/> requires an equality
        ///     implementation to determine whether keys are equal. If
        ///     <paramref name="comparer"/> is <see langword="null"/>, this constructor
        ///     uses the default generic equality comparer,
        ///     <see cref="EqualityComparer{T}.Default"/>. If type
        ///     <typeparamref name="TKey"/> implements the <see cref="IEquatable{T}"/>
        ///     generic interface, the default equality comparer uses that
        ///     implementation.
        ///   </para>
        /// </remarks>
        public OrderedDictionary(IEqualityComparer<TKey> comparer)
            : this(0, comparer)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the
        ///   <see cref="OrderedDictionary{TKey,TValue}"/> class that is empty,
        ///   has the specified initial capacity, and uses the specified
        ///   <see cref="IEqualityComparer{T}"/>.
        /// </summary>
        /// <param name="capacity">
        ///   The initial number of elements that the
        ///   <see cref="OrderedDictionary{TKey,TValue}"/> can contain.
        /// </param>
        /// <param name="comparer">
        ///   The <see cref="IEqualityComparer{T}"/> implementation to use when
        ///   comparing keys, or <see langword="null"/> to use the default
        ///   <see cref="EqualityComparer{T}"/> for the type of the key.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="capacity"/> is less than 0.
        /// </exception>
        /// <remarks>
        ///   <para>
        ///     Every key in a <see cref="OrderedDictionary{TKey,TValue}"/> must
        ///     be unique according to the default equality comparer.
        ///   </para>
        ///   <para>
        ///     The capacity of a <see cref="OrderedDictionary{TKey,TValue}"/>
        ///     is the number of elements that can be added to the
        ///     <see cref="OrderedDictionary{TKey,TValue}"/> before resizing is
        ///     necessary. As elements are added to a
        ///     <see cref="OrderedDictionary{TKey,TValue}"/>, the capacity is
        ///     automatically increased as required by reallocating the internal
        ///     array.
        ///   </para>
        ///   <para>
        ///     If the size of the collection can be estimated, specifying the
        ///     initial capacity eliminates the need to perform a number of resizing
        ///     operations while adding elements to the
        ///     <see cref="OrderedDictionary{TKey,TValue}"/>.
        ///   </para>
        ///   <para>
        ///     <see cref="OrderedDictionary{TKey,TValue}"/> requires an equality
        ///     implementation to determine whether keys are equal. If
        ///     <paramref name="comparer"/> is <see langword="null"/>, this constructor
        ///     uses the default generic equality comparer,
        ///     <see cref="EqualityComparer{T}.Default"/>. If type
        ///     <typeparamref name="TKey"/> implements the <see cref="IEquatable{T}"/>
        ///     generic interface, the default equality comparer uses that
        ///     implementation.
        ///   </para>
        /// </remarks>
        public OrderedDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            this.comparer = comparer;
            objectsTable = new Dictionary<TKey, TValue>(capacity, comparer);
            objectsArray = new List<KeyValuePair<TKey, TValue>>(capacity);
        }

        /// <summary>
        ///   Gets an <see cref="IList{T}"/> containing the keys of the
        ///   <see cref="IOrderedDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <returns>
        ///   An <see cref="IList{T}"/> containing the keys of the object that
        ///   implements <see cref="IOrderedDictionary{TKey,TValue}"/>.
        /// </returns>
        public IList<TKey> Keys
        {
            get { return new KeyCollection(this); }
        }

        /// <summary>
        ///   Gets an <see cref="IList{T}"/> containing the values in the
        ///   <see cref="IOrderedDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <returns>
        ///   An <see cref="IList{T}"/> containing the values in the object that
        ///   implements <see cref="IOrderedDictionary{TKey,TValue}"/>.
        /// </returns>
        public IList<TValue> Values
        {
            get { return new ValueCollection(this); }
        }

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
        ///   <paramref name="index"/> is not in [0, <see cref="Count"/>).
        /// </exception>
        public TValue GetAt(int index)
        {
            return objectsArray[index].Value;
        }

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
        ///   <paramref name="index"/> is not in [0, <see cref="Count"/>).
        /// </exception>
        public void SetAt(int index, TValue value)
        {
            KeyValuePair<TKey, TValue> entry = objectsArray[index];
            TKey key = entry.Key;
            objectsArray[index] = new KeyValuePair<TKey, TValue>(key, value);
            objectsTable[key] = value;
        }

        /// <summary>
        ///   Adds an element with the provided key and value to the
        ///   <see cref="OrderedDictionary{TKey,TValue}"/>.
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
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   An element with the same key already exists in the
        ///   <see cref="OrderedDictionary{TKey,TValue}"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///   The <see cref="OrderedDictionary{TKey,TValue}"/> is read-only.
        /// </exception>
        public void Insert(int index, TKey key, TValue value)
        {
            objectsArray.Insert(index, new KeyValuePair<TKey, TValue>(key, value));
            objectsTable.Add(key, value);
        }

        /// <summary>
        ///   Determines whether the <see cref="OrderedDictionary{TKey,TValue}"/>
        ///   contains a specific value.
        /// </summary>
        /// <param name="value">
        ///   The value to locate in the <see cref="OrderedDictionary{TKey,TValue}"/>.
        ///   The value can be null for reference types.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the <see cref="OrderedDictionary{TKey,TValue}"/>
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
        ///     execution time is proportional to <see cref="Count"/>. That is,
        ///     this method is an <c>O(n)</c> operation, where <c>n</c> is
        ///     <see cref="Count"/>.
        ///   </para>
        /// </remarks>
        public bool ContainsValue(TValue value)
        {
            return objectsTable.ContainsValue(value);
        }

        #region Implementation of IDictionary<TKey, TValue>

        /// <summary>
        ///   Gets the number of elements contained in the <see cref="ICollection{T}"/>.
        /// </summary>
        /// <returns>
        ///   The number of elements contained in the <see cref="ICollection{T}"/>.
        /// </returns>
        public int Count
        {
            get { return objectsArray.Count; }
        }

        /// <summary>
        ///   Gets an <see cref="ICollection{T}"/> containing the keys of the
        ///   <see cref="IDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <returns>
        ///   An <see cref="ICollection{T}"/> containing the keys of the object
        ///   that implements <see cref="IDictionary{TKey,TValue}"/>.
        /// </returns>
        ICollection<TKey> IDictionary<TKey, TValue>.Keys
        {
            get { return new KeyCollection(this); }
        }

        /// <summary>
        ///   Gets an <see cref="ICollection{T}"/> containing the values in the
        ///   <see cref="IDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <returns>
        ///   An <see cref="ICollection{T}"/> containing the values in the object
        ///   that implements <see cref="IDictionary{TKey,TValue}"/>.
        /// </returns>
        ICollection<TValue> IDictionary<TKey, TValue>.Values
        {
            get { return new ValueCollection(this); }
        }

        /// <summary>
        ///   Gets or sets the element with the specified key.
        /// </summary>
        /// <returns>
        ///   The element with the specified key.
        /// </returns>
        /// <param name="key">
        ///   The key of the element to get or set.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        ///   The property is retrieved and <paramref name="key"/> is not found.
        /// </exception>
        public TValue this[TKey key]
        {
            get { return objectsTable[key]; }
            set
            {
                if (objectsTable.ContainsKey(key)) {
                    objectsTable[key] = value;
                    objectsArray[IndexOfKey(key)] = new KeyValuePair<TKey, TValue>(key, value);
                } else {
                    Add(key, value);
                }
            }
        }

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        ///   A <see cref="IEnumerator{T}"/> that can be used to iterate through
        ///   the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return objectsArray.GetEnumerator();
        }

        /// <summary>
        ///   Determines whether the <see cref="IDictionary{TKey,TValue}"/> contains
        ///   an element with the specified key.
        /// </summary>
        /// <returns>
        ///   true if the <see cref="IDictionary{TKey,TValue}"/> contains an
        ///   element with the key; otherwise, false.
        /// </returns>
        /// <param name="key">
        ///   The key to locate in the <see cref="IDictionary{TKey,TValue}"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key"/> is <see langword="null"/>.
        /// </exception>
        public bool ContainsKey(TKey key)
        {
            return objectsTable.ContainsKey(key);
        }

        /// <summary>
        ///   Gets the value associated with the specified key.
        /// </summary>
        /// <returns>
        ///   true if the object that implements <see cref="IDictionary{TKey,TValue}"/>
        ///   contains an element with the specified key; otherwise, false.
        /// </returns>
        /// <param name="key">
        ///   The key whose value to get.
        /// </param>
        /// <param name="value">
        ///   When this method returns, the value associated with the specified
        ///   key, if the key is found; otherwise, the default value for the
        ///   type of the <paramref name="value"/> parameter. This parameter is
        ///   passed uninitialized.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key"/> is <see langword="null"/>.
        /// </exception>
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            bool ret = objectsTable.TryGetValue(key, out value);
            Contract.Assume(ret == ContainsKey(key));
            return ret;
        }

        /// <summary>
        ///   Removes all items from the <see cref="ICollection{T}"/>.
        /// </summary>
        /// <exception cref="NotSupportedException">
        ///   The <see cref="ICollection{T}"/> is read-only.
        /// </exception>
        public void Clear()
        {
            objectsTable.Clear();
            objectsArray.Clear();
        }

        /// <summary>
        ///   Adds an element with the provided key and value to the
        ///   <see cref="IDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="key">
        ///   The object to use as the key of the element to add.
        /// </param>
        /// <param name="value">
        ///   The object to use as the value of the element to add.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   An element with the same key already exists in the
        ///   <see cref="IDictionary{TKey,TValue}"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///   The <see cref="IDictionary{TKey,TValue}"/> is read-only.
        /// </exception>
        public void Add(TKey key, TValue value)
        {
            objectsTable.Add(key, value);
            objectsArray.Add(new KeyValuePair<TKey, TValue>(key, value));
        }

        /// <summary>
        ///   Removes the element with the specified key from the
        ///   <see cref="IDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <returns>
        ///   true if the element is successfully removed; otherwise, false.
        ///   This method also returns false if <paramref name="key"/> was not
        ///   found in the original <see cref="IDictionary{TKey,TValue}"/>.
        /// </returns>
        /// <param name="key">
        ///   The key of the element to remove.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///   The <see cref="IDictionary{TKey,TValue}"/> is read-only.
        /// </exception>
        public bool Remove(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            int index = IndexOfKey(key);
            if (index < 0)
                return false;

            objectsTable.Remove(key);
            objectsArray.RemoveAt(index);
            return true;
        }

        #endregion

        #region Implementation of IDictionary

        bool IDictionary.IsFixedSize
        {
            get { return false; }
        }

        bool IDictionary.IsReadOnly
        {
            get { return false; }
        }

        ICollection IDictionary.Keys
        {
            get { return new KeyCollection(this); }
        }

        ICollection IDictionary.Values
        {
            get { return new ValueCollection(this); }
        }

        object IDictionary.this[object key]
        {
            get { return ((IDictionary)objectsTable)[key]; }
            set
            {
                if (key == null) {
                    throw new ArgumentNullException(nameof(key));
                }

                ThrowIfIllegalNull<TValue>(value, "value");

                try {
                    var realKey = (TKey)key;
                    try {
                        this[realKey] = (TValue)value;
                    } catch (InvalidCastException) {
                        ThrowWrongValueTypeArgumentException(value, typeof(TValue));
                    }
                } catch (InvalidCastException) {
                    ThrowWrongKeyTypeArgumentException(key, typeof(TKey));
                }
            }
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return new Enumerator(GetEnumerator());
        }

        bool IDictionary.Contains(object key)
        {
            if (!(key is TKey))
                return false;
            return ContainsKey((TKey)key);
        }

        void IDictionary.Add(object key, object value)
        {
            ThrowIfIllegalNull<TValue>(value, "value");

            try {
                var realKey = (TKey)key;
                try {
                    Add(realKey, (TValue)value);
                } catch (InvalidCastException) {
                    ThrowWrongValueTypeArgumentException(value, typeof(TValue));
                }
            } catch (InvalidCastException) {
                ThrowWrongKeyTypeArgumentException(key, typeof(TKey));
            }
        }

        void IDictionary.Remove(object key)
        {
            if (key is TKey)
                Remove((TKey)key);
        }

        #endregion

        #region Implementation of IOrderedDictionary

        object IOrderedDictionary.this[int index]
        {
            get { return GetAt(index); }
            set
            {
                try {
                    SetAt(index, (TValue)value);
                } catch (InvalidCastException) {
                    ThrowWrongValueTypeArgumentException(value, typeof(TValue));
                }
            }
        }

        IDictionaryEnumerator IOrderedDictionary.GetEnumerator()
        {
            return new Enumerator(GetEnumerator());
        }

        void IOrderedDictionary.Insert(int index, object key, object value)
        {
            ThrowIfIllegalNull<TValue>(value, "value");

            try {
                var realKey = (TKey)key;
                try {
                    Insert(index, realKey, (TValue)value);
                } catch (InvalidCastException) {
                    ThrowWrongValueTypeArgumentException(value, typeof(TValue));
                }
            } catch (InvalidCastException) {
                ThrowWrongKeyTypeArgumentException(key, typeof(TKey));
            }
        }

        void IOrderedDictionary.RemoveAt(int index)
        {
            // Requires manual checking because IOrderedDictionary has no contract.
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            KeyValuePair<TKey, TValue> entry = objectsArray[index];
            objectsArray.RemoveAt(index);
            objectsTable.Remove(entry.Key);
        }

        #endregion

        #region Implementation of ICollection

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get
            {
                if (syncRoot == null)
                    Interlocked.CompareExchange<object>(ref syncRoot, new object(), null);
                return syncRoot;
            }
        }

        /// <summary>
        ///   Copies the elements of the <see cref="ICollection"/> to an
        ///   <see cref="Array"/>, starting at a particular <see cref="Array"/>
        ///   index.
        /// </summary>
        /// <param name="array">
        ///   The one-dimensional <see cref="Array"/> that is the destination of
        ///   the elements copied from <see cref="ICollection"/>. The <see cref="Array"/>
        ///   must have zero-based indexing.
        /// </param>
        /// <param name="index">
        ///   The zero-based index in <paramref name="array"/> at which copying
        ///   begins.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="array"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="index"/> is less than zero.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="array"/> is multidimensional. -or- The number of
        ///   elements in the source <see cref="ICollection"/> is greater than
        ///   the available space from <paramref name="index"/> to the end of
        ///   the destination <paramref name="array"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   The type of the source <see cref="ICollection"/> cannot be cast
        ///   automatically to the type of the destination <paramref name="array"/>.
        /// </exception>
        /// <filterpriority>2</filterpriority>
        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)objectsArray).CopyTo(array, index);
        }

        #endregion

        #region Implementation of ICollection<KeyValuePair<TKey, TValue>>

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get { return false; }
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)objectsTable).Contains(item);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)objectsArray).CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            int index = IndexOfKey(item.Key);
            if (index < 0 || index >= Count)
                return false;

            if ((index < 0)
                || !EqualityComparer<TValue>.Default.Equals(objectsArray[index].Value, item.Value))
                return false;

            objectsArray.RemoveAt(index);
            objectsTable.Remove(objectsArray[index].Key);
            return true;
        }

        #endregion

        #region Implementation of IEnumerable

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Implementation of ISerializable

        /// <summary>
        ///   Initializes a new instance of the
        ///   <see cref="OrderedDictionary{TKey,TValue}"/> class with serialized
        ///   data.
        /// </summary>
        /// <param name="info">
        ///   A <see cref="SerializationInfo"/> object containing the information
        ///   required to serialize the <see cref="OrderedDictionary{TKey,TValue}"/>.
        /// </param>
        /// <param name="context">
        ///   A <see cref="StreamingContext"/> structure containing the source
        ///   and destination of the serialized stream associated with the
        ///   <see cref="OrderedDictionary{TKey,TValue}"/>.
        /// </param>
        /// <remarks>
        ///   This constructor is called during deserialization to reconstitute
        ///   an object transmitted over a stream.
        /// </remarks>
        private OrderedDictionary(SerializationInfo info, StreamingContext context)
        {
            Contract.Requires<ArgumentNullException>(info != null);

            comparer = info.GetValue<IEqualityComparer<TKey>>("KeyComparer");
            var objArray = info.GetValue<KeyValuePair<TKey, TValue>[]>("ArrayList");

            if (objArray != null) {
                int capacity = objArray.Length;
                objectsTable = new Dictionary<TKey, TValue>(capacity, comparer);
                objectsArray = new List<KeyValuePair<TKey, TValue>>(capacity);
                foreach (KeyValuePair<TKey, TValue> entry in objArray) {
                    objectsArray.Add(entry);
                    objectsTable.Add(entry.Key, entry.Value);
                }
            } else {
                objectsTable = new Dictionary<TKey, TValue>(comparer);
                objectsArray = new List<KeyValuePair<TKey, TValue>>();
            }
        }

        /// <summary>
        ///   Populates a <see cref="SerializationInfo"/> with the data needed
        ///   to serialize the target object.
        /// </summary>
        /// <param name="info">
        ///   The <see cref="SerializationInfo"/> to populate with data.
        /// </param>
        /// <param name="context">
        ///   The destination (see <see cref="StreamingContext"/>) for this
        ///   serialization.
        /// </param>
        /// <exception cref="SecurityException">
        ///   The caller does not have the required permission.
        /// </exception>
        [SecurityCritical]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            var array = new KeyValuePair<TKey, TValue>[Count];
            objectsArray.CopyTo(array);
            info.AddValue("KeyComparer", comparer);
            info.AddValue("ArrayList", array);
        }

        #endregion

        private static void ThrowIfIllegalNull<T>(object value, string argumentName)
        {
            if (value == null && default(T) != null)
                throw new ArgumentNullException(argumentName);
        }

        private int IndexOfKey(TKey key)
        {
            for (int i = 0; i < objectsArray.Count; ++i) {
                var entry = objectsArray[i];
                TKey entryKey = entry.Key;
                if (comparer != null) {
                    if (comparer.Equals(entryKey, key))
                        return i;
                } else if (entryKey.Equals(key)) {
                    return i;
                }
            }

            return -1;
        }

        private static void ThrowWrongKeyTypeArgumentException(object key, Type targetType)
        {
            string message = string.Format(
                CultureInfo.CurrentCulture, Strings.Arg_WrongType, key, targetType);
            throw new ArgumentException(message, nameof(key));
        }

        private static void ThrowWrongValueTypeArgumentException(object value, Type targetType)
        {
            string message = string.Format(
                CultureInfo.CurrentCulture, Strings.Arg_WrongType, value, targetType);
            throw new ArgumentException(message, nameof(value));
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(objectsArray.Count == objectsTable.Count);
        }

        private sealed class Enumerator : IDictionaryEnumerator
        {
            private readonly IEnumerator<KeyValuePair<TKey, TValue>> enumerator;

            public Enumerator(IEnumerator<KeyValuePair<TKey, TValue>> enumerator)
            {
                this.enumerator = enumerator;
            }

            public object Current
            {
                get { return Entry; }
            }

            public object Key
            {
                get { return enumerator.Current.Key; }
            }

            public object Value
            {
                get { return enumerator.Current.Value; }
            }

            public DictionaryEntry Entry
            {
                get
                {
                    var current = enumerator.Current;
                    return new DictionaryEntry(current.Key, current.Value);
                }
            }

            public bool MoveNext()
            {
                return enumerator.MoveNext();
            }

            public void Reset()
            {
                enumerator.Reset();
            }
        }

        [Serializable]
        [DebuggerDisplay("Count = {Count}")]
        [DebuggerTypeProxy(typeof(CollectionDebuggerProxy<>))]
        private sealed class KeyCollection : IList<TKey>, ICollection
        {
            private readonly OrderedDictionary<TKey, TValue> dictionary;

            public KeyCollection(OrderedDictionary<TKey, TValue> dictionary)
            {
                if (dictionary == null)
                    throw new ArgumentNullException(nameof(dictionary));

                this.dictionary = dictionary;
            }

            public int Count
            {
                get { return dictionary.Count; }
            }

            bool ICollection<TKey>.IsReadOnly
            {
                get { return true; }
            }

            bool ICollection.IsSynchronized
            {
                get { return false; }
            }

            object ICollection.SyncRoot
            {
                get { return ((ICollection)dictionary).SyncRoot; }
            }

            public IEnumerator<TKey> GetEnumerator()
            {
                return new Enumerator(dictionary);
            }

            public bool Contains(TKey item)
            {
                return dictionary.ContainsKey(item);
            }

            public void CopyTo(TKey[] array, int index)
            {
                foreach (var entry in dictionary.objectsArray) {
                    Contract.Assume(index < array.Length);
                    array[index++] = entry.Key;
                }
            }

            void ICollection<TKey>.Clear()
            {
                throw new NotSupportedException();
            }

            void ICollection<TKey>.Add(TKey item)
            {
                throw new NotSupportedException();
            }

            bool ICollection<TKey>.Remove(TKey item)
            {
                throw new NotSupportedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            void ICollection.CopyTo(Array array, int index)
            {
                var strongArray = array as TKey[];
                if (strongArray != null) {
                    CopyTo(strongArray, index);
                    return;
                }

                var objArray = array as object[];
                if (objArray == null)
                    throw new ArgumentException(Strings.Arg_InvalidArrayType);

                try {
                    foreach (var entry in dictionary.objectsArray)
                        objArray[index++] = entry.Key;
                } catch (ArrayTypeMismatchException) {
                    throw new ArgumentException(Strings.Arg_InvalidArrayType);
                }
            }

            public int IndexOf(TKey item)
            {
                return dictionary.objectsArray.FindIndex(
                    p => EqualityComparer<TKey>.Default.Equals(p.Key, item));
            }

            public void Insert(int index, TKey item)
            {
                throw new NotSupportedException();
            }

            public void RemoveAt(int index)
            {
                throw new NotSupportedException();
            }

            public TKey this[int index]
            {
                get { return dictionary.objectsArray[index].Key; }
                set { throw new NotSupportedException(); }
            }

            [Serializable]
            [StructLayout(LayoutKind.Sequential)]
            private struct Enumerator : IEnumerator<TKey>
            {
                private readonly IEnumerator<KeyValuePair<TKey, TValue>> arrayEnumerator;

                internal Enumerator(OrderedDictionary<TKey, TValue> dictionary)
                {
                    arrayEnumerator = dictionary.objectsArray.GetEnumerator();
                }

                public TKey Current
                {
                    get { return arrayEnumerator.Current.Key; }
                }

                object IEnumerator.Current
                {
                    get { return Current; }
                }

                public void Dispose()
                {
                    arrayEnumerator.Dispose();
                }

                public bool MoveNext()
                {
                    return arrayEnumerator.MoveNext();
                }

                void IEnumerator.Reset()
                {
                    arrayEnumerator.Reset();
                }
            }
        }

        [Serializable]
        [DebuggerDisplay("Count = {Count}")]
        [DebuggerTypeProxy(typeof(CollectionDebuggerProxy<>))]
        private sealed class ValueCollection : IList<TValue>, ICollection
        {
            private readonly OrderedDictionary<TKey, TValue> dictionary;

            public ValueCollection(OrderedDictionary<TKey, TValue> dictionary)
            {
                if (dictionary == null)
                    throw new ArgumentNullException(nameof(dictionary));

                this.dictionary = dictionary;
            }

            public int Count
            {
                get { return dictionary.Count; }
            }

            bool ICollection<TValue>.IsReadOnly
            {
                get { return true; }
            }

            bool ICollection.IsSynchronized
            {
                get { return false; }
            }

            object ICollection.SyncRoot
            {
                get { return ((ICollection)dictionary).SyncRoot; }
            }

            public IEnumerator<TValue> GetEnumerator()
            {
                return new Enumerator(dictionary);
            }

            public bool Contains(TValue item)
            {
                return dictionary.ContainsValue(item);
            }

            public void CopyTo(TValue[] array, int index)
            {
                foreach (var entry in dictionary.objectsArray) {
                    Contract.Assume(index < array.Length);
                    array[index++] = entry.Value;
                }
            }

            void ICollection.CopyTo(Array array, int index)
            {
                var strongArray = array as TValue[];
                if (strongArray != null) {
                    CopyTo(strongArray, index);
                    return;
                }

                var objArray = array as object[];
                if (objArray == null)
                    throw new ArgumentException(Strings.Arg_InvalidArrayType);

                try {
                    foreach (var entry in dictionary.objectsArray)
                        objArray[index++] = entry.Value;
                } catch (ArrayTypeMismatchException) {
                    throw new ArgumentException(Strings.Arg_InvalidArrayType);
                }
            }

            void ICollection<TValue>.Clear()
            {
                throw new NotSupportedException();
            }

            void ICollection<TValue>.Add(TValue item)
            {
                throw new NotSupportedException();
            }

            bool ICollection<TValue>.Remove(TValue item)
            {
                throw new NotSupportedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public int IndexOf(TValue item)
            {
                return dictionary.objectsArray.FindIndex(
                    p => EqualityComparer<TValue>.Default.Equals(p.Value, item));
            }

            public void Insert(int index, TValue item)
            {
                throw new NotSupportedException();
            }

            public void RemoveAt(int index)
            {
                throw new NotSupportedException();
            }

            public TValue this[int index]
            {
                get { return dictionary.objectsArray[index].Value; }
                set { throw new NotSupportedException(); }
            }

            [Serializable]
            [StructLayout(LayoutKind.Sequential)]
            private struct Enumerator : IEnumerator<TValue>
            {
                private readonly IEnumerator<KeyValuePair<TKey, TValue>> arrayEnumerator;

                internal Enumerator(OrderedDictionary<TKey, TValue> dictionary)
                {
                    arrayEnumerator = dictionary.objectsArray.GetEnumerator();
                }

                public TValue Current
                {
                    get { return arrayEnumerator.Current.Value; }
                }

                object IEnumerator.Current
                {
                    get { return Current; }
                }

                public void Dispose()
                {
                    arrayEnumerator.Dispose();
                }

                public bool MoveNext()
                {
                    return arrayEnumerator.MoveNext();
                }

                void IEnumerator.Reset()
                {
                    arrayEnumerator.Reset();
                }
            }
        }

        [ExcludeFromCodeCoverage]
        private sealed class CollectionDebuggerProxy<T>
        {
            private readonly ICollection<T> collection;

            public CollectionDebuggerProxy(ICollection<T> collection)
            {
                Contract.Requires<ArgumentNullException>(collection != null);
                this.collection = collection;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public T[] Items
            {
                get
                {
                    var items = new T[collection.Count];
                    collection.CopyTo(items, 0);
                    return items;
                }
            }
        }

        [ExcludeFromCodeCoverage]
        private sealed class DebuggerProxy
        {
            private readonly IDictionary<TKey, TValue> dictionary;

            public DebuggerProxy(IDictionary<TKey, TValue> dictionary)
            {
                Contract.Requires<ArgumentNullException>(dictionary != null);
                this.dictionary = dictionary;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public KeyValuePair<TKey, TValue>[] Items
            {
                get
                {
                    var items = new KeyValuePair<TKey, TValue>[dictionary.Count];
                    dictionary.CopyTo(items, 0);
                    return items;
                }
            }
        }
    }
}
