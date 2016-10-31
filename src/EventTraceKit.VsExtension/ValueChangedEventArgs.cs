namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Concurrent;

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
