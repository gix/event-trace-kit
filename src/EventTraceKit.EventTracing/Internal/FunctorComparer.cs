namespace EventTraceKit.EventTracing.Internal
{
    using System;
    using System.Collections.Generic;

    internal sealed class FunctorComparer<T> : IComparer<T>
    {
        private readonly Comparison<T> comparison;

        public FunctorComparer(Comparison<T> comparison)
        {
            this.comparison = comparison;
        }

        public int Compare(T x, T y)
        {
            return comparison(x, y);
        }
    }
}
