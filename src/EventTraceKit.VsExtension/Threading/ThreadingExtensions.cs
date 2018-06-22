namespace EventTraceKit.VsExtension.Threading
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Threading;
    using EventTraceKit.VsExtension.Views;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Threading;
    using Microsoft.Windows.TaskDialogs;
    using Microsoft.Windows.TaskDialogs.Controls;
    using Task = System.Threading.Tasks.Task;

    public static class ThreadingExtensions
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
            string caption, string info,
            Func<CancellationToken, IProgress<ProgressDelta>, T> action,
            CancellationToken? cancellationToken = null,
            TimeSpan? progressUpdateInterval = null)
        {
            var dialogCts = new CancellationTokenSource();
            var linkedCts = cancellationToken != null
                ? CancellationTokenSource.CreateLinkedTokenSource(dialogCts.Token, cancellationToken.Value)
                : dialogCts;

            var dialog = new TaskDialog {
                Caption = caption,
                Content = string.Empty
            };
            if (cancellationToken != null) {
                dialog.IsCancelable = true;
                dialog.CommonButtons = TaskDialogButtons.Cancel;
            }

            dialog.Controls.Add(new TaskDialogProgressBar(0, 100, 0));

            dialog.Closing += (s, e) => {
                if (e.Button == TaskDialogButtonId.Cancel)
                    linkedCts.Cancel();
            };

            var progress = new PeriodicCumulativeProgress(
                progressUpdateInterval ?? TimeSpan.FromMilliseconds(100),
                x => {
                    if (x.IsIndeterminate) {
                        dialog.ProgressBar.IsIndeterminate = true;
                        dialog.Content = $"{x.Delta} {info}";
                    } else {
                        dialog.ProgressBar.IsIndeterminate = false;
                        dialog.ProgressBar.Value = x.Completion(x.Delta);
                        dialog.Content = $"{x.Delta}/{x.Total} {info}";
                    }
                });

            using (dialogCts)
            using (cancellationToken != null ? linkedCts : null)
            using (progress) {
                var dialogTask = ShowModalDelayed(dialog, dialogCts.Token);

                var result = await Task.Run(
                    () => action(linkedCts.Token, progress), linkedCts.Token);

                dialogCts.Cancel();
                if (dialog.IsShown)
                    dialog.Abort();

                await dialogTask.IgnoreCancellation();

                return result;
            }
        }

        public static async Task ShowModalDelayed(
            this TaskDialog dialog, CancellationToken cancellationToken, TimeSpan? delay = null)
        {
            var dispatcher = Dispatcher.FromThread(Thread.CurrentThread);
            if (dispatcher == null)
                throw new InvalidOperationException("Current thread has no dispatcher");

            try {
                await Delay(dispatcher, delay ?? TimeSpan.FromMilliseconds(250), cancellationToken);
                if (!cancellationToken.IsCancellationRequested)
                    dialog.ShowModal();
            } catch (OperationCanceledException) {
            }
        }

        private static Task Delay(Dispatcher dispatcher, TimeSpan delay, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);
            if (delay == TimeSpan.Zero)
                return Task.CompletedTask;

            var promise = new DelayPromise(dispatcher, cancellationToken, delay);
            return promise.Task;
        }

        private sealed class DelayPromise
        {
            private readonly CancellationToken token;
            private readonly DispatcherTimer timer;
            private readonly TaskCompletionSource<byte> tcs =
                new TaskCompletionSource<byte>();
            private CancellationTokenRegistration registration;

            internal DelayPromise(Dispatcher dispatcher, CancellationToken token, TimeSpan delay)
            {
                this.token = token;

                if (token.CanBeCanceled) {
                    registration = token.Register(
                        state => ((DelayPromise)state).Complete(), this);
                }

                timer = new DispatcherTimer(DispatcherPriority.Send, dispatcher);
                timer.Interval = delay;
                timer.Tick += (s, e) => Complete();
                timer.Start();
            }

            public Task Task => tcs.Task;

            private void Complete()
            {
                bool setSucceeded;
                if (token.IsCancellationRequested)
                    setSucceeded = tcs.TrySetCanceled(token);
                else
                    setSucceeded = tcs.TrySetResult(0);

                if (setSucceeded) {
                    timer?.Stop();
                    registration.Dispose();
                }
            }
        }

        public static Task IgnoreCancellation(this Task task)
        {
            return task.ContinueWith(
                t => { },
                CancellationToken.None,
                TaskContinuationOptions.NotOnFaulted,
                TaskScheduler.Default);
        }

        public static AsyncLazy<T> CreateAsyncLazy<T>(Func<T> factory)
        {
            return new AsyncLazy<T>(() => Task.Run(factory), ThreadHelper.JoinableTaskFactory);
        }
    }
}
