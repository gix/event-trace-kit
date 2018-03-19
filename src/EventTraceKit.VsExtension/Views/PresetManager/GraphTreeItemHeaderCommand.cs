namespace EventTraceKit.VsExtension.Views.PresetManager
{
    using System.Windows;
    using System.Windows.Input;
    using Windows;
    using Microsoft.VisualStudio.PlatformUI;

    public class GraphTreeItemHeaderCommand : DependencyObject
    {
        private DelegateCommand executeCommand;

        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register("CommandParameter", typeof(object), typeof(GraphTreeItemHeaderCommand), new PropertyMetadata(null));
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register("Command", typeof(ICommand), typeof(GraphTreeItemHeaderCommand), new PropertyMetadata(null, (s, e) => ((GraphTreeItemHeaderCommand)s).CommandPropertyChanged(e)));
        public static readonly DependencyProperty DisplayNameProperty = DependencyProperty.Register("DisplayName", typeof(string), typeof(GraphTreeItemHeaderCommand), PropertyMetadataUtils.DefaultNull);
        public static readonly DependencyProperty IsCheckableProperty = DependencyProperty.Register("IsCheckable", typeof(bool), typeof(GraphTreeItemHeaderCommand), new PropertyMetadata(Boxed.False));
        public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register("IsChecked", typeof(bool), typeof(GraphTreeItemHeaderCommand), new PropertyMetadata(Boxed.False));
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register("IsEnabled", typeof(bool), typeof(GraphTreeItemHeaderCommand), new PropertyMetadata(Boxed.True));

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

        private void CommandPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
        }

        public void Execute()
        {
            VerifyAccess();
            OnExecute();
        }

        public virtual void OnExecute()
        {
            ICommand command = Command;
            object commandParameter = CommandParameter;
            if (command != null && command.CanExecute(commandParameter)) {
                command.Execute(commandParameter);
            }
        }
    }
}
