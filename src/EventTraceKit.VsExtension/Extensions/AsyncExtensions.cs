namespace EventTraceKit.VsExtension.Extensions
{
    using System;
    using System.Threading.Tasks;
    using System.Windows.Threading;

    public static class AsyncExtensions
    {
        public static TaskScheduler ToTaskScheduler(
            this Dispatcher dispatcher,
            DispatcherPriority priority = DispatcherPriority.Normal)
        {
            return dispatcher.Invoke(
                TaskScheduler.FromCurrentSynchronizationContext, priority);
        }

        public static Task<TaskScheduler> ToTaskSchedulerAsync(
            this Dispatcher dispatcher,
            DispatcherPriority priority = DispatcherPriority.Normal)
        {
            var completionSource = new TaskCompletionSource<TaskScheduler>();

            var invocation = dispatcher.BeginInvoke(
                new Action(() =>
                    completionSource.SetResult(
                        TaskScheduler.FromCurrentSynchronizationContext())),
                priority);

            invocation.Aborted += (s, e) => completionSource.SetCanceled();

            return completionSource.Task;
        }
    }
}
