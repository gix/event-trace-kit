namespace EventTraceKit.VsExtension.Threading
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Interop;
    using System.Windows.Threading;
    using Microsoft.Windows.TaskDialogs;
    using Microsoft.Windows.TaskDialogs.Controls;

    public static class TaskExtensions
    {
        public static async Task<T> RunWithProgress<T>(
            this TaskFactory taskFactory, string caption, Window owner,
            Func<CancellationToken, IProgress<ProgressState>, T> action)
        {
            var cts = new CancellationTokenSource();

            var dialog = new TaskDialog {
                Caption = caption,
                Content = string.Empty
            };
            if (owner != null)
                dialog.OwnerWindow = new HandleRef(
                    owner, new WindowInteropHelper(owner).Handle);

            dialog.Controls.Add(new TaskDialogButton(
                TaskDialogButtonId.Cancel, "Cancel", (s, e) => cts.Cancel()));
            dialog.Controls.Add(new TaskDialogProgressBar(0, 100, 0));

            IProgress<ProgressState> progress = new PeriodicProgress<ProgressState>(
                TimeSpan.FromMilliseconds(100),
                x => {
                    dialog.ProgressBar.Value = x.Completion(x.Delta);
                    dialog.Content = $"{x.Delta}/{x.Total}";
                });

            var currentScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            var delay = TimeSpan.FromMilliseconds(250);

            var dialogTask = Task.Delay(delay, cts.Token)
                .ContinueWith(t => {
                    if (!cts.IsCancellationRequested)
                        dialog.Show();
                }, cts.Token, TaskContinuationOptions.None, currentScheduler);

            var result = await taskFactory.StartNew(() => action(cts.Token, progress), cts.Token);
            cts.Cancel();
            if (dialog.IsShown)
                dialog.Close(TaskDialogResult.Close);

            await dialogTask.IgnoreCancellation();

            return result;
        }

        public static Task IgnoreCancellation(this Task task)
        {
            return task.ContinueWith(t => { }, TaskContinuationOptions.NotOnFaulted);
        }
    }

    public struct ProgressState
    {
        public ProgressState(int delta, int total)
        {
            Delta = delta;
            Total = total;
        }

        public int Delta { get; }
        public int Total { get; }
        public int Completion(int current) => (int)Math.Round(current * 100.0 / Total);
    }

    public class PeriodicProgress<T> : Progress<ProgressState>
    {
        private readonly object mutex = new object();
        private readonly DispatcherTimer timer;
        private ProgressState lastValue;

        public PeriodicProgress(TimeSpan interval, Action<ProgressState> handler)
            : base(handler)
        {
            timer = new DispatcherTimer(DispatcherPriority.Background);
            timer.Interval = interval;
            timer.Tick += OnTick;
            timer.Start();
        }

        protected override void OnReport(ProgressState value)
        {
            lock (mutex)
                lastValue = new ProgressState(lastValue.Delta + value.Delta, value.Total);
        }

        private void OnTick(object sender, EventArgs e)
        {
            ProgressState value;
            lock (mutex)
                value = lastValue;

            base.OnReport(value);
        }
    }

    public class ThrottledProgress : Progress<ProgressState>
    {
        private readonly TimeSpan timeout;
        private DateTime lastReport = DateTime.MinValue;
        private int current;

        public ThrottledProgress(TimeSpan timeout, Action<ProgressState> handler)
            : base(handler)
        {
            this.timeout = timeout;
        }

        protected override void OnReport(ProgressState value)
        {
            var now = DateTime.UtcNow;
            var elapsed = now - lastReport;
            var localCurrent = Interlocked.Add(ref current, value.Delta);
            if (elapsed > timeout) {
                base.OnReport(new ProgressState(localCurrent, value.Total));
                lastReport = now;
            }
        }
    }
}
