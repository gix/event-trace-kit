namespace EventTraceKit.VsExtension.Views
{
    using System;
    using System.ComponentModel.Design;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Threading;
    using Controls;
    using EventTraceKit.Tracing;
    using EventTraceKit.VsExtension.Extensions;
    using EventTraceKit.VsExtension.Filtering;
    using EventTraceKit.VsExtension.Views.PresetManager;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Task = System.Threading.Tasks.Task;

    public interface IFilterable
    {
        TraceLogFilter Filter { get; set; }
    }

    public class TraceLogToolViewModel : ObservableModel, IEventInfoSource, IFilterable
    {
        private readonly IGlobalSettings settings;
        private readonly IViewPresetService viewPresetService;
        private readonly ITraceController traceController;
        private readonly ITraceSettingsService traceSettingsService;
        private readonly IVsUIShell uiShell;

        private readonly TaskFactory taskFactory;
        private readonly EventSymbolSource eventSymbolSource = new EventSymbolSource();

        private readonly DispatcherTimer updateStatsTimer;
        protected TraceProfileDescriptor traceProfile = new TraceProfileDescriptor();

        public enum LoggerState
        {
            Stopped,
            Starting,
            Started,
            Stopping
        }

        // Property backing fields
        private string status;
        private LoggerState state;
        private bool autoLog;
        private bool showStatusBar;
        private string formattedEventStatistics;
        private string formattedBufferStatistics;

        private TraceLog traceLog;
        private EventSession session;

        private TraceSettingsViewModel settingsViewModel;
        private TraceLogFilter currentFilter;
        private bool isFilterEnabled;

        public TraceLogFilter Filter
        {
            get => currentFilter;
            set
            {
                currentFilter = value;
                RefreshFilter();
            }
        }

        public TraceLogToolViewModel(
            IGlobalSettings settings,
            ITraceController traceController,
            IViewPresetService viewPresetService,
            ITraceSettingsService traceSettingsService,
            IVsUIShell uiShell = null)
        {
            this.traceController = traceController ??
                throw new ArgumentNullException(nameof(traceController));

            this.settings = settings;
            this.viewPresetService = viewPresetService;
            this.traceSettingsService = traceSettingsService;
            this.uiShell = uiShell;

            traceController.SessionStarting += OnSessionStarting;
            traceController.SessionStarted += OnSessionStarted;
            traceController.SessionStopped += OnSessionStopped;

            AutoLog = settings.AutoLog;
            ShowStatusBar = settings.ShowStatusBar;
            IsFilterEnabled = true;

            var tableTuple = new GenericEventsViewModelSource().CreateTable(this, eventSymbolSource);
            var dataTable = tableTuple.Item1;
            var templatePreset = tableTuple.Item2;

            var defaultPreset = GenericEventsViewModelSource.CreateDefaultPreset();
            var presetCollection = viewPresetService.Presets;
            presetCollection.BuiltInPresets.Add(defaultPreset);

            var preset = presetCollection.TryGetPersistedPresetByName(defaultPreset.Name);
            if (preset == null)
                preset = defaultPreset;

            EventsDataView = new TraceEventsDataView(dataTable, this);
            AdvModel = new AsyncDataViewModel(
                new WorkManager(Dispatcher.CurrentDispatcher),
                EventsDataView, templatePreset, preset, presetCollection);
            var activePreset = presetCollection.TryGetCurrentPresetByName(settings.ActiveViewPreset);
            if (activePreset != null)
                AdvModel.Preset = activePreset;

            GridModel = AdvModel.GridViewModel;
            GridModel.ColumnsModel.Visibility = settings.ShowColumnHeaders ? Visibility.Visible : Visibility.Collapsed;

            AdvModel.PresetChanged += OnViewPresetChanged;

            if (SynchronizationContext.Current == null) {
                SynchronizationContext.SetSynchronizationContext(
                    new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
            }

            var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            taskFactory = new TaskFactory(scheduler);

            updateStatsTimer = new DispatcherTimer(DispatcherPriority.Background);
            updateStatsTimer.Interval = TimeSpan.FromSeconds(1);
            updateStatsTimer.Tick += (s, e) => UpdateStats();
        }

        private void OnViewPresetChanged(
            object sender, ValueChangedEventArgs<AsyncDataViewModelPreset> args)
        {
            settings.ActiveViewPreset = args.NewValue?.Name;
            uiShell?.UpdateCommandUI(0);
        }

        public TraceEventsDataView EventsDataView { get; }
        public AsyncDataViewModel AdvModel { get; }
        public AsyncDataGridViewModel GridModel { get; }

        public TraceLogStatsModel Statistics { get; } = new TraceLogStatsModel();

        public string Status
        {
            get => status;
            set => SetProperty(ref status, value);
        }

        public bool IsCollecting => state == LoggerState.Started || state == LoggerState.Stopping;

        public bool AutoLog
        {
            get => autoLog;
            set
            {
                if (!SetProperty(ref autoLog, value))
                    return;

                if (AutoLog)
                    traceController.EnableAutoLog(
                        new TraceProfileDescriptor(traceProfile));
                else
                    traceController.DisableAutoLog();
            }
        }

        public bool ShowStatusBar
        {
            get => showStatusBar;
            set => SetProperty(ref showStatusBar, value);
        }

        public string FormattedEventStatistics
        {
            get => formattedEventStatistics;
            set => SetProperty(ref formattedEventStatistics, value);
        }

        public string FormattedBufferStatistics
        {
            get => formattedBufferStatistics;
            set => SetProperty(ref formattedBufferStatistics, value);
        }

        private bool CanStartCapture()
        {
            return
                !AutoLog &&
                traceProfile != null &&
                traceProfile.Collectors.Count > 0 &&
                traceProfile.Collectors.OfType<EventCollectorDescriptor>().All(x => x.Providers.Count > 0);
        }

        private async void ToggleCapture()
        {
            switch (state) {
                case LoggerState.Stopped:
                    if (CanStartCapture())
                        await StartCapture();
                    break;
                case LoggerState.Started:
                    await StopCapture();
                    break;
                case LoggerState.Starting:
                case LoggerState.Stopping:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Clear()
        {
            try {
                session?.Flush();
                traceLog?.Clear();
                EventsDataView.Clear();
                Statistics.Reset();
            } catch (Exception ex) {
                Status = ex.ToString();
            }
        }

        private void ChangeState(LoggerState newState)
        {
            if (state == newState)
                return;

            state = newState;
            RaisePropertyChanged(nameof(IsCollecting));
            uiShell?.UpdateCommandUI(0);
        }

        private void OnSessionStarting(TraceLog newLog)
        {
            traceLog = newLog;
            traceLog.EventsChanged += OnEventsChanged;
            RefreshFilter();
        }

        private void OnSessionStarted(EventSession newSession)
        {
            session = newSession;
            Status = null;
            ChangeState(LoggerState.Started);
            updateStatsTimer.Start();
        }

        private void OnSessionStopped(EventSession _)
        {
            updateStatsTimer.Stop();
            UpdateStats();
            ChangeState(LoggerState.Stopped);
            Status = null;
            session = null;
        }

        public async Task StartCapture()
        {
            if (!CanStartCapture() || session != null)
                return;

            Status = null;
            EventsDataView.Clear();
            UpdateStats();

            try {
                ChangeState(LoggerState.Starting);
                await traceController.StartSessionAsync(traceProfile);
            } catch (Exception ex) {
                await traceController.StopSessionAsync();
                updateStatsTimer.Stop();
                UpdateStats();
                ChangeState(LoggerState.Stopped);
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

        public async Task StopCapture()
        {
            if (session == null)
                return;

            try {
                ChangeState(LoggerState.Stopping);
                await traceController.StopSessionAsync();
            } catch (Exception ex) {
                Status = ex.ToString();
            }
        }

        private void Configure()
        {
            if (settingsViewModel == null) {
                settingsViewModel = new TraceSettingsViewModel();
                settingsViewModel.Profiles.AddRange(traceSettingsService.Profiles);
            }

            var window = new TraceSettingsWindow();
            try {
                window.DataContext = settingsViewModel;
                settingsViewModel.Attach(window);
                if (window.ShowModal() != true)
                    return;
            } finally {
                var selectedPreset = settingsViewModel.ActiveProfile;
                settingsViewModel.Detach();
                window.DataContext = null;
                settingsViewModel.ActiveProfile = selectedPreset;
                settingsViewModel.DialogResult = null;
            }

            traceProfile = settingsViewModel.GetDescriptor();
            eventSymbolSource.Update(settingsViewModel.GetEventSymbols());

            traceSettingsService.Save(settingsViewModel);
            if (AutoLog)
                traceController.EnableAutoLog(traceProfile);
        }

        private void OpenViewEditor()
        {
            PresetManagerDialog.ShowModalDialog(AdvModel);
            viewPresetService.SaveToStorage();
        }

        protected void OpenFilterEditor()
        {
            var viewModel = new FilterDialogViewModel();
            viewModel.SetFilter(currentFilter);

            var dialog = new FilterDialog { DataContext = viewModel };
            if (dialog.ShowModal() != true)
                return;

            currentFilter = viewModel.GetFilter();
            RefreshFilter();
        }

        private void UpdateStats()
        {
            var stats = session?.Query() ?? new TraceStatistics();

            Statistics.ShownEvents = traceLog?.EventCount ?? 0;
            Statistics.TotalEvents = traceLog?.TotalEventCount ?? 0;
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
                $"Showing {Statistics.ShownEvents} of " +
                $"{Statistics.TotalEvents} Events " +
                $"({Statistics.EventsLost} lost)";
            FormattedBufferStatistics =
                $"{Statistics.NumberOfBuffers} Buffers (" +
                $"{Statistics.BuffersWritten} written, " +
                $"{Statistics.LogBuffersLost} lost, " +
                $"{Statistics.RealTimeBuffersLost} real-time buffers lost)";
        }

        public void InitializeMenuCommands(IMenuCommandService commandService)
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

            id = new CommandID(PkgCmdId.TraceLogCmdSet, PkgCmdId.cmdidEnableFilter);
            commandService.AddCommand(
                new OleMenuCommand(OnToggleEnableFilter, null, OnQueryToggleEnableFilter, id));

            id = new CommandID(PkgCmdId.TraceLogCmdSet, PkgCmdId.cmdidOpenFilterEditor);
            commandService.AddCommand(new OleMenuCommand((s, e) => OpenFilterEditor(), id));

            id = new CommandID(PkgCmdId.TraceLogCmdSet, PkgCmdId.cmdidViewPresetCombo);
            commandService.AddCommand(new OleMenuCommand(OnViewPresetCombo, id));

            id = new CommandID(PkgCmdId.TraceLogCmdSet, PkgCmdId.cmdidViewPresetComboGetList);
            commandService.AddCommand(new OleMenuCommand(OnViewPresetComboGetList, id));

            id = new CommandID(PkgCmdId.TraceLogCmdSet, PkgCmdId.cmdidToggleColumnHeaders);
            commandService.AddCommand(
                new OleMenuCommand(OnToggleColumnHeaders, null, OnQueryToggleColumnHeaders, id));

            id = new CommandID(PkgCmdId.TraceLogCmdSet, PkgCmdId.cmdidToggleStatusBar);
            commandService.AddCommand(
                new OleMenuCommand(OnToggleStatusBar, null, OnQueryToggleStatusBar, id));
        }

        private void OnQueryToggleCaptureLog(object sender, EventArgs e)
        {
            var command = (MenuCommand)sender;
            command.Enabled = !AutoLog && (state == LoggerState.Started || (state == LoggerState.Stopped && CanStartCapture()));
            command.Checked = IsCollecting;
        }

        private void OnQueryToggleAutoLog(object sender, EventArgs e)
        {
            var command = (MenuCommand)sender;
            command.Checked = AutoLog;
        }

        private void OnToggleAutoLog(object sender, EventArgs e)
        {
            AutoLog = !AutoLog;
            settings.AutoLog = AutoLog;
        }

        public bool IsFilterEnabled
        {
            get => isFilterEnabled;
            set
            {
                if (isFilterEnabled != value) {
                    isFilterEnabled = value;
                    RefreshFilter();
                }
            }
        }

        private void RefreshFilter()
        {
            if (traceLog == null)
                return;

            if (!isFilterEnabled)
                traceLog.SetFilter(null);
            else if (currentFilter != null)
                traceLog.SetFilter(currentFilter.CreatePredicate());
        }

        private void OnQueryToggleEnableFilter(object sender, EventArgs e)
        {
            var command = (OleMenuCommand)sender;
            command.Checked = IsFilterEnabled;
        }

        private void OnToggleEnableFilter(object sender, EventArgs e)
        {
            IsFilterEnabled = !IsFilterEnabled;
        }

        private void OnQueryToggleColumnHeaders(object sender, EventArgs e)
        {
            var command = (OleMenuCommand)sender;
            command.Checked = settings.ShowColumnHeaders;
        }

        private void OnToggleColumnHeaders(object sender, EventArgs e)
        {
            bool value = !settings.ShowColumnHeaders;
            settings.ShowColumnHeaders = value;
            GridModel.ColumnsModel.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnQueryToggleStatusBar(object sender, EventArgs e)
        {
            var command = (OleMenuCommand)sender;
            command.Checked = ShowStatusBar;
        }

        private void OnToggleStatusBar(object sender, EventArgs e)
        {
            ShowStatusBar = !ShowStatusBar;
            settings.ShowStatusBar = ShowStatusBar;
        }

        private void OnViewPresetCombo(object sender, EventArgs args)
        {
            var cmdArgs = (OleMenuCmdEventArgs)args;
            if (cmdArgs.TrySetOutValue(AdvModel.Preset.GetDisplayName()))
                return;

            if (!(cmdArgs.InValue is string presetName))
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

            var presets = AdvModel.PresetCollection;
            var items = presets.EnumerateAllPresetsByName()
                .Select(x => x.Name + (presets.HasPersistedPreset(x.Name) ? "*" : "")).ToArray();
            cmdArgs.SetOutValue(items);
        }

        public EventSessionInfo GetInfo()
        {
            if (session != null)
                return session.GetInfo();
            return new EventSessionInfo();
        }

        public EventInfo GetEvent(int index)
        {
            if (traceLog != null)
                return traceLog.GetEvent(index);
            return new EventInfo();
        }
    }

    public class TraceLogPaneDesignTimeModel : TraceLogToolViewModel
    {
        public TraceLogPaneDesignTimeModel()
            : base(null, null, null, null)
        {
            Statistics.TotalEvents = 1429;
            Statistics.EventsLost = 30;

            Statistics.NumberOfBuffers = 10;
            Statistics.FreeBuffers = 20;
            Statistics.BuffersWritten = 40;
            Statistics.LogBuffersLost = 50;
            Statistics.RealTimeBuffersLost = 60;

            ShowStatusBar = true;
            FormatStatistics();
        }
    }

    public static class OleMenuCmdEventArgsExtensions
    {
        public static bool TrySetOutValue(this OleMenuCmdEventArgs args, object value)
        {
            if (args.OutValue == IntPtr.Zero)
                return false;
            Marshal.GetNativeVariantForObject(value, args.OutValue);
            return true;
        }

        public static void SetOutValue(this OleMenuCmdEventArgs args, object value)
        {
            if (args.OutValue == IntPtr.Zero)
                throw new InvalidOperationException();
            Marshal.GetNativeVariantForObject(value, args.OutValue);
        }
    }
}
