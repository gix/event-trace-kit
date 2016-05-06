namespace UIVirt
{
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for TraceLogTestWindow.xaml
    /// </summary>
    public partial class TraceLogTestWindow : Window
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
