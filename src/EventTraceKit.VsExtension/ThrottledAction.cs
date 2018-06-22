namespace EventTraceKit.VsExtension
{
    using System;
    using System.Threading;

    internal sealed class ThrottledAction
    {
        private readonly Timer timer;
        private readonly Action action;
        private readonly TimeSpan timeout;

        // 0: free, 1: blocked, 2+: blocked and run again after timeout
        private int state;

        public ThrottledAction(TimeSpan timeout, Action action)
        {
            this.action = action ?? throw new ArgumentNullException(nameof(action));
            this.timeout = timeout;
            timer = new Timer(OnTimer);
        }

        public void Invoke()
        {
            if (Interlocked.Add(ref state, 1) == 1)
                InvokeLocked();
        }

        private void OnTimer(object _)
        {
            if (Interlocked.Decrement(ref state) > 0) {
                Interlocked.Exchange(ref state, 1);
                InvokeLocked();
            }
        }

        private void InvokeLocked()
        {
            try {
                action();
            } finally {
                timer.Change(timeout, Timeout.InfiniteTimeSpan);
            }
        }
    }
}
