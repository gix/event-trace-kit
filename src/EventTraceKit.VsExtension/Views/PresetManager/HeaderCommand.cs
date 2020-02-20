namespace EventTraceKit.VsExtension.Views.PresetManager
{
    using System.Windows;
    using System.Windows.Input;
    using Microsoft.VisualStudio.PlatformUI;
    using Windows;

    public class HeaderCommand : DependencyObject
    {
        private DelegateCommand executeCommand;

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                nameof(Command),
                typeof(ICommand),
                typeof(HeaderCommand),
                new PropertyMetadata(null));

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register(
                nameof(CommandParameter),
                typeof(object),
                typeof(HeaderCommand),
                new PropertyMetadata(null));

        public static readonly DependencyProperty DisplayNameProperty =
            DependencyProperty.Register(
                nameof(DisplayName),
                typeof(string),
                typeof(HeaderCommand),
                PropertyMetadataUtils.DefaultNull);

        public static readonly DependencyProperty IsCheckableProperty =
            DependencyProperty.Register(
                nameof(IsCheckable),
                typeof(bool),
                typeof(HeaderCommand),
                new PropertyMetadata(Boxed.False));

        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register(
                nameof(IsChecked),
                typeof(bool),
                typeof(HeaderCommand),
                new PropertyMetadata(Boxed.False));

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.Register(
                nameof(IsEnabled),
                typeof(bool),
                typeof(HeaderCommand),
                new PropertyMetadata(Boxed.True));

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public object CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        public string DisplayName
        {
            get => (string)GetValue(DisplayNameProperty);
            set => SetValue(DisplayNameProperty, value);
        }

        public ICommand ExecuteCommand
        {
            get
            {
                VerifyAccess();
                if (executeCommand == null)
                    executeCommand = new DelegateCommand(obj => Execute());
                return executeCommand;
            }
        }

        public bool IsCheckable
        {
            get => (bool)GetValue(IsCheckableProperty);
            set => SetValue(IsCheckableProperty, Boxed.Bool(value));
        }

        public bool IsChecked
        {
            get => (bool)GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, Boxed.Bool(value));
        }

        public bool IsEnabled
        {
            get => (bool)GetValue(IsEnabledProperty);
            set => SetValue(IsEnabledProperty, Boxed.Bool(value));
        }

        public void Execute()
        {
            VerifyAccess();
            OnExecute();
        }

        protected virtual void OnExecute()
        {
            ICommand command = Command;
            object parameter = CommandParameter;
            if (command != null && command.CanExecute(parameter))
                command.Execute(parameter);
        }
    }
}
