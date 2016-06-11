namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Design;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Threading;
    using EventTraceKit.VsExtension.Controls;
    using Microsoft.VisualStudio.PlatformUI;
    using Microsoft.VisualStudio.Shell;
    using Task = System.Threading.Tasks.Task;

    public class TraceLogWindowViewModel : ViewModel
    {
        private readonly DispatcherTimer updateStatisticsTimer;
        private readonly List<TraceProviderSpec> providers = new List<TraceProviderSpec>();

        private string status;
        private bool showStatistics;
        private string formattedEventStatistics;
        private string formattedBufferStatistics;
        private bool autoLog;
        private bool isCollecting;
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

            EventsDataView = new TraceEventsView();
            GridModel = new VirtualizedDataGridViewModel(EventsDataView);

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

            var spec = new TraceProviderSpec(new Guid("716EFEF7-5AC2-4EE0-8277-D9226411A155"));
            spec.SetManifest(@"C:\Users\nrieck\dev\ffmf\src\Sculptor\Sculptor.man");
            spec.IncludeSecurityId = true;
            spec.IncludeStackTrace = true;
            spec.IncludeTerminalSessionId = true;
            providers.Add(spec);
        }

        public TraceEventsView EventsDataView { get; }
        public VirtualizedDataGridViewModel GridModel { get; }

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

    public class DataViewColumnsCollection
        : IDataViewColumnsCollection
    {
        private readonly TraceEventsView view;

        public DataViewColumnsCollection(TraceEventsView view)
        {
            this.view = view;
        }

        public int IndexOf(IDataColumn column)
        {
            return view.GetDataColumnIndex(column);
        }

        public int Count => view.ColumnCount;

        public IDataColumn this[int columnIndex] => view.GetDataColumn(columnIndex);

        public IEnumerator<IDataColumn> GetEnumerator()
        {
            for (int index = 0; index < Count; ++index)
                yield return this[index];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<IDataColumn>)this).GetEnumerator();
        }
    }

    public static class FreezableExtensions
    {
        public static T EnsureFrozen<T>(this T freezable)
            where T : Freezable
        {
            if (freezable == null)
                throw new ArgumentNullException(nameof(freezable));

            if (freezable.IsFrozen)
                return freezable;

            if (!freezable.CanFreeze)
                freezable = (T)freezable.CloneCurrentValue();

            freezable.Freeze();
            return freezable;
        }
    }

    public class HdvColumnViewModelPreset :
        FreezableCustomSerializerAccessBase
        , IComparable<HdvColumnViewModelPreset>
        , ICloneable
    {
        #region public Guid Id

        public static readonly DependencyProperty IdProperty =
            DependencyProperty.Register(
                nameof(Id),
                typeof(Guid),
                typeof(HdvColumnViewModelPreset),
                new PropertyMetadata(Guid.Empty));

        [SerializePropertyInProfile("Id")]
        public Guid Id
        {
            get { return (Guid)GetValue(IdProperty); }
            set { SetValue(IdProperty, value); }
        }

        #endregion

        #region public string Name

        public static readonly DependencyProperty NameProperty =
            DependencyProperty.Register(
                nameof(Name),
                typeof(string),
                typeof(HdvColumnViewModelPreset),
                PropertyMetadataUtils.DefaultNull);

        [SerializePropertyInProfile("Name")]
        public string Name
        {
            get { return (string)GetValue(NameProperty); }
            set { SetValue(NameProperty, value); }
        }

        #endregion

        #region public string HelpText

        public static readonly DependencyProperty HelpTextProperty =
            DependencyProperty.Register(
                nameof(HelpText),
                typeof(string),
                typeof(HdvColumnViewModelPreset),
                PropertyMetadataUtils.DefaultNull);

        [SerializePropertyInProfile("HelpText")]
        public string HelpText
        {
            get { return (string)GetValue(HelpTextProperty); }
            set { SetValue(HelpTextProperty, value); }
        }

        #endregion

        #region public int Width

        public static readonly DependencyProperty WidthProperty =
            DependencyProperty.Register(
                nameof(Width),
                typeof(int),
                typeof(HdvColumnViewModelPreset),
                new PropertyMetadata(
                    0, null, CoerceWidth));

        [SerializePropertyInProfile("Width")]
        public int Width
        {
            get { return (int)GetValue(WidthProperty); }
            set { SetValue(WidthProperty, value); }
        }

        private static object CoerceWidth(DependencyObject d, object baseValue)
        {
            int width = (int)baseValue;
            return Boxed.Int32(width.Clamp(0, 10000));
        }

        #endregion

        #region public bool IsVisible

        public static readonly DependencyProperty IsVisibleProperty =
            DependencyProperty.Register(
                nameof(IsVisible),
                typeof(bool),
                typeof(HdvColumnViewModelPreset),
                new PropertyMetadata(Boxed.False));

        [SerializePropertyInProfile("IsVisible")]
        public bool IsVisible
        {
            get { return (bool)GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, Boxed.Bool(value)); }
        }

        #endregion

        #region public TextAlignment TextAlignment

        public static readonly DependencyProperty TextAlignmentProperty =
            DependencyProperty.Register(
                nameof(TextAlignment),
                typeof(TextAlignment),
                typeof(HdvColumnViewModelPreset),
                new PropertyMetadata(TextAlignment.Left));

        [SerializePropertyInProfile("TextAlignment")]
        public TextAlignment TextAlignment
        {
            get { return (TextAlignment)GetValue(TextAlignmentProperty); }
            set { SetValue(TextAlignmentProperty, value); }
        }

        #endregion

        #region public string CellFormat

        public static readonly DependencyProperty CellFormatProperty =
            DependencyProperty.Register(
                nameof(CellFormat),
                typeof(string),
                typeof(HdvColumnViewModelPreset),
                new PropertyMetadata(null));

        [SerializePropertyInProfile("CellFormat")]
        public string CellFormat
        {
            get { return (string)GetValue(CellFormatProperty); }
            set { SetValue(CellFormatProperty, value); }
        }

        #endregion

        protected override Freezable CreateInstanceCore()
        {
            return new HdvColumnViewModelPreset();
        }

        public new HdvColumnViewModelPreset Clone()
        {
            return (HdvColumnViewModelPreset)base.Clone();
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public int CompareTo(HdvColumnViewModelPreset other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            if (ReferenceEquals(this, other))
                return 0;

            int cmp;
            bool dummy =
                ComparisonUtils.CompareValueT(out cmp, IsVisible, other.IsVisible) &&
                ComparisonUtils.Compare(out cmp, Id, Id) &&
                ComparisonUtils.Compare(out cmp, TextAlignment, other.TextAlignment) &&
                ComparisonUtils.CompareT(out cmp, CellFormat, other.CellFormat) &&
                ComparisonUtils.CompareValueT(out cmp, Width, other.Width);

            return cmp;
        }

        protected override void CloneCore(Freezable sourceFreezable)
        {
            base.CloneCore(sourceFreezable);
        }
    }

    public class TraceEventsView : DependencyObject, IDataView
    {
        private readonly List<DataColumn> columns = new List<DataColumn>();

        public IDataViewColumnsCollection Columns =>
            new DataViewColumnsCollection(this);

        public IDataViewColumnsCollection VisibleColumns =>
            new DataViewColumnsCollection(this);

        public TraceEventsView()
        {
            workManager = new WorkManager(Dispatcher);

            var providerNamePreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("934D2438-65F3-4AE9-8FEA-94B81AA5A4A6"),
                    Name = "Provider Name",
                    IsVisible = true,
                    Width = 200
                }.EnsureFrozen();
            var taskNamePreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("730765B3-2E42-43E7-8B26-BAB7F4999E69"),
                    Name = "Task Name",
                    IsVisible = true,
                    Width = 80
                }.EnsureFrozen();
            var opcodeOrTypePreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("F08CCD14-FE1E-4D9E-BE6C-B527EA4B25DA"),
                    Name = "Opcode/Type ",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            var levelPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("388591F3-43B2-4E68-B080-0B1A48D33559"),
                    Name = "Level",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            var versionPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("215AB0D7-BEC9-4A70-96C4-028EE3404F09"),
                    Name = "Version",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            var taskPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("CE90F4D8-0FDE-4324-8D39-5BF74C8F4D9B"),
                    Name = "Task",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            var keywordPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("62DC8843-C7BF-45F0-AC61-644395D53409"),
                    Name = "Keyword",
                    IsVisible = false,
                    Width = 80,
                    TextAlignment = TextAlignment.Right,
                    CellFormat = "x"
                }.EnsureFrozen();
            var channelPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("CF9373E2-5876-4F84-BB3A-F6C878D36F86"),
                    Name = "Channel",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            var idPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("0FE03A19-FBCB-4514-9441-2D0B1AB5E2E1"),
                    Name = "Id",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            var opcodeNamePreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("99C0A192-174F-4DD5-AFD8-32F513506E88"),
                    Name = "Opcode Name",
                    IsVisible = true,
                    Width = 80
                }.EnsureFrozen();
            var eventNamePreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("B82277B9-7066-4938-A959-EABF0C689087"),
                    Name = "Event Name",
                    IsVisible = true,
                    Width = 100
                }.EnsureFrozen();
            var messagePreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("89F731F6-D4D2-40E8-9615-6EB5A5A68A75"),
                    Name = "Message",
                    IsVisible = true,
                    Width = 100
                }.EnsureFrozen();
            var providerIdPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("9B9DAF0F-EAC6-43FE-B68F-EAF0D9A4AFB9"),
                    Name = "Provider Id",
                    IsVisible = false,
                    Width = 100
                }.EnsureFrozen();
            var eventTypePreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("AC2A6011-BCB3-4721-BEF1-E1DEC50C073D"),
                    Name = "Event Type",
                    IsVisible = false,
                    Width = 100
                }.EnsureFrozen();
            var cpuPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("452A05E3-A1C0-4686-BB6B-C39AFF2F24BE"),
                    Name = "Cpu",
                    IsVisible = true,
                    Width = 30
                }.EnsureFrozen();
            var threadIdPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("6BEB4F24-53DC-4A9D-8EEA-ED8F69990349"),
                    Name = "ThreadId",
                    IsVisible = true,
                    Width = 50
                }.EnsureFrozen();
            var processIdPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("7600E8FD-D7C2-4BA4-9DE4-AADE5230DC53"),
                    Name = "Event Header ProcessId",
                    IsVisible = true,
                    Width = 50,
                    HelpText = "(0 = PID Not Found)"
                }.EnsureFrozen();
            var userDataLengthPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("813F4638-8D41-4EAD-94DD-9A4AFFEFA701"),
                    Name = "UserDataLength",
                    IsVisible = false,
                    Width = 30
                }.EnsureFrozen();
            var activityIdPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("21695563-AC1B-4953-9B9B-991353DBC082"),
                    Name = "etw:ActivityId",
                    IsVisible = false,
                    Width = 60
                }.EnsureFrozen();
            var relatedActivityIdPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("83B1BF6F-5E8D-4143-A84B-8C16ED1EF6BD"),
                    Name = "etw:Related ActivityId",
                    IsVisible = false,
                    Width = 60
                }.EnsureFrozen();
            var userSecurityIdentifierPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("F979E52D-EE1B-4A7E-950F-28103990D11B"),
                    Name = "etw:UserSid",
                    IsVisible = false,
                    Width = 60
                }.EnsureFrozen();
            var sessionIdPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("84FC6D0C-5FFD-40D9-8C3B-F0EB8F8F2D1B"),
                    Name = "etw:SessionId",
                    IsVisible = false,
                    Width = 60
                }.EnsureFrozen();
            var eventKeyPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("4F0679D2-B5E7-4AB1-ADF7-FCDEBEEF801B"),
                    Name = "etw:EventKey",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            var timestampGeneratorPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("9C75AA69-046E-42AE-B594-B4AD24335A0A"),
                    Name = "Time",
                    IsVisible = true,
                    Width = 80,
                    TextAlignment = TextAlignment.Right,
                    CellFormat = "sN"
                }.EnsureFrozen();
            var datetimeGeneratorPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("8823874B-917D-4D64-ABDF-EA29E6C87789"),
                    Name = "DateTime (Local)",
                    Width = 150,
                }.EnsureFrozen();
            var modernProcessDataPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("DC7E68B0-E753-47DF-8357-61BEC093E405"),
                    Name = "Process",
                    IsVisible = true,
                    Width = 150
                }.EnsureFrozen();
            var processNamePreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("BB09F706-FE79-43AA-A103-120801DAC28F"),
                    Name = "Process Name",
                    IsVisible = true,
                    Width = 150
                }.EnsureFrozen();
            var stackTopPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("D55383F4-D0ED-404B-98A8-DC9CF4533FBF"),
                    Name = "Stack",
                    IsVisible = false,
                    Width = 100
                }.EnsureFrozen();
            var threadStartModulePreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("D58C42B0-818D-4D83-BD99-9DA872E77B54"),
                    Name = "Thread Start Module",
                    IsVisible = false,
                    Width = 100
                }.EnsureFrozen();
            var threadStartFunctionPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("125BB527-34C6-4A33-82B8-05E3B0C7A591"),
                    Name = "Thread Start Function",
                    IsVisible = false,
                    Width = 100
                }.EnsureFrozen();
            var countPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("3EF367F3-62FD-453B-88F7-67D97D3F12F8"),
                    Name = "Count",
                    IsVisible = true,
                    Width = 80
                }.EnsureFrozen();

            defaultPreset = new HdvViewModelPreset();

            AddColumn(providerNamePreset, new DataColumn());
            AddColumn(taskNamePreset, new DataColumn());
            AddColumn(opcodeOrTypePreset, new DataColumn());
            AddColumn(levelPreset, new DataColumn());
            AddColumn(versionPreset, new DataColumn());
            AddColumn(taskPreset, new DataColumn());
            AddColumn(keywordPreset, new DataColumn());
            AddColumn(channelPreset, new DataColumn());
            AddColumn(idPreset, new DataColumn());
            AddColumn(opcodeNamePreset, new DataColumn());
            AddColumn(eventNamePreset, new DataColumn());
            AddColumn(messagePreset, new DataColumn());
            AddColumn(providerIdPreset, new DataColumn());
            AddColumn(eventTypePreset, new DataColumn());
            AddColumn(cpuPreset, new DataColumn());
            AddColumn(threadIdPreset, new DataColumn());
            AddColumn(processIdPreset, new DataColumn());
            AddColumn(userDataLengthPreset, new DataColumn());
            AddColumn(activityIdPreset, new DataColumn());
            AddColumn(relatedActivityIdPreset, new DataColumn());
            AddColumn(userSecurityIdentifierPreset, new DataColumn());
            AddColumn(sessionIdPreset, new DataColumn());
            AddColumn(eventKeyPreset, new DataColumn());
            AddColumn(timestampGeneratorPreset, new DataColumn());
            AddColumn(datetimeGeneratorPreset, new DataColumn());
            AddColumn(modernProcessDataPreset, new DataColumn());
            AddColumn(processNamePreset, new DataColumn());
            AddColumn(stackTopPreset, new DataColumn());
            AddColumn(threadStartModulePreset, new DataColumn());
            AddColumn(threadStartFunctionPreset, new DataColumn());
            AddColumn(countPreset, new DataColumn());

            HdvViewModelPreset = defaultPreset;
        }

        private HdvViewModelPreset defaultPreset;

        private void AddColumn(HdvColumnViewModelPreset preset, DataColumn column)
        {
            column.Name = preset.Name;
            column.Width = preset.Width;
            column.IsVisible = preset.IsVisible;
            column.IsResizable = true;
            column.TextAlignment = preset.TextAlignment;
            columns.Add(column);
            defaultPreset.ConfigurableColumns.Add(preset);
        }

        public CellValue GetCellValue(int rowIndex, int columnIndex)
        {
            var result = new CellValue(string.Format("{0}:{1}", rowIndex, columnIndex), null, null);
            //this.workManager.BackgroundThread.Send(delegate {
            //    result = this.hdv.GetCellValue(rowIndex, columnIndex);
            //    result.PrecomputeString();
            //});
            return result;
        }

        public void UpdateRowCount(int newCount)
        {
            if (newCount == RowCount)
                return;

            RowCount = newCount;
            //Application.Current.Dispatcher.Invoke(delegate {
            //    Updated?.Invoke(this, trueEventArgs);
            //});
            //RaisePropertyChanged(rowCountChangedArgs);
        }

        public int EventCount => RowCount;

        public int RowCount { get; private set; }
        public int ColumnCount => columns.Count;

        public IDataColumn GetDataColumn(int columnIndex)
        {
            return columns[columnIndex];
        }

        public void Clear()
        {
        }

        public int GetDataColumnIndex(IDataColumn column)
        {
            var dataColumn = column as DataColumn;
            if (dataColumn == null)
                return -1;
            return columns.IndexOf(dataColumn);
        }

        #region

        private static readonly DependencyPropertyKey IsReadyPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsReady),
                typeof(bool),
                typeof(TraceEventsView),
                new PropertyMetadata(Boxed.True,
                    OnIsReadyChanged));

        public static readonly DependencyProperty IsReadyProperty = IsReadyPropertyKey.DependencyProperty;

        public bool IsReady
        {
            get { return (bool)GetValue(IsReadyProperty); }
            private set { SetValue(IsReadyPropertyKey, Boxed.Bool(value)); }
        }

        private static void OnIsReadyChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TraceEventsView)d).OnIsReadyChanged(e.NewValue);
        }

        private void OnIsReadyChanged(object newValue)
        {
        }

        #endregion

        #region

        public static readonly DependencyProperty HdvViewModelPresetProperty =
            DependencyProperty.Register(
                nameof(HdvViewModelPreset),
                typeof(HdvViewModelPreset),
                typeof(TraceEventsView),
                new PropertyMetadata(
                    null,
                    (s, e) => ((TraceEventsView)s).HdvViewModelPresetPropertyChanged(e),
                    (dO, bV) => ((TraceEventsView)dO).CoerceHdvViewModelPresetProperty(bV)));

        public HdvViewModelPreset HdvViewModelPreset
        {
            get { return (HdvViewModelPreset)GetValue(HdvViewModelPresetProperty); }
            set { SetValue(HdvViewModelPresetProperty, value); }
        }

        private bool isInitializedWithFirstPreset;
        private bool shouldApplyPreset;
        private HdvViewModelPreset presetBeingApplied;
        private HdvViewModelPreset presetToApplyOnReady;

        private void HdvViewModelPresetPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            var preset = (HdvViewModelPreset)e.NewValue;
            if (preset != null && (!isInitializedWithFirstPreset || shouldApplyPreset)) {
                if (!preset.IsFrozen)
                    throw new ArgumentException("Preset must be frozen before being applied");

                if (!IsReady && !isInitializedWithFirstPreset) {
                    IsReady = true;
                    isInitializedWithFirstPreset = true;
                }
                if (!IsReady) {
                    var b = presetToApplyOnReady == null &&
                            presetBeingApplied != null &&
                            !presetBeingApplied.Equals(preset);
                    var b1 = presetToApplyOnReady != null &&
                             !presetToApplyOnReady.Equals(preset);
                    if (b || b1) {
                        presetToApplyOnReady = preset;
                    }
                    return;
                }
                //this.UpdateHelpTextFromPreset(preset.HelpText);
                //ColumnChangingContext columnChangingContext = HdvViewModel.columnChangingContext;
                //if (columnChangingContext != null) {
                //    this.columnMetadataCollection.PresetMetadataEntries = preset.ColumnMetadataEntries;
                //    if (this.HasAnyVisibleColumnAffectedByChange(preset, columnChangingContext.ColumnChangingPredicate)) {
                //        this.DisableTableForAsyncOperation();
                //        this.presetToApplyOnReady = preset;
                //        AddChangingHdvViewModel(this);
                //        return;
                //    }
                //}
                presetBeingApplied = preset;
                //if (this.presenterRowSelectionToUseOnReady == null) {
                //    this.SaveTableRowSelectionAndFocus();
                //}

                string presetName = preset.Name ?? string.Empty;
                //this.Hdv.BeginDataUpdate();
                ApplyPresetToGridModel(
                    preset,
                    () => ContinuePresetAfterGridModelInSync(presetName, preset), -1);
            }
            //this.HdvViewModelPresetChanged.Raise<HdvViewModelPreset>(this, e);
            //this.ResetIsPresetError();
        }

        private object CoerceHdvViewModelPresetProperty(object baseValue)
        {
            var preset = (HdvViewModelPreset)baseValue;
            if (preset == null)
                return null;

            //bool includeDynamicColumns = this.IsUnmodifiedBuiltInPreset(preset);
            //HdvViewModelPreset compatiblePreset = preset.CreateCompatiblePreset(
            //    this.templatePreset, this.viewCreationInfoCollection, includeDynamicColumns);
            HdvViewModelPreset compatiblePreset = preset;

            //shouldApplyPreset = !compatiblePreset.IsModifiedFromUI;
            //compatiblePreset.IsModifiedFromUI = false;
            compatiblePreset.Freeze();
            //this.cachedHdvViewModelPreset = compatiblePreset;
            return compatiblePreset;
        }

        #endregion

        internal void EnableTableAfterAsyncOperation()
        {
            IsReady = true;
            //this.UpdateSelectionIndicesAfterTableChanged();
        }

        internal void DisableTableForAsyncOperation()
        {
            IsReady = false;
        }

        private void ApplyPresetToGridModel(
            HdvViewModelPreset preset, Action callbackOnComplete, int maxPreservedKeyCount = -1)
        {
            if (!IsReady)
                ExceptionUtils.ThrowInvalidOperationException(
                    "Must be ready before applying preset to grid model.");

            DisableTableForAsyncOperation();
            //DataColumnViewInfo[] columnViewInfos = this.viewCreationInfoCollection.GetDataColumnViewInfosFromPreset(preset, this).ToArray<DataColumnViewInfo>();
            //this.WaitForReadOperationToComplete();
            WorkManager.BackgroundThread.Post(delegate {
                //this.Hdv.BeginDataUpdate();
                //int preservedKeyCount = this.hdv.ApplyColumnView(columnViewInfos, maxPreservedKeyCount);
                //this.columnViewReady = true;
                //this.Hdv.EndDataUpdate();
                WorkManager.UIThread.Post(callbackOnComplete);
            });
        }

        private void ContinuePresetAfterGridModelInSync(
            string presetName, HdvViewModelPreset preset)
        {
            bool ignoreInitialSelection = false;
            bool ignoreInitialExpansion = false;
            columnsViewModel.ApplyPresetAssumeGridModelInSync(preset);
            ContinueAsyncOperation(
                () => CompleteApplyPreset(presetName, ignoreInitialSelection, ignoreInitialExpansion), true);
        }

        private void CompleteApplyPreset(
            string presetName, bool ignoreInitialSelection,
            bool ignoreInitialExpansion)
        {

            //if (!this.Hdv.EndDataUpdate()) {
            //    ExceptionUtils.ThrowInternalErrorException("Expected data update nesting depth to be 0");
            //}

            if (!ignoreInitialSelection) {
                //this.ReapplyInitialSelection();
            }
            CompleteAsyncOrSyncUpdate();
        }

        private WorkManager workManager;
        private bool refreshViewModelFromModelOnReady;
        private bool refreshViewModelOnUpdateRequest;
        private VirtualizedDataGridColumnsViewModel columnsViewModel;

        public VirtualizedDataGridColumnsViewModel ColumnsViewModel
        {
            set { columnsViewModel = value; }
        }

        internal WorkManager WorkManager
        {
            get
            {
                return workManager;
            }
        }

        private void ContinueAsyncOperation(Action callback, bool refreshViewModelFromModelOnReady = true)
        {
            if (IsReady)
                ExceptionUtils.ThrowInvalidOperationException(
                    "The system is ready, you aren't continuing an async operation?");
            this.refreshViewModelFromModelOnReady |= refreshViewModelFromModelOnReady;
            WorkManager.BackgroundThread.Post(callback);
        }

        private void CompleteAsyncOrSyncUpdate()
        {
            WorkManager.UIThread.Send(() => CompleteAsyncUpdate());
        }

        private bool CompleteAsyncUpdate()
        {
            bool flag = false;
            //this.IsTableRefreshing = true;
            EnableTableAfterAsyncOperation();
            //if (this.checkForColumnChangingOnReady) {
            //    this.checkForColumnChangingOnReady = false;
            //    ColumnChangingContext columnChangingContext = HdvViewModel.columnChangingContext;
            //    HdvViewModelPreset preset = presetBeingApplied ?? presetToApplyOnReady;
            //    if (this.HasAnyVisibleColumnAffectedByChange(preset, columnChangingContext.ColumnChangingPredicate)) {
            //        this.DisableTableForAsyncOperation();
            //        AddChangingHdvViewModel(this);
            //        flag = true;
            //    }
            //    countBusyHdvViewModels--;
            //    CompleteUpdateWhenAllReady();
            //}
            if (!flag && (presetToApplyOnReady != null)) {
                HdvViewModelPreset = presetToApplyOnReady;
                presetToApplyOnReady = null;
                flag = true;
            }

            //if (!flag && (this.hdvViewModelToCopyStateOnReady != null)) {
            //    this.TryCopyAllStateFrom(this.hdvViewModelToCopyStateOnReady);
            //    this.hdvViewModelToCopyStateOnReady = null;
            //    flag = true;
            //}

            //if (flag) {
            //    if (this.selectionBookmarkToApplyOnReady != null) {
            //        this.selectionBookmarkToApplyOnReady.ConvertRowsToRowIds();
            //    }
            //    if (this.focusBookmarkToApplyOnReady != null) {
            //        this.focusBookmarkToApplyOnReady.ConvertRowsToRowIds();
            //    }
            //} else {
            //    flag = this.RestoresSelectionAndFocusFromBookmarksAsync();
            //}

            if (!flag) {
                //this.UpdateDynamicHeader();
                bool refreshViewModelFromModel = refreshViewModelFromModelOnReady && IsReady;
                if (refreshViewModelFromModel)
                    refreshViewModelFromModelOnReady = false;

                bool flag3 = this.RequestUpdate(refreshViewModelFromModel);
                if (refreshViewModelFromModel && !flag3) {
                    ExceptionUtils.ThrowInternalErrorException("We should have sent an update for the hdvviewmodel, but didn't");
                }
                //this.IsTableRefreshing = false;
            }
            return !flag;
        }

        public bool RequestUpdate(bool refreshViewModelFromModel = true)
        {
            if (!IsReady) {
                refreshViewModelOnUpdateRequest |= refreshViewModelFromModel;
                return false;
            }

            RaiseUpdate(refreshViewModelFromModel);
            return true;
        }

        public event ItemEventHandler<bool> Updated;

        public void RaiseUpdate(bool refreshViewModelFromModel = true)
        {
            VerifyAccess();

            Updated?.Invoke(this, new ItemEventArgs<bool>(refreshViewModelFromModel));
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

        public async void Send(Action action)
        {
            await Task.Run(action);
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
