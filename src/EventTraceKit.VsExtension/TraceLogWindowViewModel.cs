namespace EventTraceKit.VsExtension
{
    using System;
    using System.ComponentModel.Design;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Threading;
    using Controls;
    using Microsoft.VisualStudio.Shell;
    using Task = System.Threading.Tasks.Task;

    public class TraceLogWindowViewModel : ViewModel, IEventInfoSource
    {
        private readonly DispatcherTimer updateStatisticsTimer;
        private TraceSessionDescriptor sessionDescriptor = new TraceSessionDescriptor();

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
        private EventSymbolSource eventSymbolSource = new EventSymbolSource();
        private readonly Func<ISolutionFileGatherer> gathererFactory;

        public TraceLogWindowViewModel(
            IOperationalModeProvider modeProvider,
            Func<ISolutionFileGatherer> gathererFactory)
        {
            this.gathererFactory = gathererFactory;
            if (modeProvider != null)
                modeProvider.OperationalModeChanged += OnOperationalModeChanged;

            var tableTuple = new GenericEventsViewModelSource().CreateTable(this, eventSymbolSource);
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

        private async void ToggleCapture()
        {
            if (IsCollecting)
                StopCapture();
            else if (CanStartCapture())
                await StartCapture();
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

        public async Task StartCapture()
        {
            if (!CanStartCapture() || session != null)
                return;

            Status = null;
            try {
                traceLog = new TraceLog();
                traceLog.EventsChanged += OnEventsChanged;
                session = new TraceSession(sessionDescriptor);
                await session.StartAsync(traceLog);
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

        private TraceSessionSettingsViewModel settingsViewModel;

        private void Configure()
        {
            if (settingsViewModel == null) {
                var gatherer = gathererFactory();
                settingsViewModel = new TraceSessionSettingsViewModel(gatherer);
            }

            var window = new TraceSessionSettingsWindow();
            try {
                window.DataContext = settingsViewModel;
                if (window.ShowModal() != true)
                    return;
            } finally {
                window.DataContext = null;
                settingsViewModel.DialogResult = null;
            }

            sessionDescriptor = settingsViewModel.GetDescriptor();
            eventSymbolSource.Update(settingsViewModel.GetEventSymbols());
        }

        private void UpdateStats()
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

        private async void OnOperationalModeChanged(object sender, VsOperationalMode newMode)
        {
            if (!AutoLog)
                return;

            switch (newMode) {
                case VsOperationalMode.Design:
                    StopCapture();
                    break;
                case VsOperationalMode.Debug:
                    await StartCapture();
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
            : base(null, null)
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
}
