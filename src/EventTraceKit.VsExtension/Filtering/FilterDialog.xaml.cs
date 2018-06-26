namespace EventTraceKit.VsExtension.Filtering
{
    using System.Windows;
    using System.Windows.Input;

    public partial class FilterDialog
    {
        private FilterHelpDialog helpDialog;

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

        private void OnHelpButtonClicked(object sender, RoutedEventArgs e)
        {
            if (helpDialog == null) {
                helpDialog = new FilterHelpDialog {
                    Owner = this
                };
                helpDialog.Closed += delegate { helpDialog = null; };
            }

            helpDialog.Show();
            helpDialog.Focus();
        }
    }
}
