namespace EventTraceKit.VsExtension
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    public class AsyncLazy<T>
    {
        private readonly Lazy<Task<T>> lazy;

        public AsyncLazy(
            Func<T> valueFactory,
            LazyThreadSafetyMode mode = LazyThreadSafetyMode.ExecutionAndPublication)
        {
            lazy = new Lazy<Task<T>>(() => Task.Run(valueFactory), mode);
        }

        public AsyncLazy(
            Func<Task<T>> taskFactory,
            LazyThreadSafetyMode mode = LazyThreadSafetyMode.ExecutionAndPublication)
        {
            lazy = new Lazy<Task<T>>(taskFactory, mode);
        }

        public bool IsValueCreated => lazy.IsValueCreated;

        public Task<T> Value => lazy.Value;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public TaskAwaiter<T> GetAwaiter()
        {
            return lazy.Value.GetAwaiter();
        }

        public override string ToString()
        {
            return lazy.ToString();
        }
    }

    public static class AsyncExtensions
    {
        public static Func<Task<T>> AsProvider<T>(this AsyncLazy<T> lazy)
        {
            return async () => await lazy;
        }
    }
}
