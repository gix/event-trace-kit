namespace InstrManifestCompiler.Collections
{
    using System.Collections;
    using System.Collections.Generic;

    internal sealed class DictionaryEnumeratorAdapter<TKey, TValue> : IDictionaryEnumerator
    {
        private readonly IEnumerator<KeyValuePair<TKey, TValue>> enumerator;

        public DictionaryEnumeratorAdapter(IEnumerator<KeyValuePair<TKey, TValue>> enumerator)
        {
            this.enumerator = enumerator;
        }

        /// <summary>
        ///   Gets the current element in the collection.
        /// </summary>
        /// <returns>
        ///   The current element in the collection.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        ///   The enumerator is positioned before the first element of the collection
        ///   or after the last element.
        /// </exception>
        /// <filterpriority>2</filterpriority>
        public object Current
        {
            get { return Entry; }
        }

        /// <summary>
        ///   Gets the key of the current dictionary entry.
        /// </summary>
        /// <returns>
        ///   The key of the current element of the enumeration.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        ///   The <see cref="System.Collections.IDictionaryEnumerator"/> is
        ///   positioned before the first entry of the dictionary or after the
        ///   last entry.
        /// </exception>
        /// <filterpriority>2</filterpriority>
        public object Key
        {
            get { return enumerator.Current.Key; }
        }

        /// <summary>
        ///   Gets the value of the current dictionary entry.
        /// </summary>
        /// <returns>
        ///   The value of the current element of the enumeration.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        ///   The <see cref="System.Collections.IDictionaryEnumerator"/> is
        ///   positioned before the first entry of the dictionary or after the
        ///   last entry.
        /// </exception>
        /// <filterpriority>2</filterpriority>
        public object Value
        {
            get { return enumerator.Current.Value; }
        }

        /// <summary>
        ///   Gets both the key and the value of the current dictionary entry.
        /// </summary>
        /// <returns>
        ///   A <see cref="System.Collections.DictionaryEntry"/> containing
        ///   both the key and the value of the current dictionary entry.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        ///   The <see cref="System.Collections.IDictionaryEnumerator"/> is
        ///   positioned before the first entry of the dictionary or after the
        ///   last entry.
        /// </exception>
        /// <filterpriority>2</filterpriority>
        public DictionaryEntry Entry
        {
            get
            {
                var current = enumerator.Current;
                return new DictionaryEntry(current.Key, current.Value);
            }
        }

        /// <summary>
        ///   Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        ///   true if the enumerator was successfully advanced to the next element;
        ///   false if the enumerator has passed the end of the collection.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        ///   The collection was modified after the enumerator was created.
        /// </exception>
        /// <filterpriority>2</filterpriority>
        public bool MoveNext()
        {
            return enumerator.MoveNext();
        }

        /// <summary>
        ///   Sets the enumerator to its initial position, which is before the
        ///   first element in the collection.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        ///   The collection was modified after the enumerator was created.
        /// </exception>
        /// <filterpriority>2</filterpriority>
        public void Reset()
        {
            enumerator.Reset();
        }
    }
}
