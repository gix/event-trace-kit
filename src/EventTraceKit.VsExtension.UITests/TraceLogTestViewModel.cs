namespace EventTraceKit.VsExtension.UITests
{
    using System;
    using System.Collections.ObjectModel;
    using System.Windows.Input;
    using EventTraceKit.VsExtension;
    using Microsoft.VisualStudio.PlatformUI;

    public class TraceLogTestViewModel : TraceLogWindowViewModel
    {
        private string selectedTheme;
        private bool running;

        public TraceLogTestViewModel()
            : base(new StubOperationalModeProvider())
        {
            StartCommand = new DelegateCommand(Start, CanStart);
            StopCommand = new DelegateCommand(Stop, CanStop);
            ClearCommand = new DelegateCommand(Clear);

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

        private bool CanStart(object obj)
        {
            return !running;
        }

        private void Start(object param)
        {
            StartCapture();
            running = true;
        }

        private bool CanStop(object obj)
        {
            return running;
        }

        private void Stop(object param)
        {
            StopCapture();
            running = false;
        }

        private void Clear(object param)
        {
            base.Clear();
        }

        private class StubOperationalModeProvider : IOperationalModeProvider
        {
            public VsOperationalMode CurrentMode => VsOperationalMode.Design;
            public event EventHandler<VsOperationalMode> OperationalModeChanged;
        }
    }
}
