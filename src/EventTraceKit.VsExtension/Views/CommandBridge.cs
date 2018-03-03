namespace EventTraceKit.VsExtension.Views
{
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Interactivity;

    public class CommandBridge : Behavior<UIElement>
    {
        public static readonly DependencyProperty SourceCommandProperty =
            DependencyProperty.Register(
                nameof(SourceCommand),
                typeof(ICommand),
                typeof(CommandBridge));

        public static readonly DependencyProperty TargetCommandProperty =
            DependencyProperty.Register(
                nameof(TargetCommand),
                typeof(ICommand),
                typeof(CommandBridge));

        public ICommand SourceCommand
        {
            get => (ICommand)GetValue(SourceCommandProperty);
            set => SetValue(SourceCommandProperty, value);
        }

        public ICommand TargetCommand
        {
            get => (ICommand)GetValue(TargetCommandProperty);
            set => SetValue(TargetCommandProperty, value);
        }

        protected override void OnAttached()
        {
            AddBinding(SourceCommand, TargetCommand);
        }

        private void AddBinding(ICommand source, ICommand target)
        {
            var binding = new CommandBinding(
                source,
                (s, e) => Execute(e, target),
                (s, e) => CanExecute(e, target));

            AssociatedObject.CommandBindings.Add(binding);
        }

        private static void CanExecute(CanExecuteRoutedEventArgs args, ICommand command)
        {
            if (command != null) {
                args.CanExecute = command.CanExecute(args.Parameter);
                args.ContinueRouting = false;
            }
        }

        private static void Execute(ExecutedRoutedEventArgs e, ICommand command)
        {
            command?.Execute(e.Parameter);
        }
    }
}
