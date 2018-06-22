namespace EventTraceKit.VsExtension.Threading
{
    using System;
    using System.Windows.Threading;

    public class PeriodicCumulativeProgress : Progress<ProgressDelta>, IDisposable
    {
        private readonly object mutex = new object();
        private readonly DispatcherTimer timer;
        private ProgressDelta lastValue;

        public PeriodicCumulativeProgress(TimeSpan interval, Action<ProgressDelta> handler)
            : base(handler)
        {
            timer = new DispatcherTimer(DispatcherPriority.Background);
            timer.Interval = interval;
            timer.Tick += OnTick;
            timer.Start();
        }

        protected override void OnReport(ProgressDelta value)
        {
            lock (mutex)
                lastValue = new ProgressDelta(lastValue.Delta + value.Delta, value.Total);
        }

        private void OnTick(object sender, EventArgs e)
        {
            ProgressDelta value;
            lock (mutex)
                value = lastValue;

            base.OnReport(value);
        }

        public void Dispose()
        {
            timer.Stop();
        }
    }

    public struct ProgressDelta
    {
        public ProgressDelta(int delta, int total = 0)
        {
            Delta = delta;
            Total = total;
        }

        public bool IsIndeterminate => Total == 0;
        public int Delta { get; }
        public int Total { get; }

        public int Completion(int current)
        {
            return (int)Math.Round(current * 100.0 / Total);
        }
    }
}
