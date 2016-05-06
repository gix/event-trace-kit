namespace UIVirt
{
    using System;
    using EventTraceKit.VsExtension;

    /// <summary>
    /// Interaction logic for TableControlWindow.xaml
    /// </summary>
    public partial class TableControlWindow
    {
        public TableControlWindow()
        {
            InitializeComponent();
            var vm = new TraceLogWindowViewModel(new OperationalModeProviderStub());
            DataContext = vm;

            vm.StartCapture();
        }

        private class OperationalModeProviderStub : IOperationalModeProvider
        {
            public VsOperationalMode CurrentMode => VsOperationalMode.Design;
            public event EventHandler<VsOperationalMode> OperationalModeChanged;
        }
    }
}
