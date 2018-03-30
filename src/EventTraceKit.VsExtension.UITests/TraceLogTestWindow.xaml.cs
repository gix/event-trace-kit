namespace EventTraceKit.VsExtension.UITests
{
    using System.ComponentModel;

    public partial class TraceLogTestWindow
    {
        private readonly DefaultTraceController traceController = new DefaultTraceController();

        public TraceLogTestWindow()
        {
            InitializeComponent();
            DataContext = new TraceLogTestViewModel(traceController);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            traceController.StopSession();
        }
    }
}
