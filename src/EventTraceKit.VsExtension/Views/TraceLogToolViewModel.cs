namespace EventTraceKit.VsExtension.Views
{
    using System;
    using System.ComponentModel.Design;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Threading;
    using Controls;
    using EventTraceKit.Tracing;
    using EventTraceKit.VsExtension.Filtering;
    using EventTraceKit.VsExtension.Serialization;
    using EventTraceKit.VsExtension.Settings;
    using EventTraceKit.VsExtension.Views.PresetManager;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Task = System.Threading.Tasks.Task;
    using TraceLogFilter = EventTraceKit.VsExtension.Filtering.TraceLogFilter;

    public interface IFilterable
    {
        TraceLogFilter Filter { get; set; }
    }

    public class TraceLogToolViewModel
        : ObservableModel, IEventInfoSource, IFilterable
    {
        private readonly ISettingsService settings;
        private readonly ITraceController traceController;
        private readonly ISolutionBrowser solutionBrowser;
        private readonly IVsUIShell uiShell;

        private SettingsStoreWrapper globalStore;
        private SettingsStoreWrapper ambientStore;

        private readonly TaskFactory taskFactory;
        private readonly EventSymbolSource eventSymbolSource = new EventSymbolSource();

        private readonly DispatcherTimer updateStatsTimer;
        protected TraceProfileDescriptor traceProfile = new TraceProfileDescriptor();

        private enum LoggerState
        {
            Stopped,
            Starting,
            Started,
            Stopping
        }

        // Property backing fields
        private string status;
        private LoggerState state;
        private string formattedEventStatistics;
        private string formattedBufferStatistics;

        private TraceLog traceLog;
        private EventSession session;

        private TraceSettingsViewModel settingsViewModel;
        private bool autoLog;
        private bool autoScroll;
        private bool showStatusBar;
        private bool showColumnHeaders = true;
        private bool isFilterEnabled;
        private TraceLogFilter currentFilter;

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
            ISettingsService settings, ITraceController traceController,
            ISolutionBrowser solutionBrowser = null, IVsUIShell uiShell = null)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.traceController = traceController ?? throw new ArgumentNullException(nameof(traceController));
            this.solutionBrowser = solutionBrowser;
            this.uiShell = uiShell;

            settings.SettingsLayerChanged += OnSettingsLayerChanged;

            traceController.SessionStarting += OnSessionStarting;
            traceController.SessionStarted += OnSessionStarted;
            traceController.SessionStopped += OnSessionStopped;

            var tableTuple = new GenericEventsViewModelSource().CreateTable(this, eventSymbolSource);
            var dataTable = tableTuple.Item1;
            var templatePreset = tableTuple.Item2;

            var defaultPreset = GenericEventsViewModelSource.CreateDefaultPreset();
            var viewPresets = settings.GetGlobalStore().GetViewPresets(SettingsSerializer.Mapper);
            viewPresets.BuiltInPresets.Add(defaultPreset);

            var preset = viewPresets.TryGetPersistedPresetByName(defaultPreset.Name);
            if (preset == null)
                preset = defaultPreset;

            EventsDataView = new TraceEventsDataView(dataTable, this);
            AdvModel = new AsyncDataViewModel(
                new WorkManager(Dispatcher.CurrentDispatcher),
                EventsDataView, templatePreset, preset, viewPresets);

            GridModel = AdvModel.GridViewModel;
            AdvModel.PresetChanged += OnViewPresetChanged;
            BindingOperations.SetBinding(GridModel, AsyncDataGridViewModel.AutoScrollProperty, new Binding(nameof(AutoScroll)) {
                Source = this
            });

            if (SynchronizationContext.Current == null) {
                SynchronizationContext.SetSynchronizationContext(
                    new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
            }

            var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            taskFactory = new TaskFactory(scheduler);

            updateStatsTimer = new DispatcherTimer(DispatcherPriority.Background);
            updateStatsTimer.Interval = TimeSpan.FromSeconds(1);
            updateStatsTimer.Tick += (s, e) => UpdateStats();

            LoadGlobalSettings();
            LoadAmbientSettings();
        }

        public void OnClose()
        {
            if (AdvModel != null) {
                globalStore.SetViewPresets(AdvModel.PresetCollection, SettingsSerializer.Mapper);
                globalStore.Save();
            }

            settings.SaveAmbient();
        }

        private void LoadGlobalSettings()
        {
            globalStore = settings.GetGlobalStore().AsWrapper();
            AutoLog = globalStore.AutoLog;
            AutoScroll = globalStore.AutoScroll;
            ShowColumnHeaders = globalStore.ShowColumnHeaders;
            ShowStatusBar = globalStore.ShowStatusBar;
        }

        private void LoadAmbientSettings()
        {
            ambientStore = settings.GetAmbientStore().AsWrapper();
            IsFilterEnabled = ambientStore.IsFilterEnabled;

            traceProfile = GetActiveProfile(ambientStore);
            PropagateAutoLog();

            string activeViewPreset = ambientStore.ActiveViewPreset;
            var activePreset = AdvModel.PresetCollection.TryGetCurrentPresetByName(activeViewPreset);
            if (activePreset != null)
                AdvModel.Preset = activePreset;
        }

        private static TraceProfileDescriptor GetActiveProfile(ISettingsStore store)
        {
            var tracing = store.GetValue(SettingsKeys.Tracing);
            var profile = tracing?.Profiles.FirstOrDefault(x => x.Id == tracing.ActiveProfile);
            return profile?.GetDescriptor();
        }

        private void OnSettingsLayerChanged(object sender, EventArgs eventArgs)
        {
            settingsViewModel = null;
            LoadAmbientSettings();
        }

        private void OnViewPresetChanged(
            object sender, ValueChangedEventArgs<AsyncDataViewModelPreset> args)
        {
            string name = args.NewValue?.Name;
            if (!string.IsNullOrEmpty(name))
                ambientStore.SetValue(SettingsKeys.ActiveViewPreset, args.NewValue?.Name);
            else
                ambientStore.ClearValue(SettingsKeys.ActiveViewPreset);

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

        public bool IsCollecting => state != LoggerState.Stopped;

        public bool AutoLog
        {
            get => autoLog;
            set
            {
                if (SetProperty(ref autoLog, value)) {
                    globalStore.AutoLog = value;
                    PropagateAutoLog();
                }
            }
        }

        public bool AutoScroll
        {
            get => autoScroll;
            set
            {
                if (SetProperty(ref autoScroll, value))
                    globalStore.AutoScroll = value;
            }
        }

        public bool ShowStatusBar
        {
            get => showStatusBar;
            set
            {
                if (SetProperty(ref showStatusBar, value))
                    globalStore.ShowStatusBar = value;
            }
        }

        public bool ShowColumnHeaders
        {
            get => showColumnHeaders;
            set
            {
                if (SetProperty(ref showColumnHeaders, value)) {
                    globalStore.ShowColumnHeaders = value;
                    GridModel.ColumnsModel.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                }
            }
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
            return !AutoLog && traceProfile.IsUsable();
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
            if (settingsViewModel == null)
                settingsViewModel = new TraceSettingsViewModel(
                    ambientStore, solutionBrowser);

            var window = new TraceSettingsWindow();
            window.DataContext = settingsViewModel;
            try {
                if (window.ShowModal() != true)
                    return;
            } finally {
                var selectedPreset = settingsViewModel.ActiveProfile;
                window.DataContext = null;
                settingsViewModel.ActiveProfile = selectedPreset;
                settingsViewModel.DialogResult = null;
            }

            traceProfile = GetActiveProfile(ambientStore);
            PropagateAutoLog();
        }

        private void PropagateAutoLog()
        {
            if (traceProfile != null && AutoLog)
                traceController.EnableAutoLog(traceProfile);
            else
                traceController.DisableAutoLog();
        }

        private void OpenViewEditor()
        {
            PresetManagerDialog.ShowModalDialog(AdvModel);
            globalStore.SetViewPresets(AdvModel.PresetCollection, SettingsSerializer.Mapper);
            globalStore.Save();
        }

        protected void OpenFilterEditor()
        {
            var viewModel = new FilterDialogViewModel();
            viewModel.SetFilter(currentFilter);

            var dialog = new FilterDialog { DataContext = viewModel };
            if (dialog.ShowModal() != true)
                return;

            currentFilter = viewModel.GetFilter();
            IsFilterEnabled = true;
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

            id = new CommandID(PkgCmdId.TraceLogCmdSet, PkgCmdId.cmdidAutoScroll);
            commandService.AddCommand(
                new OleMenuCommand(OnToggleAutoScroll, null, OnQueryToggleAutoScroll, id));

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

        private void OnQueryToggleCaptureLog(object sender, EventArgs args)
        {
            var command = (MenuCommand)sender;
            command.Enabled = !AutoLog && (state == LoggerState.Started || (state == LoggerState.Stopped && CanStartCapture()));
            command.Checked = IsCollecting;
        }

        private void OnQueryToggleAutoLog(object sender, EventArgs args)
        {
            var command = (MenuCommand)sender;
            command.Checked = AutoLog;
        }

        private void OnToggleAutoLog(object sender, EventArgs args)
        {
            AutoLog = !AutoLog;
        }

        private void OnQueryToggleAutoScroll(object sender, EventArgs args)
        {
            var command = (MenuCommand)sender;
            command.Checked = AutoScroll;
        }

        private void OnToggleAutoScroll(object sender, EventArgs args)
        {
            AutoScroll = !AutoScroll;
        }

        public bool IsFilterEnabled
        {
            get => isFilterEnabled;
            set
            {
                if (isFilterEnabled != value) {
                    ambientStore.IsFilterEnabled = value;
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

        private void OnQueryToggleEnableFilter(object sender, EventArgs args)
        {
            var command = (MenuCommand)sender;
            command.Checked = IsFilterEnabled;
        }

        private void OnToggleEnableFilter(object sender, EventArgs args)
        {
            IsFilterEnabled = !IsFilterEnabled;
        }

        private void OnQueryToggleColumnHeaders(object sender, EventArgs args)
        {
            var command = (MenuCommand)sender;
            command.Checked = ShowColumnHeaders;
        }

        private void OnToggleColumnHeaders(object sender, EventArgs args)
        {
            ShowColumnHeaders = !ShowColumnHeaders;
        }

        private void OnQueryToggleStatusBar(object sender, EventArgs args)
        {
            var command = (MenuCommand)sender;
            command.Checked = ShowStatusBar;
        }

        private void OnToggleStatusBar(object sender, EventArgs args)
        {
            ShowStatusBar = !ShowStatusBar;
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

        EventSessionInfo IEventInfoSource.GetInfo()
        {
            return traceLog?.GetInfo() ?? default;
        }

        EventInfo IEventInfoSource.GetEvent(int index)
        {
            return traceLog?.GetEvent(index) ?? default;
        }
    }

    public class TraceLogPaneDesignTimeModel : TraceLogToolViewModel
    {
        public TraceLogPaneDesignTimeModel()
            : base(null, null, null)
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
