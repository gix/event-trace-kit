namespace EventTraceKit.VsExtension.Windows
{
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;
    using EventTraceKit.VsExtension.Extensions;
    using Microsoft.Xaml.Behaviors;

    public class CommandBridge : Behavior<UIElement>
    {
        public static readonly DependencyProperty SourceCommandProperty =
            DependencyProperty.Register(
                nameof(SourceCommand),
                typeof(ICommand),
                typeof(CommandBridge),
                new PropertyMetadata(null, OnSourceCommandChanged));

        public static readonly DependencyProperty TargetCommandProperty =
            DependencyProperty.Register(
                nameof(TargetCommand),
                typeof(ICommand),
                typeof(CommandBridge),
                new PropertyMetadata(null, OnTargetCommandChanged));

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

        private static void OnSourceCommandChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (CommandBridge)d;
            var oldSource = (ICommand)e.OldValue;
            var newSource = (ICommand)e.NewValue;
            source.RemoveBinding(oldSource, source.TargetCommand);
            source.AddBinding(newSource, source.TargetCommand);
        }

        private static void OnTargetCommandChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (CommandBridge)d;
            var oldTarget = (ICommand)e.OldValue;
            var newTarget = (ICommand)e.NewValue;
            source.RemoveBinding(source.SourceCommand, oldTarget);
            source.AddBinding(source.SourceCommand, newTarget);
        }

        private void AddBinding(ICommand source, ICommand target)
        {
            if (AssociatedObject == null || source == null || target == null)
                return;

            var binding = new TargetCommandBinding(source, target);
            AssociatedObject.CommandBindings.Add(binding);
        }

        private void RemoveBinding(ICommand source, ICommand target)
        {
            if (AssociatedObject == null || source == null || target == null)
                return;

            var bindings = AssociatedObject.CommandBindings;
            var index = bindings.OfType<TargetCommandBinding>().IndexOf(
                x => x.Command == source && x.Target == target);

            Debug.Assert(index != -1);
            bindings.RemoveAt(index);
        }

        private static void ForwardCanExecute(ICommand command, CanExecuteRoutedEventArgs args)
        {
            if (command != null) {
                args.CanExecute = command.CanExecute(args.Parameter);
                args.ContinueRouting = false;
            }
        }

        private static void ForwardExecute(ICommand command, ExecutedRoutedEventArgs e)
        {
            command?.Execute(e.Parameter);
        }

        private sealed class TargetCommandBinding : CommandBinding
        {
            public TargetCommandBinding(ICommand source, ICommand target)
                : base(source, (s, e) => ForwardExecute(target, e), (s, e) => ForwardCanExecute(target, e))
            {
                Target = target;
            }

            public ICommand Target { get; }
        }
    }
}
