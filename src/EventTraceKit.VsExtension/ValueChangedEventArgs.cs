namespace EventTraceKit.VsExtension
{
    using System;
    using Utilities;

    public sealed class ValueChangedEventArgs<T> : EventArgs
    {
        private static readonly ObjectPool<ValueChangedEventArgs<T>> Pool;

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
            OldValue = oldValue;
            NewValue = newValue;
        }

        public T OldValue { get; private set; }
        public T NewValue { get; private set; }

        public static ValueChangedEventArgs<T> Get(T oldValue, T newValue)
        {
            ValueChangedEventArgs<T> args = Pool.Acquire();
            args.OldValue = oldValue;
            args.NewValue = newValue;
            return args;
        }

        public void Return()
        {
            OldValue = default;
            NewValue = default;
            Pool.Release(this);
        }
    }
}
