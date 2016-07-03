namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Threading;
    using EventTraceKit.VsExtension.Collections;
    using EventTraceKit.VsExtension.Controls;
    using Microsoft.VisualStudio.Shell;
    using Task = System.Threading.Tasks.Task;

    public class TraceLogWindowViewModel : ViewModel, IEventInfoSource
    {
        private readonly DispatcherTimer updateStatisticsTimer;
        private readonly TraceSessionDescriptor sessionDescriptor = new TraceSessionDescriptor();

        private string status;
        private bool showStatistics;
        private string formattedEventStatistics;
        private string formattedBufferStatistics;
        private bool autoLog;
        private bool isCollecting;
        private TraceLog traceLog;
        private TraceSession session;

        private DateTime lastUpdateEvent = DateTime.MinValue;
        private TimeSpan updateThreshold = TimeSpan.FromMilliseconds(50);
        private DispatcherSynchronizationContext syncCtx;
        private TaskScheduler scheduler;
        private TaskFactory taskFactory;

        public TraceLogWindowViewModel(IOperationalModeProvider modeProvider)
        {
            if (modeProvider != null)
                modeProvider.OperationalModeChanged += OnOperationalModeChanged;

            var tableTuple = new GenericEventsViewModelSource().CreateTable(this);
            var dataTable = tableTuple.Item1;
            var preset = tableTuple.Item2;

            EventsDataView = new TraceEventsView(dataTable);
            HdvViewModel = new HdvViewModel(EventsDataView);
            GridModel = HdvViewModel.GridViewModel;

            HdvViewModel.HdvViewModelPreset = preset;

            syncCtx = new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher);
            SynchronizationContext.SetSynchronizationContext(syncCtx);
            scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            taskFactory = new TaskFactory(scheduler);

            Statistics = new TraceLogStatsModel();
            ShowStatistics = true;
            AutoLog = true;

            updateStatisticsTimer = new DispatcherTimer(DispatcherPriority.Background);
            updateStatisticsTimer.Interval = TimeSpan.FromSeconds(1);
            updateStatisticsTimer.Tick += (s, a) => UpdateStats();

            var provider = new TraceProviderDescriptor(new Guid("716EFEF7-5AC2-4EE0-8277-D9226411A155")) {
                IncludeSecurityId = true,
                IncludeStackTrace = true,
                IncludeTerminalSessionId = true
            };
            //spec.SetManifest(@"C:\Users\nrieck\dev\ffmf\src\Sculptor\Sculptor.man");
            provider.SetProviderBinary(@"C:\Users\nrieck\dev\ffmf\build\x64-dbg\bin\Sculptor.dll");
            sessionDescriptor.Providers.Add(provider);
        }

        public TraceEventsView EventsDataView { get; }
        public HdvViewModel HdvViewModel { get; }
        public AsyncDataGridViewModel GridModel { get; }

        public TraceLogStatsModel Statistics { get; }

        public string Status
        {
            get { return status; }
            set { SetProperty(ref status, value); }
        }

        public bool IsCollecting
        {
            get { return isCollecting; }
            private set { SetProperty(ref isCollecting, value); }
        }

        public bool AutoLog
        {
            get { return autoLog; }
            set { SetProperty(ref autoLog, value); }
        }

        public bool ShowStatistics
        {
            get { return showStatistics; }
            set { SetProperty(ref showStatistics, value); }
        }

        public string FormattedEventStatistics
        {
            get { return formattedEventStatistics; }
            set { SetProperty(ref formattedEventStatistics, value); }
        }

        public string FormattedBufferStatistics
        {
            get { return formattedBufferStatistics; }
            set { SetProperty(ref formattedBufferStatistics, value); }
        }

        private bool CanStartCapture()
        {
            return sessionDescriptor.Providers.Count > 0;
        }

        private void ToggleCapture()
        {
            if (IsCollecting)
                StopCapture();
            else if (CanStartCapture())
                StartCapture();
        }

        public void Clear()
        {
            try {
                traceLog?.Clear();
                session?.Flush();
                EventsDataView.Clear();
                Statistics.Reset();
            } catch (Exception ex) {
                Status = ex.ToString();
            }
        }

        public void StartCapture()
        {
            if (!CanStartCapture() || session != null)
                return;

            Status = null;
            try {
                traceLog = new TraceLog();
                traceLog.EventsChanged += OnEventsChanged;
                session = new TraceSession(sessionDescriptor);
                session.Start(traceLog);
                IsCollecting = true;
                updateStatisticsTimer.Start();
            } catch (Exception ex) {
                session?.Stop();
                session = null;
                IsCollecting = false;
                updateStatisticsTimer.Stop();
                Status = ex.ToString();
                MessageBox.Show(
                    "Failed to start trace session.\r\n" + ex.Message,
                    "",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OnEventsChanged(UIntPtr newCount)
        {
            EventsDataView.UpdateRowCount((int)newCount.ToUInt32());
            return;

            var now = DateTime.UtcNow;
            var elapsed = now - lastUpdateEvent;

            if (elapsed < updateThreshold)
                return;

            lastUpdateEvent = now;
            taskFactory.StartNew(() => Update((int)newCount.ToUInt32()));
        }

        private void Update(int newCount)
        {
            //Events.Update(newCount);
        }

        public void StopCapture()
        {
            if (session == null)
                return;

            try {
                //Events.Detach(session);
                session.Stop();
                session.Dispose();
                session = null;
                Status = null;
                IsCollecting = false;
                updateStatisticsTimer.Stop();
            } catch (Exception ex) {
                Status = ex.ToString();
            }
        }

        private void Configure()
        {
            var viewModel = new TraceSessionSettingsWindowViewModel();
            foreach (var provider in sessionDescriptor.Providers)
                viewModel.Providers.Add(new TraceProviderSpecViewModel(provider));

            var window = new TraceSessionSettingsWindow();
            window.DataContext = viewModel;
            if (window.ShowModal() != true)
                return;

            sessionDescriptor.Providers.Clear();
            sessionDescriptor.Providers.AddRange(viewModel.Providers.Select(x => x.ToModel()));
        }

        private async void UpdateStats()
        {
            if (session == null)
                return;

            var stats = session.Query();

            Statistics.TotalEvents = (uint)EventsDataView.EventCount;
            Statistics.EventsLost = stats.EventsLost;
            Statistics.NumberOfBuffers = stats.NumberOfBuffers;
            Statistics.BuffersWritten = stats.BuffersWritten;
            Statistics.FreeBuffers = stats.FreeBuffers;
            Statistics.LogBuffersLost = stats.LogBuffersLost;
            Statistics.RealTimeBuffersLost = stats.RealTimeBuffersLost;
            FormatStatistics();
        }

        protected void FormatStatistics()
        {
            FormattedEventStatistics =
                $"{Statistics.TotalEvents} Events " +
                $"({Statistics.EventsLost} lost)";
            FormattedBufferStatistics =
                $"{Statistics.NumberOfBuffers} Buffers (" +
                $"{Statistics.BuffersWritten} written, " +
                $"{Statistics.LogBuffersLost} lost, " +
                $"{Statistics.RealTimeBuffersLost} real-time buffers lost)";
        }

        public void Attach(MenuCommandService commandService)
        {
            var id = new CommandID(Guids.TraceLogCmdSet, PkgCmdId.cmdidCaptureLog);
            commandService.AddCommand(
                new OleMenuCommand(OnToggleCaptureLog, null, OnQueryToggleCaptureLog, id));

            id = new CommandID(Guids.TraceLogCmdSet, PkgCmdId.cmdidClearLog);
            commandService.AddCommand(new OleMenuCommand(OnClearLog, id));

            id = new CommandID(Guids.TraceLogCmdSet, PkgCmdId.cmdidAutoLog);
            commandService.AddCommand(
                new OleMenuCommand(OnToggleAutoLog, null, OnQueryToggleAutoLog, id));

            id = new CommandID(Guids.TraceLogCmdSet, PkgCmdId.cmdidConfigureLog);
            commandService.AddCommand(new OleMenuCommand(OnConfigureLog, id));
        }

        private void OnQueryToggleCaptureLog(object sender, EventArgs e)
        {
            var command = sender as OleMenuCommand;
            if (command == null)
                return;

            command.Enabled = IsCollecting || CanStartCapture();
            command.Checked = IsCollecting;
        }

        private void OnQueryToggleAutoLog(object sender, EventArgs e)
        {
            var command = sender as OleMenuCommand;
            if (command != null)
                command.Checked = AutoLog;
        }

        private void OnToggleAutoLog(object sender, EventArgs e)
        {
            AutoLog = !AutoLog;
        }

        private void OnToggleCaptureLog(object sender, EventArgs e)
        {
            ToggleCapture();
        }

        private void OnClearLog(object sender, EventArgs e)
        {
            Clear();
        }

        private void OnConfigureLog(object sender, EventArgs e)
        {
            Configure();
        }

        private void OnOperationalModeChanged(object sender, VsOperationalMode newMode)
        {
            if (!AutoLog)
                return;

            switch (newMode) {
                case VsOperationalMode.Design:
                    StopCapture();
                    break;
                case VsOperationalMode.Debug:
                    StartCapture();
                    break;
            }
        }

        public TraceSessionInfo GetInfo()
        {
            if (session != null)
                return session.GetInfo();
            return new TraceSessionInfo();
        }

        public EventInfo GetEvent(int index)
        {
            if (traceLog != null)
                return traceLog.GetEvent(index);
            return new EventInfo();
        }
    }

    public class TraceLogWindowDesignTimeModel : TraceLogWindowViewModel
    {
        public TraceLogWindowDesignTimeModel()
            : base(null)
        {
            Statistics.TotalEvents = 1429;
            Statistics.EventsLost = 30;

            Statistics.NumberOfBuffers = 10;
            Statistics.FreeBuffers = 20;
            Statistics.BuffersWritten = 40;
            Statistics.LogBuffersLost = 50;
            Statistics.RealTimeBuffersLost = 60;

            ShowStatistics = true;
            FormatStatistics();
        }
    }

    public class TraceLogStatsModel : ViewModel
    {
        private uint totalEvents;
        private uint eventsLost;
        private uint numberOfBuffers;
        private uint freeBuffers;
        private uint buffersWritten;
        private uint logBuffersLost;
        private uint realTimeBuffersLost;

        public uint TotalEvents
        {
            get { return totalEvents; }
            set { SetProperty(ref totalEvents, value); }
        }

        public uint EventsLost
        {
            get { return eventsLost; }
            set { SetProperty(ref eventsLost, value); }
        }

        public uint NumberOfBuffers
        {
            get { return numberOfBuffers; }
            set { SetProperty(ref numberOfBuffers, value); }
        }

        public uint FreeBuffers
        {
            get { return freeBuffers; }
            set { SetProperty(ref freeBuffers, value); }
        }

        public uint BuffersWritten
        {
            get { return buffersWritten; }
            set { SetProperty(ref buffersWritten, value); }
        }

        public uint LogBuffersLost
        {
            get { return logBuffersLost; }
            set { SetProperty(ref logBuffersLost, value); }
        }

        public uint RealTimeBuffersLost
        {
            get { return realTimeBuffersLost; }
            set { SetProperty(ref realTimeBuffersLost, value); }
        }

        public void Reset()
        {
            totalEvents = 0;
            eventsLost = 0;
            numberOfBuffers = 0;
            freeBuffers = 0;
            buffersWritten = 0;
            logBuffersLost = 0;
            realTimeBuffersLost = 0;
        }
    }

    public class TraceProviderSpecViewModel : ViewModel
    {
        private byte level;
        private ulong matchAnyKeyword;
        private ulong matchAllKeyword;

        private bool includeSecurityId;
        private bool includeTerminalSessionId;
        private bool includeStackTrace;

        private string manifestOrProvider;

        public TraceProviderSpecViewModel(Guid id)
        {
            Id = id;
            ProcessIds = new ObservableCollection<uint>();
            EventIds = new ObservableCollection<ushort>();
            Level = 0xFF;
        }

        public TraceProviderSpecViewModel(TraceProviderDescriptor provider)
        {
            Id = provider.Id;
            Level = provider.Level;
            MatchAnyKeyword = provider.MatchAnyKeyword;
            MatchAllKeyword = provider.MatchAllKeyword;
            IncludeSecurityId = provider.IncludeSecurityId;
            IncludeTerminalSessionId = provider.IncludeTerminalSessionId;
            IncludeStackTrace = provider.IncludeStackTrace;
            ManifestOrProvider = provider.Manifest ?? provider.ProviderBinary;
            if (provider.ProcessIds != null)
                ProcessIds = new ObservableCollection<uint>(provider.ProcessIds);
            if (provider.EventIds != null)
                EventIds = new ObservableCollection<ushort>(provider.EventIds);
        }

        public Guid Id { get; }

        public byte Level
        {
            get { return level; }
            set { SetProperty(ref level, value); }
        }

        public ulong MatchAnyKeyword
        {
            get { return matchAnyKeyword; }
            set { SetProperty(ref matchAnyKeyword, value); }
        }

        public ulong MatchAllKeyword
        {
            get { return matchAllKeyword; }
            set { SetProperty(ref matchAllKeyword, value); }
        }

        public bool IncludeSecurityId
        {
            get { return includeSecurityId; }
            set { SetProperty(ref includeSecurityId, value); }
        }

        public bool IncludeTerminalSessionId
        {
            get { return includeTerminalSessionId; }
            set { SetProperty(ref includeTerminalSessionId, value); }
        }

        public bool IncludeStackTrace
        {
            get { return includeStackTrace; }
            set { SetProperty(ref includeStackTrace, value); }
        }

        public string ManifestOrProvider
        {
            get { return manifestOrProvider; }
            set { SetProperty(ref manifestOrProvider, value); }
        }

        public ObservableCollection<uint> ProcessIds { get; }
        public ObservableCollection<ushort> EventIds { get; }

        public TraceProviderDescriptor ToModel()
        {
            var spec = new TraceProviderDescriptor(Id);
            spec.Level = Level;
            spec.MatchAnyKeyword = MatchAnyKeyword;
            spec.MatchAllKeyword = MatchAllKeyword;
            spec.IncludeSecurityId = IncludeSecurityId;
            spec.IncludeTerminalSessionId = IncludeTerminalSessionId;
            spec.IncludeStackTrace = IncludeStackTrace;
            if (string.IsNullOrWhiteSpace(ManifestOrProvider)) {
                if (ManifestOrProvider.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || ManifestOrProvider.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    spec.SetManifest(ManifestOrProvider);
                else
                    spec.SetProviderBinary(ManifestOrProvider);
            }
            spec.ProcessIds.AddRange(ProcessIds);
            spec.EventIds.AddRange(EventIds);
            return spec;
        }
    }

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

    internal sealed class UIWorkManager : IWorkManager
    {
        private readonly Dispatcher dispatcher;

        internal UIWorkManager(Dispatcher dispatcher)
        {
            if (dispatcher == null)
                throw new ArgumentNullException(nameof(dispatcher));
            this.dispatcher = dispatcher;
        }

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

        public IWorkManager BackgroundThread => backgroundWorkManager;
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
