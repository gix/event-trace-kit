namespace EventTraceKit.VsExtension.Utilities
{
    using System;
    using System.Threading;

    internal sealed class ValueCache<T>
    {
        private readonly Func<T> factory;
        private Lazy<T> lazyValue;

        public ValueCache(Func<T> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            this.factory = factory;
            Clear();
        }

        public T Value => lazyValue.Value;

        public void Clear()
        {
            if (lazyValue == null || lazyValue.IsValueCreated)
                lazyValue = new Lazy<T>(factory, LazyThreadSafetyMode.None);
        }
    }
}
