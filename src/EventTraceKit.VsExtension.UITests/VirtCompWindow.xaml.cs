namespace EventTraceKit.VsExtension.UITests
{
    public partial class VirtCompWindow
    {
        public VirtCompWindow()
        {
            InitializeComponent();
            DataContext = new VirtCompViewModel();
        }
    }
}
