namespace EventTraceKit.VsExtension
{
    using System;
    using System.Windows.Threading;

    internal sealed class QueuedDispatcherAction
    {
        private readonly Dispatcher dispatcher;
        private readonly Action update;
        private readonly Action<int> action;
        private int queuedValue;
        private bool isQueued;

        public QueuedDispatcherAction(Dispatcher dispatcher, Action<int> action)
        {
            this.dispatcher = dispatcher;
            this.action = action;
            update = Update;
        }

        public void Queue(int value)
        {
            queuedValue = value;

            if (!isQueued) {
                isQueued = true;
                dispatcher.BeginInvoke(update, DispatcherPriority.Input);
            }
        }

        private void Update()
        {
            isQueued = false;
            action(queuedValue);
        }
    }
}
