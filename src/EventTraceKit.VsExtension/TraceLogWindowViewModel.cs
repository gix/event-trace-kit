namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Threading;
    using EventTraceKit.VsExtension.Controls;
    using Microsoft.VisualStudio.PlatformUI;
    using Microsoft.VisualStudio.Shell;
    using Task = System.Threading.Tasks.Task;

    public class TableControlViewModel : ViewModel
    {
        public ObservableCollection<TableHeaderViewModel> Headers { get; } =
            new ObservableCollection<TableHeaderViewModel>();

        //public ObservableCollection<TableEntryViewModel> Entries { get; } =
        //    new ObservableCollection<TableEntryViewModel>();

        private GridView view;

        public void AddColumn(TableHeaderViewModel header)
        {
            var factory = new FrameworkElementFactory(typeof(ContentPresenter));
            factory.SetBinding(
                ContentPresenter.ContentProperty,
                new Binding(header.MemberName));
            var template = new DataTemplate { VisualTree = factory };
            var column = new GridViewColumn {
                Header = header.Header,
                Width = header.ColumnWidth,
                //CellTemplate = template
            };

            int index = Headers.Where(h => h.IsVisible).TakeWhile(h => h != header).Count();
            view.Columns.Insert(index, column);
        }

        public void RemoveColumn(TableHeaderViewModel header)
        {
            var column = view.Columns.FirstOrDefault(
                c => (c.Header as FrameworkElement)?.DataContext == header);
            if (column != null)
                view.Columns.Remove(column);
        }

        private MenuItem BuildColumnsMenu()
        {
            var menu = new MenuItem();
            menu.Header = "Show Columns";

            foreach (var header in Headers) {
                var item = new MenuItem {
                    Header = header.Header,
                    Tag = header,
                    IsCheckable = true,
                    IsChecked = true
                };
                item.Command = new DelegateCommand(obj => {
                    if (!item.IsChecked)
                        AddColumn(header);
                    else
                        RemoveColumn(header);
                });
                menu.Items.Add(item);
            }

            return menu;
        }
    }

    public unsafe class TraceEvent2
    {
        private EventRecordCPtr record;

        public TraceEvent2(IntPtr eventPtr)
        {
            record = new EventRecordCPtr((EVENT_RECORD*)eventPtr.ToPointer());
        }

        public int Id => record.EventHeader.EventDescriptor.Id;
    }

    public class EventsCollection
        : IReadOnlyList<TraceEvent2>
        , INotifyPropertyChanged
        , INotifyCollectionChanged
        , ICollectionViewFactory
    {
        private TraceSession session;

        public void Attach(TraceSession session)
        {
            this.session = session;
        }

        public void Detach(TraceSession session)
        {
            this.session = null;
            Update(0);
            cache.Clear();
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public IEnumerator<TraceEvent2> GetEnumerator()
        {
            int count = Count;
            for (int i = 0; i < count; ++i)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count { get; private set; }

        public TraceEvent2 this[int index]
        {
            get
            {
                if (session == null)
                    throw new ArgumentOutOfRangeException(nameof(index));

                TraceEvent2 evt;
                if (!cache.TryGetValue(index, out evt))
                    cache[index] = evt = new TraceEvent2(session.GetEvent(index));

                return evt;
            }
        }

        private class VirtualList : IList
        {
            private readonly EventsCollection collection;
            private readonly int offset;
            private readonly int count;

            public VirtualList(EventsCollection collection, int offset, int count)
            {
                this.collection = collection;
                this.offset = offset;
                this.count = count;
            }

            public IEnumerator GetEnumerator()
            {
                throw new NotImplementedException();
            }

            public int Count => count;

            public object SyncRoot => this;

            public bool IsSynchronized => false;

            public bool IsReadOnly => true;

            public bool IsFixedSize => true;

            public object this[int index]
            {
                get
                {
                    if (index < 0 || index > count)
                        throw new ArgumentOutOfRangeException(nameof(index));
                    return collection[offset + index];
                }
                set { throw new NotSupportedException(); }
            }

            public int Add(object value)
            {
                throw new NotSupportedException();
            }

            public bool Contains(object value)
            {
                return false;
            }

            public int IndexOf(object value)
            {
                return -1;
            }

            public void CopyTo(Array array, int index)
            {
                if (array.Length - count < index)
                    throw new ArgumentOutOfRangeException(nameof(array));
                for (int i = 0; i < count; ++i)
                    array.SetValue(collection[offset + i], index + i);
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public void Insert(int index, object value)
            {
                throw new NotSupportedException();
            }

            public void Remove(object value)
            {
                throw new NotSupportedException();
            }

            public void RemoveAt(int index)
            {
                throw new NotSupportedException();
            }
        }

        public void Update(int newCount)
        {
            int oldCount = Count;
            Count = newCount;
            RaisePropertyChanged(nameof(Count));
            RaisePropertyChanged("Item[]");

            if (Count == 0) {
                var args = new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Reset);
                CollectionChanged?.Invoke(this, args);
            } else if (newCount > oldCount) {
                int added = newCount - oldCount;
                var args = new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Add, new VirtualList(this, oldCount, added));
                CollectionChanged?.Invoke(this, args);
            }
        }

        private readonly Dictionary<int, TraceEvent2> cache = new Dictionary<int, TraceEvent2>();

        public void Clear()
        {
        }

        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ICollectionView CreateView()
        {
            return new CollectionView(this);
        }

        private class CollectionView
            : DispatcherObject
            , ICollectionView
            , INotifyPropertyChanged
        {
            private readonly EventsCollection collection;
            private int deferLevel;
            private object currentItem;
            private int currentPosition;
            private int timestamp;

            public CollectionView(EventsCollection collection)
            {
                this.collection = collection;

                var propertyChanged = collection as INotifyPropertyChanged;
                if (propertyChanged != null) {
                    propertyChanged.PropertyChanged += OnPropertyChanged;
                }

                var changed = collection as INotifyCollectionChanged;
                if (changed != null) {
                    changed.CollectionChanged += OnCollectionChanged;
                    //this.SetFlag(CollectionViewFlags.IsDynamic, true);
                }
            }

            private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
            }

            private void OnCollectionChanged(
                object sender, NotifyCollectionChangedEventArgs args)
            {
                //if (!this.AllowsCrossThreadChanges) {
                if (!CheckAccess())
                    throw new NotSupportedException("MultiThreadedCollectionChangeNotSupported");

                ProcessCollectionChanged(args);
                //} else {
                //    this.PostChange(args);
                //}
            }

            protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
            {
                if (args == null)
                    throw new ArgumentNullException(nameof(args));

                ++timestamp;
                CollectionChanged?.Invoke(this, args);

                if (args.Action != NotifyCollectionChangedAction.Replace &&
                    args.Action != NotifyCollectionChangedAction.Move)
                    RaisePropertyChanged(nameof(Count));

                bool isEmpty = IsEmpty;
                if (isEmpty != this.CheckFlag(CollectionViewFlags.CachedIsEmpty)) {
                    SetFlag(CollectionViewFlags.CachedIsEmpty, isEmpty);
                    RaisePropertyChanged(nameof(IsEmpty));
                }
            }

            [Flags]
            private enum CollectionViewFlags
            {
                AllowsCrossThreadChanges = 0x100,
                CachedIsEmpty = 0x200,
                IsCurrentAfterLast = 0x10,
                IsCurrentBeforeFirst = 8,
                IsDataInGroupOrder = 0x40,
                IsDynamic = 0x20,
                NeedsRefresh = 0x80,
                ShouldProcessCollectionChanged = 4,
                UpdatedOutsideDispatcher = 2
            }

            private CollectionViewFlags flags;

            private bool CheckFlag(CollectionViewFlags flag)
            {
                return (flags & flag) > 0;
            }

            private void SetFlag(CollectionViewFlags flag, bool value)
            {
                if (value)
                    flags |= flag;
                else
                    flags &= ~flag;
            }

            protected virtual void ProcessCollectionChanged(NotifyCollectionChangedEventArgs args)
            {
                //this.ValidateCollectionChangedEventArgs(args);

                object oldCurrentItem = currentItem;
                int oldCurrentPosition = currentPosition;
                bool oldIsCurrentAfterLast = CheckFlag(CollectionViewFlags.IsCurrentAfterLast);
                bool oldIsCurrentBeforeFirst = CheckFlag(CollectionViewFlags.IsCurrentBeforeFirst);

                bool changed = false;
                switch (args.Action) {
                    case NotifyCollectionChangedAction.Add:
                        changed = true;
                        //AdjustCurrencyForAdd(args.NewStartingIndex);
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        changed = true;
                        //AdjustCurrencyForRemove(args.OldStartingIndex);
                        break;

                    case NotifyCollectionChangedAction.Replace:
                        changed = true;
                        //AdjustCurrencyForReplace(args.OldStartingIndex);
                        break;

                    case NotifyCollectionChangedAction.Move:
                        changed = true;
                        //AdjustCurrencyForMove(args.OldStartingIndex, args.NewStartingIndex);
                        break;

                    case NotifyCollectionChangedAction.Reset:
                        //RefreshOrDefer();
                        return;
                }

                if (changed)
                    OnCollectionChanged(args);

                //if (this._currentElementWasRemovedOrReplaced) {
                //    this.MoveCurrencyOffDeletedElement();
                //    this._currentElementWasRemovedOrReplaced = false;
                //}

                if (IsCurrentAfterLast != oldIsCurrentAfterLast)
                    RaisePropertyChanged(nameof(IsCurrentAfterLast));
                if (IsCurrentBeforeFirst != oldIsCurrentBeforeFirst)
                    RaisePropertyChanged(nameof(IsCurrentBeforeFirst));
                if (currentPosition != oldCurrentPosition)
                    RaisePropertyChanged(nameof(CurrentPosition));
                if (currentItem != oldCurrentItem)
                    RaisePropertyChanged(nameof(CurrentItem));
            }

            public IEnumerator GetEnumerator()
            {
                return new Enumerator(this);
            }

            private class Enumerator : IEnumerator
            {
                private readonly CollectionView view;
                private int index;

                public Enumerator(CollectionView view)
                {
                    this.view = view;
                }

                public bool MoveNext()
                {
                    if (index == view.collection.Count)
                        return false;
                    ++index;
                    return true;
                }

                public void Reset()
                {
                    index = -1;
                }

                public object Current
                {
                    get { return view.collection[index]; }
                }
            }

            public event NotifyCollectionChangedEventHandler CollectionChanged;

            public bool Contains(object item)
            {
                return false;
            }

            public void Refresh()
            {
                OnCollectionChanged(
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }

            public IDisposable DeferRefresh()
            {
                ++deferLevel;
                return new DeferRefreshScope(this);

            }

            private sealed class DeferRefreshScope : IDisposable
            {
                private CollectionView collectionView;

                public DeferRefreshScope(CollectionView collectionView)
                {
                    this.collectionView = collectionView;
                }

                public void Dispose()
                {
                    if (collectionView != null) {
                        collectionView.EndDeferRefresh();
                        collectionView = null;
                    }
                }
            }

            private void EndDeferRefresh()
            {
                if (--deferLevel == 0)
                    Refresh();
            }

            public bool MoveCurrentToFirst()
            {
                return false;
            }

            public bool MoveCurrentToLast()
            {
                return false;
            }

            public bool MoveCurrentToNext()
            {
                return false;
            }

            public bool MoveCurrentToPrevious()
            {
                return false;
            }

            public bool MoveCurrentTo(object item)
            {
                return false;
            }

            public bool MoveCurrentToPosition(int position)
            {
                return false;
            }

            public CultureInfo Culture { get; set; }

            public IEnumerable SourceCollection => collection;

            public bool CanFilter => false;
            public bool CanSort => false;
            public bool CanGroup => false;

            public Predicate<object> Filter
            {
                get { return null; }
                set { }
            }

            public SortDescriptionCollection SortDescriptions
            {
                get { return null; }
            }

            public ObservableCollection<GroupDescription> GroupDescriptions
            {
                get { return null; }
            }

            public ReadOnlyObservableCollection<object> Groups
            {
                get { return null; }
            }

            public bool IsEmpty => collection.Count == 0;

            public object CurrentItem => currentItem;

            public int CurrentPosition => currentPosition;

            public bool IsCurrentAfterLast =>
                CheckFlag(CollectionViewFlags.IsCurrentAfterLast);

            public bool IsCurrentBeforeFirst =>
                CheckFlag(CollectionViewFlags.IsCurrentBeforeFirst);

            public event CurrentChangingEventHandler CurrentChanging;
            public event EventHandler CurrentChanged;
            public event PropertyChangedEventHandler PropertyChanged;

            private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class TraceLogWindowViewModel : ViewModel
    {
        private readonly object eventsMutex = new object();
        private readonly DispatcherTimer updateStatisticsTimer;
        private readonly List<TraceProviderSpec> providers = new List<TraceProviderSpec>();

        private string status;
        private bool showStatistics;
        private string formattedEventStatistics;
        private string formattedBufferStatistics;
        private bool autoLog;
        private bool isCollecting;
        private TraceSession session;

        public TraceLogWindowViewModel(IOperationalModeProvider modeProvider)
        {
            if (modeProvider != null)
                modeProvider.OperationalModeChanged += OnOperationalModeChanged;

            var eventsTable = new TableControlViewModel();

            var header = eventsTable.Headers;
            header.Add(new TableHeaderViewModel {
                Header = "Time",
                ColumnWidth = 100,
                MemberName = "Time",
                StringFormat = "hh:mm:ss.fffffff"
            });
            header.Add(new TableHeaderViewModel { Header = "PID", ColumnWidth = 50, MemberName = "ProcessId" });
            header.Add(new TableHeaderViewModel { Header = "TID", ColumnWidth = 50, MemberName = "ThreadId" });
            header.Add(new TableHeaderViewModel {
                Header = "ProviderId",
                ColumnWidth = 50,
                MemberName = "ProviderId"
            });
            header.Add(new TableHeaderViewModel { Header = "Provider", ColumnWidth = 50, MemberName = "Provider" });
            header.Add(new TableHeaderViewModel { Header = "Id", ColumnWidth = 25, MemberName = "Id" });
            header.Add(new TableHeaderViewModel { Header = "Version", ColumnWidth = 20, MemberName = "Version" });
            header.Add(new TableHeaderViewModel { Header = "Channel", ColumnWidth = 20, MemberName = "Channel" });
            header.Add(new TableHeaderViewModel { Header = "ChannelId", ColumnWidth = 20, MemberName = "ChannelId" });
            header.Add(new TableHeaderViewModel { Header = "Level", ColumnWidth = 20, MemberName = "Level" });
            header.Add(new TableHeaderViewModel { Header = "LevelId", ColumnWidth = 20, MemberName = "LevelId" });
            header.Add(new TableHeaderViewModel { Header = "Task", ColumnWidth = 40, MemberName = "Task" });
            header.Add(new TableHeaderViewModel { Header = "TaskId", ColumnWidth = 40, MemberName = "TaskId" });
            header.Add(new TableHeaderViewModel { Header = "Opcode", ColumnWidth = 40, MemberName = "Opcode" });
            header.Add(new TableHeaderViewModel { Header = "OpcodeId", ColumnWidth = 40, MemberName = "OpcodeId" });
            header.Add(new TableHeaderViewModel { Header = "Keywords", ColumnWidth = 50, MemberName = "Keywords" });
            header.Add(new TableHeaderViewModel {
                Header = "KeywordMask",
                ColumnWidth = 50,
                MemberName = "KeywordMask",
                StringFormat = "X"
            });
            header.Add(new TableHeaderViewModel { Header = "Message", ColumnWidth = 500, MemberName = "Message" });

            Events = new ObservableCollection<TraceEvent>();
            Events.Add(new TraceEvent { Id = 1 });
            Events.Add(new TraceEvent { Id = 2 });
            //Events = new EventsCollection();
            GridModel = new GridViewModel();
            BindingOperations.EnableCollectionSynchronization(Events, eventsMutex);

            syncCtx = new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher);
            SynchronizationContext.SetSynchronizationContext(syncCtx);
            scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            taskFactory = new TaskFactory(scheduler);

            EventsTable = eventsTable;
            Statistics = new TraceLogStatsModel();
            ShowStatistics = true;
            AutoLog = true;

            updateStatisticsTimer = new DispatcherTimer(DispatcherPriority.Background);
            updateStatisticsTimer.Interval = TimeSpan.FromSeconds(1);
            updateStatisticsTimer.Tick += (s, a) => UpdateStats();

            var spec = new TraceProviderSpec(new Guid("716EFEF7-5AC2-4EE0-8277-D9226411A155"));
            spec.SetManifest(@"C:\Users\nrieck\dev\ffmf\src\Sculptor\Sculptor.man");
            spec.IncludeSecurityId = true;
            spec.IncludeStackTrace = true;
            spec.IncludeTerminalSessionId = true;
            providers.Add(spec);
        }

        public class GridViewModel : ViewModel, IVirtualizedDataGridViewModel
        {
            private readonly PropertyChangedEventArgs rowCountChangedArgs =
                new PropertyChangedEventArgs(nameof(RowCount));

            public GridViewModel()
            {
                var col1 = new VirtualizedDataGridViewColumn("Id") {
                    Width = 200,
                    IsVisible = true,
                    IsResizable = true
                };

                var col2 = new VirtualizedDataGridViewColumn("Channel") {
                    Width = 200,
                    IsVisible = true,
                    IsResizable = true
                };

                var col3 = new VirtualizedDataGridViewColumn("Level") {
                    Width = 200,
                    IsVisible = true,
                    IsResizable = true
                };

                var col4 = new VirtualizedDataGridViewColumn("Task") {
                    Width = 200,
                    IsVisible = true,
                    IsResizable = true
                };

                var columns = new[] { col1, col2, col3, col4 };

                CellsPresenterViewModel = new VirtualizedDataGridCellsPresenterViewModel(this);

                ColumnsViewModel = new VirtualizedDataGridColumnsViewModel(this);
                foreach (var column in columns)
                    ColumnsViewModel.WritableColumns.Add(
                        new VirtualizedDataGridColumnViewModel(ColumnsViewModel, column));

                ColumnsViewModel.RefreshAllObservableCollections();

                RowCount = 40;
            }

            public event ItemEventHandler<bool> Updated;

            public IVirtualizedDataGridCellsPresenterViewModel CellsPresenterViewModel { get; }

            public VirtualizedDataGridColumnsViewModel ColumnsViewModel { get; }

            public int RowCount { get; private set; }

            public void UpdateRowCount(int newCount)
            {
                if (newCount == RowCount)
                    return;

                RowCount = newCount;
                //Application.Current.Dispatcher.Invoke(delegate {
                //    Updated?.Invoke(this, trueEventArgs);
                //});
                RaisePropertyChanged(rowCountChangedArgs);
            }

            public void RaiseUpdate(bool refreshViewModelFromModel = true)
            {
                Updated?.Invoke(this, new ItemEventArgs<bool>(refreshViewModelFromModel));
            }
        }

        public TableControlViewModel EventsTable { get; }
        public ObservableCollection<TraceEvent> Events { get; }
        //public EventsCollection Events { get; }
        public IVirtualizedDataGridViewModel GridModel { get; }

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
            return providers.Count > 0;
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
                session?.Clear();
                Events.Clear();
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
                session = new TraceSession(new List<TraceEvent>(), providers);
                session.NewEvents += OnNewEvents;
                //Events.Attach(session);
                session.Start();
                IsCollecting = true;
                updateStatisticsTimer.Start();
            } catch (Exception ex) {
                Status = ex.ToString();
            }
        }

        private DateTime lastUpdateEvent = DateTime.MinValue;
        private TimeSpan updateThreshold = TimeSpan.FromMilliseconds(50);
        private DispatcherSynchronizationContext syncCtx;
        private TaskScheduler scheduler;
        private TaskFactory taskFactory;

        private void OnNewEvents(int newCount)
        {
            var now = DateTime.UtcNow;
            var elapsed = now - lastUpdateEvent;

            if (elapsed < updateThreshold)
                return;

            lastUpdateEvent = now;
            taskFactory.StartNew(() => Update(newCount));
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
                session.NewEvents -= OnNewEvents;
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
            foreach (var provider in providers)
                viewModel.Providers.Add(new TraceProviderSpecViewModel(provider));

            var window = new TraceSessionSettingsWindow();
            window.DataContext = viewModel;
            if (window.ShowModal() != true)
                return;

            providers.Clear();
            providers.AddRange(viewModel.Providers.Select(x => x.ToModel()));
        }

        private async void UpdateStats()
        {
            if (session == null)
                return;

            var stats = session.Query();

            Statistics.TotalEvents = (uint)Events.Count;
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
    }

    public class TableHeaderViewModel : ViewModel
    {
        public string Header { get; set; }

        public double ColumnWidth { get; set; }

        public string MemberName { get; set; }

        public string StringFormat { get; set; }

        private bool isVisible = true;

        public bool IsVisible
        {
            get { return isVisible; }
            set { SetProperty(ref isVisible, value); }
        }

        public bool ResetColumnWidth { get; set; }

        public double MinWidth => double.MinValue;

        public double MaxWidth => double.MaxValue;

        public bool IsStar => false;
    }

    public class TraceLogWindowDesignTimeModel : TraceLogWindowViewModel
    {
        public TraceLogWindowDesignTimeModel()
            : base(null)
        {
            //Events.Add(new TraceEvent {
            //    Id = 4452,
            //    Version = 1,
            //    ChannelId = 3,
            //    LevelId = 2,
            //    OpcodeId = 10,
            //    TaskId = 1000,
            //    KeywordMask = 0x8000000,
            //    Time = new DateTime(2000, 10, 11, 12, 13, 14),
            //    Message = "First event",
            //    Formatted = true
            //});

            //Events.Add(new TraceEvent {
            //    Id = 4453,
            //    Version = 1,
            //    ChannelId = 3,
            //    LevelId = 2,
            //    OpcodeId = 11,
            //    TaskId = 2000,
            //    KeywordMask = 0x8000000,
            //    Time = new DateTime(2000, 10, 11, 12, 13, 15),
            //    Message = "Second event",
            //    Formatted = true
            //});

            //Events.Add(new TraceEvent {
            //    Id = 4454,
            //    Version = 1,
            //    ChannelId = 3,
            //    LevelId = 3,
            //    OpcodeId = 12,
            //    TaskId = 3000,
            //    KeywordMask = 0x8000000,
            //    Time = new DateTime(2000, 10, 11, 12, 14, 14),
            //    Message = "Another event",
            //    Formatted = true
            //});

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

        public TraceProviderSpecViewModel(TraceProviderSpec provider)
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

        public TraceProviderSpec ToModel()
        {
            var spec = new TraceProviderSpec(Id);
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
}
