namespace EventTraceKit.VsExtension.UITests
{
    public partial class TraceLogTestWindow
    {
        public TraceLogTestWindow()
        {
            InitializeComponent();
            DataContext = new TraceLogTestViewModel();
        }
    }
}
