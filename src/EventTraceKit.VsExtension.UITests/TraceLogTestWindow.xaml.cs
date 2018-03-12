namespace EventTraceKit.VsExtension.UITests
{
    using System.Windows;
    using EventTraceKit.VsExtension.Controls;

    public partial class TraceLogTestWindow
    {
        public TraceLogTestWindow()
        {
            InitializeComponent();
            DataContext = new TraceLogTestViewModel();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            TryFindResource(typeof(TransitioningControl));
        }
    }
}
