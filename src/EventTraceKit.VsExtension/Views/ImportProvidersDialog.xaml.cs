namespace EventTraceKit.VsExtension.Views
{
    using System.Threading.Tasks;
    using System.Windows;

    public partial class ImportProvidersDialog
    {
        private AsyncDelegateCommand importCommand;

        public static readonly DependencyProperty CanExecuteProperty =
            DependencyProperty.Register(
                nameof(CanExecute),
                typeof(bool),
                typeof(ImportProvidersDialog),
                new PropertyMetadata(
                    Boxed.False,
                    (d, e) => ((ImportProvidersDialog)d).OnCanExecuteChanged(e),
                    (d, v) => ((ImportProvidersDialog)d).CoerceCanExecuteProperty(v)));

        public static readonly DependencyProperty ImportDeclarationsProperty =
            DependencyProperty.Register(
                nameof(ImportDeclarations),
                typeof(string),
                typeof(ImportProvidersDialog),
                new PropertyMetadata(
                    string.Empty,
                    (d, e) => ((ImportProvidersDialog)d).OnImportDeclarationsChanged(e)));

        private object CoerceCanExecuteProperty(object baseValue)
        {
            var trimmedName = ImportDeclarations.Trim();
            return !string.IsNullOrWhiteSpace(trimmedName);
        }

        public ImportProvidersDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        public static string Prompt(Window owner = null)
        {
            var dialog = new ImportProvidersDialog();
            if (owner != null)
                dialog.Owner = owner;
            if (dialog.ShowDialog() != true)
                return string.Empty;
            return dialog.ImportDeclarations;
        }

        public bool CanExecute => (bool)GetValue(CanExecuteProperty);

        public string ImportDeclarations
        {
            get => (string)GetValue(ImportDeclarationsProperty);
            set => SetValue(ImportDeclarationsProperty, value);
        }

        public AsyncDelegateCommand ImportCommand
        {
            get
            {
                if (importCommand == null)
                    importCommand = new AsyncDelegateCommand(Import, () => CanExecute);
                return importCommand;
            }
        }

        private Task Import()
        {
            DialogResult = true;
            Close();
            return Task.CompletedTask;
        }

        private void OnCanExecuteChanged(DependencyPropertyChangedEventArgs e)
        {
            ImportCommand.RaiseCanExecuteChanged();
        }

        private void OnImportDeclarationsChanged(DependencyPropertyChangedEventArgs e)
        {
            CoerceValue(CanExecuteProperty);
        }
    }
}
