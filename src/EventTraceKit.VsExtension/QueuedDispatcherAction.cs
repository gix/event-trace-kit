namespace EventTraceKit.VsExtension
{
    using System;
    using System.Windows.Threading;

    internal sealed class QueuedDispatcherAction<T>
    {
        private readonly Dispatcher dispatcher;
        private readonly Action update;
        private readonly Action<T> action;
        private T queuedValue;
        private bool isQueued;

        public QueuedDispatcherAction(Dispatcher dispatcher, Action<T> action)
        {
            this.dispatcher = dispatcher;
            this.action = action;
            update = Update;
        }

        public void Queue(T value)
        {
            queuedValue = value;

            if (!isQueued) {
                isQueued = true;
                dispatcher.InvokeAsync(update, DispatcherPriority.Input);
            }
        }

        private void Update()
        {
            isQueued = false;
            action(queuedValue);
        }
    }
}
