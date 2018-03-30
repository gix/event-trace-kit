namespace EventTraceKit.VsExtension.Threading
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Threading;
    using EventTraceKit.VsExtension.Views;
    using Microsoft.Windows.TaskDialogs;
    using Microsoft.Windows.TaskDialogs.Controls;

    public static class TaskExtensions
    {
        public static async Task RunWithProgress(
            Func<Task> action, string caption, CancellationTokenSource cts = null)
        {
            await Task.Run(action).WaitWithProgress(caption, cts);
        }

        public static async Task RunWithProgress(
            Action action, string caption, CancellationTokenSource cts = null)
        {
            await Task.Run(action).WaitWithProgress(caption, cts);
        }

        public static async Task WaitWithProgress(
            this Task task, string caption, CancellationTokenSource cts = null)
        {
            var dialogCts = new CancellationTokenSource();

            var dialog = new TaskDialog {
                Caption = caption,
                Content = string.Empty
            };
            if (cts != null) {
                dialog.IsCancelable = true;
                dialog.CommonButtons = TaskDialogButtons.Cancel;
            }
            dialog.Controls.Add(new TaskDialogProgressBar { IsIndeterminate = true });

            dialog.Closing += (s, e) => {
                if (e.Button == TaskDialogButtonId.Cancel) {
                    dialogCts.Cancel();
                    cts?.Cancel();
                }
            };

            await Task.WhenAny(task, dialog.ShowModalDelayed(dialogCts.Token));

            dialogCts.Cancel();
            if (dialog.IsShown)
                dialog.Abort();
        }

        public static async Task<T> RunWithProgress<T>(
            this TaskFactory taskFactory, string caption,
            Func<CancellationToken, IProgress<ProgressState>, T> action)
        {
            var cts = (CancellationTokenSource)null;
            var dialogCts = new CancellationTokenSource();

            var dialog = new TaskDialog {
                Caption = caption,
                Content = string.Empty
            };
            if (cts != null) {
                dialog.IsCancelable = true;
                dialog.CommonButtons = TaskDialogButtons.Cancel;
            }

            dialog.Controls.Add(new TaskDialogProgressBar(0, 100, 0));

            dialog.Closing += (s, e) => {
                if (e.Button == TaskDialogButtonId.Cancel) {
                    dialogCts.Cancel();
                    cts?.Cancel();
                }
            };

            IProgress<ProgressState> progress = new PeriodicProgress<ProgressState>(
                TimeSpan.FromMilliseconds(100),
                x => {
                    dialog.ProgressBar.Value = x.Completion(x.Delta);
                    dialog.Content = $"{x.Delta}/{x.Total}";
                });

            var dialogTask = ShowModalDelayed(dialog, dialogCts.Token);

            var result = await taskFactory.StartNew(() => action(dialogCts.Token, progress), dialogCts.Token);

            dialogCts.Cancel();
            if (dialog.IsShown)
                dialog.Abort();

            await dialogTask.IgnoreCancellation();

            return result;
        }

        public static Task ShowModalDelayed(
            this TaskDialog dialog, CancellationToken cancellationToken)
        {
            var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            var delay = TimeSpan.FromMilliseconds(250);

            return Task.Delay(delay, cancellationToken)
                .ContinueWith(t => {
                    if (!cancellationToken.IsCancellationRequested)
                        dialog.ShowModal();
                }, cancellationToken, TaskContinuationOptions.None, scheduler)
                .IgnoreCancellation();
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
