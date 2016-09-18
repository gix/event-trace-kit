namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Threading;
    using EventTraceKit.VsExtension.Controls;
    using Task = System.Threading.Tasks.Task;

    public sealed class DataViewColumnsCollection : IEnumerable<DataColumnView>
    {
        private readonly DataView view;

        public DataViewColumnsCollection(DataView view)
        {
            this.view = view;
        }

        public int IndexOf(DataColumnView column)
        {
            return view.GetDataColumnViewIndex(column);
        }

        public int Count => view.ColumnCount;

        public DataColumnView this[int columnIndex] => view.GetDataColumnView(columnIndex);

        public IEnumerator<DataColumnView> GetEnumerator()
        {
            for (int index = 0; index < Count; ++index)
                yield return this[index];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class DataView : DependencyObject, IDataView, INotifyPropertyChanged
    {
        private readonly DataTable table;

        private int deferredUpdateNestingDepth;

        public DataView(DataTable table)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));
            this.table = table;

            ClearCache();
        }

        public DataViewColumnsCollection Columns =>
            new DataViewColumnsCollection(this);

        public DataViewColumnsCollection VisibleColumns =>
            new DataViewColumnsCollection(this);

        protected DataColumnViewInfo[] DataColumnViewInfos { get; set; }
        protected DataColumnView[] DataColumnViews { get; set; }

        public bool DeferUpdates => deferredUpdateNestingDepth > 0;

        public void BeginDataUpdate()
        {
            ++deferredUpdateNestingDepth;
        }

        public bool EndDataUpdate()
        {
            if (--deferredUpdateNestingDepth != 0)
                return false;

            OnDataUpdated();
            return true;
        }

        protected virtual void OnDataUpdated()
        {
        }

        public void ApplyColumnView(DataColumnViewInfo[] dataColumnViewInfos)
        {
            ApplyColumnViewCore(dataColumnViewInfos);
        }

        protected void ApplyColumnViewCore(IEnumerable<DataColumnViewInfo> dataColumnViewInfos)
        {
            if (dataColumnViewInfos == null)
                throw new ArgumentNullException(nameof(dataColumnViewInfos));

            DataColumnViewInfos = dataColumnViewInfos.ToArray();
            foreach (DataColumnViewInfo info in DataColumnViewInfos)
                info.View = this;

            RefreshDataColumnViewFromViewInfos();
            //this.VisibleDataColumnViewIndices = new Int32List();
            //this.RefreshVisibleColumns();
        }

        private void RefreshDataColumnViewFromViewInfos()
        {
            DataColumnViews = new DataColumnView[DataColumnViewInfos.Length];
            Parallel.For(0, DataColumnViewInfos.Length, i => {
                DataColumnViews[i] = CreateDataColumnViewFromInfo(DataColumnViewInfos[i]);
            });
        }

        public DataColumnView CreateDataColumnViewFromInfo(DataColumnViewInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            DataColumn column = table.Columns[info.ColumnId];
            return column.CreateView(info);
        }

        public CellValue GetCellValue(int rowIndex, int columnIndex)
        {
            if (rowIndex >= RowCount)
                return null;

            return DataColumnViews[columnIndex].GetCellValue(rowIndex);
        }

        public event EventHandler RowCountChanged;

        public void UpdateRowCount(int newCount)
        {
            if (newCount == RowCount)
                return;

            if (newCount == 0)
                ClearCache();

            RowCount = newCount;
            //Application.Current.Dispatcher.Invoke(delegate {
            //    Updated?.Invoke(this, trueEventArgs);
            //});
            //RaisePropertyChanged(rowCountChangedArgs);
            RaisePropertyChanged(rowChangedEventArgs);
            RowCountChanged?.Invoke(this, EventArgs.Empty);
        }

        private readonly PropertyChangedEventArgs rowChangedEventArgs = new PropertyChangedEventArgs(nameof(RowCount));

        public int RowCount { get; private set; }
        public int ColumnCount => DataColumnViews?.Length ?? 0;

        public DataColumnView GetDataColumnView(int columnIndex)
        {
            return DataColumnViews[columnIndex];
        }

        public void Clear()
        {
            ClearCache();
        }

        private void ClearCache()
        {
        }

        public object DataValidityToken { get; private set; }

        public bool IsValidDataValidityToken(object dataValidityToken)
        {
            return dataValidityToken != null && dataValidityToken == DataValidityToken;
        }

        public int GetDataColumnViewIndex(DataColumnView column)
        {
            if (DataColumnViews == null)
                return -1;
            return Array.IndexOf(DataColumnViews, column);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(
            [CallerMemberName] string propertyName = null)
        {
            RaisePropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        private void RaisePropertyChanged(PropertyChangedEventArgs eventArgs)
        {
            PropertyChanged?.Invoke(this, eventArgs);
        }
    }

    public class TraceEventsView : DataView
    {
        public TraceEventsView(DataTable table)
            : base(table)
        {
        }

        public int EventCount => RowCount;
    }

    public class DataTable : ICloneable
    {
        private readonly List<DataColumn> columns = new List<DataColumn>();
        private readonly Dictionary<Guid, DataColumn> mapColumnGuidToColumn =
            new Dictionary<Guid, DataColumn>();
        private readonly Dictionary<string, DataColumn> mapColumnNameToColumn =
            new Dictionary<string, DataColumn>();

        public DataTable(string tableName)
        {
            TableName = tableName;
        }

        public string TableName { get; }
        internal int Count => columns.Count;
        public DataTableColumnCollection Columns => new DataTableColumnCollection(this);

        internal DataColumn this[int index] => columns[index];
        internal DataColumn this[string columnName] => mapColumnNameToColumn[columnName];
        internal DataColumn this[Guid columnGuid] => mapColumnGuidToColumn[columnGuid];

        internal void Add(DataColumn column)
        {
            if (column == null)
                throw new ArgumentNullException(nameof(column));
            if (column.Name == null)
                throw new ArgumentException("Column must have a name.");
            if (column.Id == Guid.Empty)
                throw new ArgumentException("Column must have an id.");

            if (mapColumnNameToColumn.ContainsKey(column.Name))
                throw new InvalidOperationException(
                    $"DataTable already contains a column named {column.Name}");
            if (mapColumnGuidToColumn.ContainsKey(column.Id))
                throw new InvalidOperationException(
                    $"DataTable already contains a column with ID {column.Id}.");

            columns.Add(column);
            mapColumnNameToColumn[column.Name] = column;
            mapColumnGuidToColumn[column.Id] = column;
        }

        object ICloneable.Clone() => Clone();

        public DataTable Clone()
        {
            var table = new DataTable(TableName);
            foreach (DataColumn column in Columns)
                table.Add(column);

            return table;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DataTableColumnCollection : IEnumerable<DataColumn>
    {
        private readonly DataTable table;

        public DataTableColumnCollection(DataTable table)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));
            this.table = table;
        }

        public DataColumn this[int index] => table[index];
        public DataColumn this[Guid id] => table[id];
        public DataColumn this[string name] => table[name];

        public void Add(DataColumn column)
        {
            if (column == null)
                throw new ArgumentNullException(nameof(column));
            table.Add(column);
        }

        public IEnumerator<DataColumn> GetEnumerator()
        {
            return table.Columns.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

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

    public interface IWorkManager
    {
        bool CheckAccess();
        void Post(Action action);
        void Send(Action action);
        void VerifyAccess();
    }

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

        public void VerifyAccess()
        {
        }

        public TaskScheduler TaskScheduler => TaskScheduler.Default;
        public TaskFactory TaskFactory => Task.Factory;
    }

    public static class ExceptionUtils
    {
        public static void ThrowInvalidOperationException(string message)
        {
            throw new InvalidOperationException(message);
        }

        public static void ThrowInternalErrorException(string message)
        {
            throw new InternalErrorException(message);
        }
    }

    [Serializable]
    public class InternalErrorException : InvalidOperationException
    {
        public InternalErrorException()
        {
        }

        public InternalErrorException(string message)
            : base(message)
        {
        }

        public InternalErrorException(Exception innerException)
            : base(innerException?.Message, innerException)
        {
        }

        public InternalErrorException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected InternalErrorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
