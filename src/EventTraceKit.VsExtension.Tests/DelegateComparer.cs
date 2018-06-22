namespace EventTraceKit.VsExtension.Tests
{
    using System;
    using System.Collections.Generic;

    internal class DelegateComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> comparer;
        private readonly Func<T, int> hasher;

        public DelegateComparer(Func<T, T, bool> comparer, Func<T, int> hasher = null)
        {
            this.comparer = comparer;
            this.hasher = hasher;
        }

        public bool Equals(T x, T y)
        {
            return comparer(x, y);
        }

        public int GetHashCode(T obj)
        {
            return hasher != null ? hasher(obj) : 0;
        }
    }
}
