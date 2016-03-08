namespace EventTraceKit.Dev14
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Threading;
    using Microsoft.VisualStudio.PlatformUI;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.Win32;

    public class TraceLogWindowViewModel : ViewModel
    {
        private readonly object eventsMutex = new object();
        private readonly DispatcherTimer updateStatisticsTimer;
        private readonly List<TraceProviderSpec> providers = new List<TraceProviderSpec>();

        private string status;
        private bool showStatistics;
        private bool isCollecting;
        private TraceSession session;

        public TraceLogWindowViewModel()
        {
            Events = new ObservableCollection<TraceEvent>();
            BindingOperations.EnableCollectionSynchronization(Events, eventsMutex);
            Statistics = new TraceLogStatsModel();
            ShowStatistics = true;

            ToggleCaptureCommand = new DelegateCommand(ToggleCapture, CanToggleCapture);
            ClearCommand = new DelegateCommand(Clear);
            ConfigureCommand = new DelegateCommand(Configure);

            updateStatisticsTimer = new DispatcherTimer(DispatcherPriority.Background);
            updateStatisticsTimer.Interval = TimeSpan.FromSeconds(1);
            updateStatisticsTimer.Tick += (s, a) => UpdateStats();

            var spec = new TraceProviderSpec(new Guid("716EFEF7-5AC2-4EE0-8277-D9226411A155"));
            spec.SetManifest(@"C:\Users\nrieck\dev\ffmf\src\Sculptor\Sculptor.man");
            providers.Add(spec);
        }

        public ObservableCollection<TraceEvent> Events { get; }

        public TraceLogStatsModel Statistics { get; }

        public ICommand ToggleCaptureCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand ConfigureCommand { get; }

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

        public bool ShowStatistics
        {
            get { return showStatistics; }
            set { SetProperty(ref showStatistics, value); }
        }

        private bool CanCapture()
        {
            return providers.Count > 0;
        }

        private bool CanToggleCapture(object obj)
        {
            return IsCollecting || CanCapture();
        }

        private void ToggleCapture(object obj)
        {
            if (IsCollecting)
                StopCapture();
            else if (CanCapture())
                StartCapture();
        }

        private void Clear(object obj)
        {
            try {
                session?.Clear();
                Events.Clear();
                Statistics.Reset();
            } catch (Exception ex) {
                Status = ex.ToString();
            }
        }

        private void StartCapture()
        {
            if (!CanCapture())
                return;

            Status = null;
            try {
                session = new TraceSession(Events, providers);
                session.Start();
                IsCollecting = true;
                updateStatisticsTimer.Start();
            } catch (Exception ex) {
                Status = ex.ToString();
            }
        }

        private void StopCapture()
        {
            try {
                session.Stop();
                session = null;
                Status = null;
                IsCollecting = false;
                updateStatisticsTimer.Stop();
            } catch (Exception ex) {
                Status = ex.ToString();
            }
        }

        private void Configure(object obj)
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
            if (true) {
                Statistics.TotalEvents = (uint)Events.Count;
                Statistics.EventsLost = stats.EventsLost;
                Statistics.NumberOfBuffers = stats.NumberOfBuffers;
                Statistics.BuffersWritten = stats.BuffersWritten;
                Statistics.FreeBuffers = stats.FreeBuffers;
                Statistics.LogBuffersLost = stats.LogBuffersLost;
                Statistics.RealTimeBuffersLost = stats.RealTimeBuffersLost;
            }
        }
    }

    //public struct TraceEvent
    //{
    //    public ushort Id { get; set; }
    //    public byte Version { get; set; }
    //    public byte Channel { get; set; }
    //    public byte Level { get; set; }
    //    public byte Opcode { get; set; }
    //    public ushort Task { get; set; }
    //    public ulong Keyword { get; set; }

    //    public DateTime Time { get; set; }
    //    public string Message { get; set; }
    //    public bool Formatted { get; set; }
    //}

    public class TraceLogWindowDesignTimeModel : TraceLogWindowViewModel
    {
        public TraceLogWindowDesignTimeModel()
        {
            Events.Add(new TraceEvent {
                Id = 4452,
                Version = 1,
                ChannelId = 3,
                LevelId = 2,
                OpcodeId = 10,
                TaskId = 1000,
                KeywordMask = 0x8000000,
                Time = new DateTime(2000, 10, 11, 12, 13, 14),
                Message = "First event",
                Formatted = true
            });

            Events.Add(new TraceEvent {
                Id = 4453,
                Version = 1,
                ChannelId = 3,
                LevelId = 2,
                OpcodeId = 11,
                TaskId = 2000,
                KeywordMask = 0x8000000,
                Time = new DateTime(2000, 10, 11, 12, 13, 15),
                Message = "Second event",
                Formatted = true
            });

            Events.Add(new TraceEvent {
                Id = 4454,
                Version = 1,
                ChannelId = 3,
                LevelId = 3,
                OpcodeId = 12,
                TaskId = 3000,
                KeywordMask = 0x8000000,
                Time = new DateTime(2000, 10, 11, 12, 14, 14),
                Message = "Another event",
                Formatted = true
            });

            Statistics.TotalEvents = 1429;
            Statistics.EventsLost = 30;

            Statistics.NumberOfBuffers = 10;
            Statistics.FreeBuffers = 20;
            Statistics.BuffersWritten = 40;
            Statistics.LogBuffersLost = 50;
            Statistics.RealTimeBuffersLost = 60;
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

    internal static class EtkColors
    {
        public static object TraceLogBackgroundKey => "EtkColors.TraceLogBackgroundKey";
        public static object TraceLogSelectedBackgroundKey => "EtkColors.TraceLogSelectedBackgroundKey";
        public static object TraceLogInactiveSelectedBackgroundKey => "EtkColors.TraceLogInactiveSelectedBackgroundKey";
        public static object TraceLogForegroundKey => "EtkColors.TraceLogForegroundKey";
    }

    internal static class EtkFonts
    {
        public static object TraceLogEntryFontFamilyKey => "EtkFont.TraceLogEntryFontFamily";
        public static object TraceLogEntryFontSizeKey => "EtkFont.TraceLogEntryFontSize";
    }

    public class TraceSessionSettingsWindowViewModel : ViewModel
    {
        public TraceSessionSettingsWindowViewModel()
        {
            AddProviderCommand = new DelegateCommand(AddProvider);
            AddManifestCommand = new DelegateCommand(AddManifest);
            AddBinaryCommand = new DelegateCommand(AddBinary);
            Providers = new ObservableCollection<TraceProviderSpecViewModel>();
        }

        public ICommand AddProviderCommand { get; }
        public ICommand AddManifestCommand { get; }
        public ICommand AddBinaryCommand { get; }
        public ObservableCollection<TraceProviderSpecViewModel> Providers { get; }

        private void AddProvider(object obj)
        {
        }

        private void AddManifest(object obj)
        {
            var dialog = new OpenFileDialog();
            dialog.ShowDialog();
        }

        private void AddBinary(object obj)
        {
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
                if (ManifestOrProvider.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                    ManifestOrProvider.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
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
