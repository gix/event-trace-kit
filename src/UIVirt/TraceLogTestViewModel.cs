namespace UIVirt
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using EventTraceKit.VsExtension;

    public class TraceLogTestViewModel : TraceLogWindowViewModel
    {
        private CancellationTokenSource cts;

        public TraceLogTestViewModel()
            : base(new StubOperationalModeProvider())
        {
            StartCommand = new DelegateCommand(Start);
            StopCommand = new DelegateCommand(Stop);
            ClearCommand = new DelegateCommand(Clear);
        }

        private void Start()
        {
            if (cts != null)
                return;

            cts = new CancellationTokenSource();
            Task.Factory.StartNew(() => Gen(cts.Token), TaskCreationOptions.LongRunning);
        }

        private void Stop()
        {
            cts?.Cancel();
            cts = null;
        }

        private new void Clear()
        {
        }

        private int rows = 0;

        private void Gen(CancellationToken token)
        {
            while (!token.IsCancellationRequested) {
                GridModel.UpdateRowCount(rows);
                Thread.Sleep(TimeSpan.FromMilliseconds(500));
                rows += 1;
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
