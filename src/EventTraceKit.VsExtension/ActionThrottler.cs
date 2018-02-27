namespace EventTraceKit.VsExtension
{
    using System;
    using System.Threading;

    internal class ActionThrottler
    {
        private readonly Timer timer;
        private readonly TimeSpan timeout;
        private readonly object mutex = new object();
        private Action nextAction;
        private bool blocked;

        public ActionThrottler(TimeSpan timeout)
        {
            this.timeout = timeout;
            timer = new Timer(OnTimer);
        }

        private void OnTimer(object state)
        {
            lock (mutex) {
                if (nextAction == null) {
                    blocked = false;
                    return;
                }

                Action action = nextAction;
                nextAction = null;
                RunInternal(action);
            }
        }

        public void Run(Action action)
        {
            lock (mutex) {
                if (blocked) {
                    nextAction = action;
                    return;
                }

                RunInternal(action);
            }
        }

        private void RunInternal(Action action)
        {
            blocked = true;
            action();
            timer.Change(timeout, TimeSpan.FromMilliseconds(-1));
        }
    }
}
