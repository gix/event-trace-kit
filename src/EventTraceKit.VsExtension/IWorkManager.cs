namespace EventTraceKit.VsExtension
{
    using System;
    using System.Threading.Tasks;
    using System.Windows.Threading;

    public interface IWorkManager
    {
        bool CheckAccess();
        void Post(Action action);
        void Send(Action action);
        T Send<T>(Func<T> action);
        void VerifyAccess();
    }

    internal sealed class UIWorkManager : IWorkManager
    {
        private readonly Dispatcher dispatcher;

        internal UIWorkManager(Dispatcher dispatcher)
        {
            if (dispatcher == null)
                throw new ArgumentNullException(nameof(dispatcher));
            this.dispatcher = dispatcher;

            TaskScheduler = dispatcher.ToTaskScheduler();
            TaskFactory = new TaskFactory(TaskScheduler);
        }

        public TaskScheduler TaskScheduler { get; }
        public TaskFactory TaskFactory { get; }

        public bool CheckAccess()
        {
            return dispatcher.CheckAccess();
        }

        public void Post(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            dispatcher.BeginInvoke(action, DispatcherPriority.ContextIdle);
        }

        public void Send(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (CheckAccess())
                action();
            else
                dispatcher.Invoke(action, DispatcherPriority.ContextIdle);
        }

        public T Send<T>(Func<T> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (CheckAccess())
                return action();

            return dispatcher.Invoke(action, DispatcherPriority.ContextIdle);
        }

        public void VerifyAccess()
        {
            dispatcher.VerifyAccess();
        }
    }

    public sealed class WorkManager
    {
        private BackgroundWorkManager backgroundWorkManager;
        private static readonly object backgroundWorkThreadIDObj;
        private UIWorkManager uiWorkManager;
        private static readonly object uiWorkThreadIDObj;

        static WorkManager()
        {
            uiWorkThreadIDObj = WorkThreadID.UI;
            backgroundWorkThreadIDObj = WorkThreadID.Background;
        }

        public WorkManager(Dispatcher uiDispatcher)
        {
            backgroundWorkManager = new BackgroundWorkManager();
            uiWorkManager = new UIWorkManager(uiDispatcher);
        }

        public TaskScheduler BackgroundTaskScheduler => backgroundWorkManager.TaskScheduler;
        public TaskFactory BackgroundTaskFactory => backgroundWorkManager.TaskFactory;
        public IWorkManager BackgroundThread => backgroundWorkManager;
        public TaskScheduler UIThreadTaskScheduler => uiWorkManager.TaskScheduler;
        public TaskFactory UIThreadTaskFactory => uiWorkManager.TaskFactory;
        public IWorkManager UIThread => uiWorkManager;

        private enum WorkThreadID
        {
            UI,
            Background
        }
    }

    internal class BackgroundWorkManager : IWorkManager
    {
        public bool CheckAccess()
        {
            return true;
        }

        public void Post(Action action)
        {
            Task.Run(action);
        }

        public void Send(Action action)
        {
            Task.Run(action).Wait();
        }

        public T Send<T>(Func<T> action)
        {
            return Task.Run(action).Result;
        }

        public void VerifyAccess()
        {
        }

        public TaskScheduler TaskScheduler => TaskScheduler.Default;
        public TaskFactory TaskFactory => Task.Factory;
    }
}
