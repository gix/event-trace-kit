namespace EventTraceKit.VsExtension.Filtering
{
    using System.Windows.Input;

    public partial class FilterDialog
    {
        public FilterDialog()
        {
            InitializeComponent();
        }

        private void OnRowDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var command = (DataContext as FilterDialogViewModel)?.RemoveCommand;
            if (command != null && command.CanExecute(null))
                command.Execute(null);
        }
    }
}
