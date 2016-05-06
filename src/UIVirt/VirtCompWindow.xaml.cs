namespace UIVirt
{
    using System;
    using EventTraceKit.VsExtension;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new VirtCompViewModel();
        }
    }
}
