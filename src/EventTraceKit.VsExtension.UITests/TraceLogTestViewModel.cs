namespace EventTraceKit.VsExtension.UITests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using EventTraceKit.VsExtension;
    using Microsoft.VisualStudio.PlatformUI;

    public class TraceLogTestViewModel : TraceLogWindowViewModel
    {
        private string selectedTheme;
        private bool isRunning;

        public TraceLogTestViewModel()
            : base(new OperationalModeProviderStub(), () => new StubSolutionFileGatherer())
        {
            StartCommand = new AsyncDelegateCommand(Start, CanStart);
            StopCommand = new DelegateCommand(Stop, CanStop);
            ClearCommand = new DelegateCommand(Clear);
            ConfigureCommand = new DelegateCommand(Configure);

            foreach (var name in App.Current.AvailableThemes)
                Themes.Add(name);

            SelectedTheme = App.Current.ActiveTheme;
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

        public bool IsRunning
        {
            get { return isRunning; }
            set { SetProperty(ref isRunning, value); }
        }

        private bool CanStart(object obj)
        {
            return !IsRunning;
        }

        private async Task Start(object param)
        {
            await StartCapture();
            IsRunning = true;
            CommandManager.InvalidateRequerySuggested();
        }

        private bool CanStop(object obj)
        {
            return IsRunning;
        }

        private void Stop(object param)
        {
            StopCapture();
            IsRunning = false;
            CommandManager.InvalidateRequerySuggested();
        }

        private void Clear(object param)
        {
            base.Clear();
        }

        private void Configure(object obj)
        {
            var gatherer = new StubSolutionFileGatherer();
            var viewModel = new TraceSessionSettingsViewModel(gatherer);
            var dialog = new TraceSessionSettingsWindow();
            dialog.DataContext = viewModel;
            dialog.HasDialogFrame = false;
            if (dialog.ShowDialog() != true)
                return;
        }

        private class StubSolutionFileGatherer : ISolutionFileGatherer
        {
            public IEnumerable<string> GetFiles()
            {
                yield break;
            }
        }

        private sealed class OperationalModeProviderStub : IOperationalModeProvider
        {
            public VsOperationalMode CurrentMode => VsOperationalMode.Design;
            public event EventHandler<VsOperationalMode> OperationalModeChanged;
        }
    }
}
