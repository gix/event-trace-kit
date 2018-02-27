namespace EventTraceKit.VsExtension
{
    using System;
    using Utilities;

    public sealed class ValueChangedEventArgs<T> : EventArgs
    {
        private static readonly ObjectPool<ValueChangedEventArgs<T>> Pool;

        private T newValue;
        private T oldValue;

        static ValueChangedEventArgs()
        {
            Pool = new ObjectPool<ValueChangedEventArgs<T>>(
                () => new ValueChangedEventArgs<T>());
        }

        public ValueChangedEventArgs()
        {
        }

        public ValueChangedEventArgs(T oldValue, T newValue)
        {
            this.oldValue = oldValue;
            this.newValue = newValue;
        }

        public T NewValue => newValue;
        public T OldValue => oldValue;

        public static ValueChangedEventArgs<T> Get(T oldValue, T newValue)
        {
            ValueChangedEventArgs<T> args = Pool.Acquire();
            args.oldValue = oldValue;
            args.newValue = newValue;
            return args;
        }

        public void Return()
        {
            oldValue = default(T);
            newValue = default(T);
            Pool.Release(this);
        }
    }
}
