namespace EventManifestFramework.Collections
{
    using System;
    using System.Collections.Generic;

    internal sealed class SelectorComparer<TSource, TResult>
        : IComparer<TSource> where TResult : IComparable<TResult>
    {
        private readonly Func<TSource, TResult> selector;

        public SelectorComparer(Func<TSource, TResult> selector)
        {
            this.selector = selector;
        }

        public int Compare(TSource x, TSource y)
        {
            return selector(x).CompareTo(selector(y));
        }
    }
}
