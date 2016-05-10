namespace EventTraceKit.VsExtension.UITests
{
    /// <summary>
    /// Interaction logic for TraceLogTestWindow.xaml
    /// </summary>
    public partial class TraceLogTestWindow
    {
        public TraceLogTestWindow()
        {
            InitializeComponent();

            var vm = new TraceLogTestViewModel();
            //vm.StartCapture();
            DataContext = vm;
        }
    }
}
