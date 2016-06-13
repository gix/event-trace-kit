namespace EventTraceKit.VsExtension.UITests
{
    using System;
    using System.Collections.ObjectModel;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using EventTraceKit.VsExtension;

    public class TraceLogTestViewModel : TraceLogWindowViewModel
    {
        private string selectedTheme;

        private CancellationTokenSource cts;

        public TraceLogTestViewModel()
            : base(new StubOperationalModeProvider())
        {
            StartCommand = new DelegateCommand(Start);
            StopCommand = new DelegateCommand(Stop);
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

        private void Start()
        {
            StartCapture();
            return;

            if (cts != null)
                return;

            cts = new CancellationTokenSource();
            Task.Factory.StartNew(() => Gen(cts.Token), TaskCreationOptions.LongRunning);
        }

        private void Stop()
        {
            StopCapture();
            return;

            cts?.Cancel();
            cts = null;
        }

        private new void Clear()
        {
            base.Clear();
        }

        private int rows;

        private void Gen(CancellationToken token)
        {
            while (!token.IsCancellationRequested) {
                EventsDataView.UpdateRowCount(rows);
                Thread.Sleep(TimeSpan.FromMilliseconds(10));
                rows += 100;
            }
        }

        private class StubOperationalModeProvider : IOperationalModeProvider
        {
            public VsOperationalMode CurrentMode => VsOperationalMode.Design;
            public event EventHandler<VsOperationalMode> OperationalModeChanged;
        }

        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand ClearCommand { get; }
    }
}
