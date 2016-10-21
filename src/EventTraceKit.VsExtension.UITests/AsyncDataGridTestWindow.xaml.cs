namespace EventTraceKit.VsExtension.UITests
{
    public partial class AsyncDataGridTestWindow
    {
        public AsyncDataGridTestWindow()
        {
            InitializeComponent();
            DataContext = new AsyncDataGridTestViewModel();
        }
    }
}
