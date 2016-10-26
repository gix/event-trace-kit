namespace EventTraceKit.VsExtension.UITests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Windows;
    using System.Windows.Input;
    using EventTraceKit.VsExtension;
    using Microsoft.VisualStudio.PlatformUI;
    using Task = System.Threading.Tasks.Task;

    public class TraceLogTestViewModel : TraceLogWindowViewModel
    {
        private string selectedTheme;
        private bool isRunning;

        public TraceLogTestViewModel()
            : base(new StubGlobalSettings(),
                   new OperationalModeProviderStub(),
                   new StubViewPresetService(),
                   new StubTraceSettingsService())
        {
            StartCommand = new AsyncDelegateCommand(Start, CanStart);
            StopCommand = new AsyncDelegateCommand(Stop, CanStop);
            ClearCommand = new AsyncDelegateCommand(Clear);
            ConfigureCommand = new DelegateCommand(Configure);
            OpenViewEditorCommand = new DelegateCommand(OpenViewEditor);

            foreach (var name in App.Current.AvailableThemes)
                Themes.Add(name);

            SelectedTheme = App.Current.ActiveTheme;

            viewModel = new TraceSettingsViewModel();
            var session = new TraceSessionSettingsViewModel();
            session.Providers.Add(new TraceProviderDescriptorViewModel {
                Id = new Guid("65CD4C8A-0848-4583-92A0-31C0FBAF00C0"),
                Name = "DX",
                IsEnabled = true
            });
            viewModel.Sessions.Add(session);
            viewModel.ActiveSession = session;
            sessionDescriptor = viewModel.GetDescriptor();
        }

        public ObservableCollection<string> Themes { get; } =
            new ObservableCollection<string>();

        public string SelectedTheme
        {
            get { return selectedTheme; }
            set
            {
                if (selectedTheme == value)
                    return;

                if (App.Current.TryLoadTheme(value))
                    selectedTheme = value;
            }
        }

        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand ConfigureCommand { get; }
        public ICommand OpenViewEditorCommand { get; }

        public bool IsRunning
        {
            get { return isRunning; }
            set { SetProperty(ref isRunning, value); }
        }

        private bool CanStart()
        {
            return !IsRunning && sessionDescriptor != null;
        }

        private async Task Start()
        {
            await StartCapture();
            IsRunning = true;
            CommandManager.InvalidateRequerySuggested();
        }

        private bool CanStop()
        {
            return IsRunning;
        }

        private Task Stop()
        {
            StopCapture();
            IsRunning = false;
            CommandManager.InvalidateRequerySuggested();
            return Task.CompletedTask;
        }

        private new Task Clear()
        {
            base.Clear();
            return Task.CompletedTask;
        }

        private TraceSettingsViewModel viewModel;

        private void Configure(object obj)
        {
            if (viewModel == null) {
                viewModel = new TraceSettingsViewModel();
            }

            var dialog = new TraceSessionSettingsWindow();
            dialog.DataContext = viewModel;
            try {
                if (dialog.ShowDialog() != true)
                    return;
            } finally {
                var selectedSession = viewModel.ActiveSession;
                dialog.DataContext = null;
                viewModel.ActiveSession = selectedSession;
                viewModel.DialogResult = null;
            }

            sessionDescriptor = viewModel.GetDescriptor();
        }

        private void OpenViewEditor(object obj)
        {
            var dialog = PresetManagerDialog.CreateDialog(AdvModel);
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();
        }

        private class StubTraceSettingsService : ITraceSettingsService
        {
            public IReadOnlyCollection<TraceSessionSettingsViewModel> Sessions { get; } =
                new List<TraceSessionSettingsViewModel>();

            public void Save(TraceSettingsViewModel sessions)
            {
            }
        }

        private class StubViewPresetService : IViewPresetService
        {
            public AdvmPresetCollection Presets { get; } =
                new AdvmPresetCollection();

            public event EventHandler<ExceptionFilterEventArgs> ExceptionFilter;

            public void SaveToStorage()
            {
            }
        }

        private class StubGlobalSettings : IGlobalSettings
        {
            public string ActiveViewPreset { get; set; }
            public bool AutoLog { get; set; }
        }

        private sealed class OperationalModeProviderStub : IOperationalModeProvider
        {
            public VsOperationalMode CurrentMode => VsOperationalMode.Design;
            public event EventHandler<VsOperationalMode> OperationalModeChanged;
        }
    }
}
