namespace EventTraceKit.VsExtension
{
    using System;
    using System.Threading.Tasks;
    using System.Windows.Threading;
    using Extensions;

    public interface IWorkManager
    {
        bool CheckAccess();
        void VerifyAccess();
    }

    internal sealed class DispatcherWorkManager : IWorkManager
    {
        private readonly Dispatcher dispatcher;

        internal DispatcherWorkManager(Dispatcher dispatcher)
        {
            this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

            TaskScheduler = dispatcher.ToTaskScheduler();
            TaskFactory = new TaskFactory(TaskScheduler);
        }

        public TaskScheduler TaskScheduler { get; }
        public TaskFactory TaskFactory { get; }

        public bool CheckAccess()
        {
            return dispatcher.CheckAccess();
        }

        public void VerifyAccess()
        {
            dispatcher.VerifyAccess();
        }
    }

    public sealed class WorkManager
    {
        private readonly BackgroundWorkManager backgroundWorkManager;
        private readonly DispatcherWorkManager uiWorkManager;

        public WorkManager(Dispatcher uiDispatcher)
        {
            backgroundWorkManager = new BackgroundWorkManager();
            uiWorkManager = new DispatcherWorkManager(uiDispatcher);
        }

        public TaskScheduler BackgroundTaskScheduler => backgroundWorkManager.TaskScheduler;
        public TaskScheduler UIThreadTaskScheduler => uiWorkManager.TaskScheduler;
        public TaskFactory UIThreadTaskFactory => uiWorkManager.TaskFactory;
    }

    internal class BackgroundWorkManager : IWorkManager
    {
        public BackgroundWorkManager()
        {
            TaskScheduler = TaskScheduler.Default;
            TaskFactory = new TaskFactory(TaskScheduler);
        }

        public TaskScheduler TaskScheduler { get; }
        public TaskFactory TaskFactory { get; }

        public bool CheckAccess()
        {
            return true;
        }

        public void VerifyAccess()
        {
        }
    }
}
