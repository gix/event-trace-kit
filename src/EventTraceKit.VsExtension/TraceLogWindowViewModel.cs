namespace EventTraceKit.VsExtension
{
    using System;
    using System.ComponentModel.Design;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Threading;
    using Collections;
    using Controls;
    using Formatting;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Task = System.Threading.Tasks.Task;

    public class TraceLogWindowViewModel : ViewModel, IEventInfoSource
    {
        private readonly IGlobalSettings settings;
        private readonly IViewPresetService viewPresetService;
        private readonly ITraceSettingsService traceSettingsService;
        private readonly IVsUIShell uiShell;
        private readonly DispatcherTimer updateStatisticsTimer;
        protected TraceSessionDescriptor sessionDescriptor = new TraceSessionDescriptor();

        private string status;
        private bool showStatistics;
        private string formattedEventStatistics;
        private string formattedBufferStatistics;
        private bool autoLog;
        private bool isCollecting;
        private TraceLog traceLog;
        private TraceSession session;

        private DateTime lastUpdateEvent = DateTime.MinValue;
        private readonly TimeSpan updateThreshold = TimeSpan.FromMilliseconds(50);
        private DispatcherSynchronizationContext syncCtx;
        private TaskScheduler scheduler;
        private readonly TaskFactory taskFactory;
        private readonly EventSymbolSource eventSymbolSource = new EventSymbolSource();

        public TraceLogWindowViewModel(
            IGlobalSettings settings,
            IOperationalModeProvider modeProvider,
            IViewPresetService viewPresetService,
            ITraceSettingsService traceSettingsService,
            IVsUIShell uiShell = null)
        {
            this.settings = settings;
            this.viewPresetService = viewPresetService;
            this.traceSettingsService = traceSettingsService;
            this.uiShell = uiShell;

            if (modeProvider != null)
                modeProvider.OperationalModeChanged += OnOperationalModeChanged;

            var tableTuple = new GenericEventsViewModelSource().CreateTable(this, eventSymbolSource);
            var dataTable = tableTuple.Item1;
            var templatePreset = tableTuple.Item2;

            var defaultPreset = GenericEventsViewModelSource.CreateDefaultPreset();
            var presetCollection = viewPresetService.Presets;
            presetCollection.BuiltInPresets.Add(defaultPreset);

            var preset = presetCollection.TryGetPersistedPresetByName(defaultPreset.Name);
            if (preset == null)
                preset = defaultPreset;

            EventsDataView = new TraceEventsView(dataTable);
            AdvModel = new AsyncDataViewModel(
                new WorkManager(Dispatcher.CurrentDispatcher),
                EventsDataView, templatePreset, preset, presetCollection);
            var activePreset = presetCollection.TryGetCurrentPresetByName(settings.ActiveViewPreset);
            if (activePreset != null)
                AdvModel.Preset = activePreset;

            GridModel = AdvModel.GridViewModel;

            AdvModel.PresetChanged += OnViewPresetChanged;

            syncCtx = new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher);
            SynchronizationContext.SetSynchronizationContext(syncCtx);
            scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            taskFactory = new TaskFactory(scheduler);

            Statistics = new TraceLogStatsModel();
            ShowStatistics = true;
            AutoLog = settings.AutoLog;

            updateStatisticsTimer = new DispatcherTimer(DispatcherPriority.Background);
            updateStatisticsTimer.Interval = TimeSpan.FromSeconds(1);
            updateStatisticsTimer.Tick += (s, a) => UpdateStats();
        }

        private void OnViewPresetChanged(
            object sender, ValueChangedEventArgs<AsyncDataViewModelPreset> args)
        {
            settings.ActiveViewPreset = args.NewValue?.Name;
            uiShell?.UpdateCommandUI(0);
        }

        public class TraceEventsView : DataView
        {
            public TraceEventsView(DataTable table)
                : base(table, new DefaultFormatProviderSource())
            {
            }

            public int EventCount => RowCount;
        }

        public TraceEventsView EventsDataView { get; }
        public AsyncDataViewModel AdvModel { get; }
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
            return sessionDescriptor != null && sessionDescriptor.Providers.Count > 0;
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
        }

        public void StopCapture()
        {
            if (session == null)
                return;

            try {
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

        private TraceSettingsViewModel settingsViewModel;

        private void Configure()
        {
            if (settingsViewModel == null) {
                settingsViewModel = new TraceSettingsViewModel();
                settingsViewModel.Sessions.AddRange(traceSettingsService.Sessions);
            }

            var window = new TraceSessionSettingsWindow();
            try {
                window.DataContext = settingsViewModel;
                if (window.ShowModal() != true)
                    return;
            } finally {
                var selectedPreset = settingsViewModel.ActiveSession;
                window.DataContext = null;
                settingsViewModel.ActiveSession = selectedPreset;
                settingsViewModel.DialogResult = null;
            }

            sessionDescriptor = settingsViewModel.GetDescriptor();
            eventSymbolSource.Update(settingsViewModel.GetEventSymbols());

            traceSettingsService.Save(settingsViewModel);
        }

        private void OpenViewEditor()
        {
            PresetManagerDialog.ShowPresetManagerDialog(AdvModel);
            viewPresetService.SaveToStorage();
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

        public void AddCommandHandler(IMenuCommandService commandService)
        {
            var id = new CommandID(PkgCmdId.TraceLogCmdSet, PkgCmdId.cmdidCaptureLog);
            commandService.AddCommand(new OleMenuCommand(
                (s, e) => ToggleCapture(), null, OnQueryToggleCaptureLog, id));

            id = new CommandID(PkgCmdId.TraceLogCmdSet, PkgCmdId.cmdidClearLog);
            commandService.AddCommand(new OleMenuCommand((s, e) => Clear(), id));

            id = new CommandID(PkgCmdId.TraceLogCmdSet, PkgCmdId.cmdidAutoLog);
            commandService.AddCommand(
                new OleMenuCommand(OnToggleAutoLog, null, OnQueryToggleAutoLog, id));

            id = new CommandID(PkgCmdId.TraceLogCmdSet, PkgCmdId.cmdidConfigureSession);
            commandService.AddCommand(new OleMenuCommand((s, e) => Configure(), id));

            id = new CommandID(PkgCmdId.TraceLogCmdSet, PkgCmdId.cmdidOpenViewEditor);
            commandService.AddCommand(new OleMenuCommand((s, e) => OpenViewEditor(), id));

            id = new CommandID(PkgCmdId.TraceLogCmdSet, PkgCmdId.cmdidViewPresetCombo);
            commandService.AddCommand(new OleMenuCommand(OnViewPresetCombo, id));

            id = new CommandID(PkgCmdId.TraceLogCmdSet, PkgCmdId.cmdidViewPresetComboGetList);
            commandService.AddCommand(new OleMenuCommand(OnViewPresetComboGetList, id));
        }

        private void OnQueryToggleCaptureLog(object sender, EventArgs e)
        {
            var command = (OleMenuCommand)sender;
            command.Enabled = IsCollecting || CanStartCapture();
            command.Checked = IsCollecting;
        }

        private void OnQueryToggleAutoLog(object sender, EventArgs e)
        {
            var command = (OleMenuCommand)sender;
            command.Checked = AutoLog;
        }

        private void OnToggleAutoLog(object sender, EventArgs e)
        {
            AutoLog = !AutoLog;
            settings.AutoLog = AutoLog;
        }

        private void OnViewPresetCombo(object sender, EventArgs args)
        {
            var cmdArgs = (OleMenuCmdEventArgs)args;

            string presetName = cmdArgs.InValue as string;
            IntPtr outValue = cmdArgs.OutValue;

            if (outValue != IntPtr.Zero) {
                string displayName = AdvModel.Preset.GetDisplayName();
                Marshal.GetNativeVariantForObject(displayName, outValue);
                return;
            }

            if (presetName == null)
                return;

            if (presetName.EndsWith("*"))
                presetName = presetName.Substring(0, presetName.Length - 1);

            var preset = AdvModel.PresetCollection.TryGetCurrentPresetByName(presetName);
            if (preset != null)
                AdvModel.Preset = preset;
        }

        private void OnViewPresetComboGetList(object sender, EventArgs args)
        {
            var cmdArgs = (OleMenuCmdEventArgs)args;
            if (cmdArgs.InValue != null)
                throw new ArgumentException("InValue must not be provided.");
            if (cmdArgs.OutValue == IntPtr.Zero)
                throw new ArgumentException("OutValue is required.");

            var items = AdvModel.PresetCollection.EnumerateAllPresetsByName()
                .Select(x => x.Name + (AdvModel.PresetCollection.HasPersistedPreset(x.Name) ? "*" : "")).ToArray();
            Marshal.GetNativeVariantForObject(items, cmdArgs.OutValue);
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
            : base(null, null, null, null)
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
