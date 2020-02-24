namespace EventTraceKit.EventTracing.Tests.Compilation.TestSupport
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Xunit;
    using Xunit.Sdk;

    public static class SequenceAssert
    {
        public static void SequenceEqual(
            IEnumerable expected, IEnumerable actual, IEqualityComparer comparer = null)
        {
            comparer ??= new AssertEqualityComparerAdapter<object>(new XunitAssertEqualityComparer<object>());

            BufferedEnumerator enumeratorActual = null;
            BufferedEnumerator enumeratorExpected = null;
            try {
                enumeratorActual = new BufferedEnumerator(actual);
                enumeratorExpected = new BufferedEnumerator(expected);

                for (int index = 0; ; ++index) {
                    var hasNextX = enumeratorActual.MoveNext();
                    var hasNextY = enumeratorExpected.MoveNext();

                    if (!hasNextX || !hasNextY) {
                        if (hasNextX != hasNextY)
                            throw new SequenceEqualException(enumeratorExpected.Finish(), enumeratorActual.Finish(), index, index);
                        break;
                    }

                    if (!comparer.Equals(enumeratorActual.Current, enumeratorExpected.Current)) {
                        throw new SequenceEqualException(enumeratorExpected.Finish(), enumeratorActual.Finish(), index, index);
                    }
                }
            } finally {
                if (enumeratorActual is IDisposable dx)
                    dx.Dispose();
                if (enumeratorExpected is IDisposable dy)
                    dy.Dispose();
            }
        }
    }

    internal class XunitAssertEqualityComparer<T> : IEqualityComparer<T>
    {
        private static readonly Type assertEqualityComparerType;

        private readonly IEqualityComparer<T> innerComparer;

        static XunitAssertEqualityComparer()
        {
            assertEqualityComparerType = typeof(Assert).Assembly.GetType(
                "Xunit.Sdk.AssertEqualityComparer`1", true);
        }

        public XunitAssertEqualityComparer(IEqualityComparer innerComparer = null)
        {
            this.innerComparer = (IEqualityComparer<T>)Activator.CreateInstance(
                assertEqualityComparerType.MakeGenericType(typeof(T)),
                innerComparer);
        }

        public bool Equals(T x, T y)
        {
            return innerComparer.Equals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return innerComparer.GetHashCode(obj);
        }
    }

    internal class AssertEqualityComparerAdapter<T> : IEqualityComparer
    {
        private readonly IEqualityComparer<T> innerComparer;

        public AssertEqualityComparerAdapter(IEqualityComparer<T> innerComparer)
        {
            this.innerComparer = innerComparer;
        }

        public new bool Equals(object x, object y)
        {
            return innerComparer.Equals((T)x, (T)y);
        }

        public int GetHashCode(object obj)
        {
            return innerComparer.GetHashCode((T)obj);
        }
    }

    internal class BufferedEnumerator : IEnumerator
    {
        private readonly IEnumerable sequence;
        private readonly IEnumerator enumerator;
        private readonly SparseList<object> buffer;

        public BufferedEnumerator(IEnumerable sequence, int prefixLength = 10, int suffixLength = 20)
        {
            this.sequence = sequence;
            enumerator = sequence.GetEnumerator();
            buffer = new SparseList<object>(prefixLength, suffixLength);
        }

        public object Current => enumerator.Current;

        public bool MoveNext()
        {
            if (!enumerator.MoveNext())
                return false;

            buffer.Add(enumerator.Current);
            return true;
        }

        public void Reset()
        {
            enumerator.Reset();
            buffer.Reset();
        }

        public BufferedEnumerable Finish()
        {
            buffer.FreezeWindow();
            while (enumerator.MoveNext())
                buffer.Add(enumerator.Current);

            return new BufferedEnumerable(sequence, buffer);
        }
    }

    internal class SparseList<T>
    {
        private readonly T[] window;
        private int windowStart = 0;
        private int windowEnd = 0;
        private bool frozen;

        public SparseList(int prefixLength, int suffixLength)
        {
            PrefixLength = prefixLength;
            SuffixLength = suffixLength;
            window = new T[prefixLength + 1 + suffixLength];
        }

        public int PrefixLength { get; }
        public int SuffixLength { get; }

        public int Count { get; private set; }
        public int CurrentIndex { get; private set; } = -1;

        public object this[int index]
        {
            get
            {
                if (index < windowStart || index >= windowEnd)
                    throw new ArgumentOutOfRangeException(nameof(window));
                return window[index % window.Length];
            }
        }

        public void Add(T item)
        {
            if (Count < CurrentIndex + 1 + SuffixLength)
                window[Count % window.Length] = item;

            if (!frozen)
                ++CurrentIndex;

            ++Count;
            windowStart = Math.Max(CurrentIndex - PrefixLength, 0);
            windowEnd = Math.Min(CurrentIndex + 1 + SuffixLength, Count);
        }

        public void Reset()
        {
            Count = 0;
            CurrentIndex = -1;
            windowStart = 0;
            windowEnd = 0;
            frozen = false;
            for (int i = 0; i < window.Length; ++i)
                window[i] = default;
        }

        public void FreezeWindow()
        {
            frozen = true;
        }
    }

    internal class BufferedEnumerable
    {
        public BufferedEnumerable(IEnumerable sequence, SparseList<object> values)
        {
            Sequence = sequence;
            Values = values;
        }

        public IEnumerable Sequence { get; }
        public SparseList<object> Values { get; }

        public int Count => Values.Count;
        public object this[int index] => Values[index];

        public int GetStartIndex(int position)
        {
            if (position != Values.CurrentIndex)
                throw new ArgumentException();
            return Math.Max(position - Values.PrefixLength, 0);
        }

        public int GetEndIndex(int position)
        {
            if (position != Values.CurrentIndex)
                throw new ArgumentException();
            return Math.Min(position + Values.SuffixLength + 1, Values.Count);
        }
    }
}
