namespace EventTraceKit.VsExtension.Windows
{
    using System.Windows;
    using System.Windows.Interactivity;
    using Microsoft.VisualStudio.PlatformUI;

    public class BindableDialogResult : Behavior<DialogWindow>
    {
        public static readonly DependencyProperty BindingProperty =
            DependencyProperty.Register(
                nameof(Binding),
                typeof(bool?),
                typeof(BindableDialogResult),
                new PropertyMetadata(OnDialogResultChanged));

        public bool? Binding
        {
            get { return (bool?)GetValue(BindingProperty); }
            set { SetValue(BindingProperty, value); }
        }

        private static void OnDialogResultChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (BindableDialogResult)d;
            source.OnDialogResultChanged((bool?)e.NewValue);
        }

        private void OnDialogResultChanged(bool? newValue)
        {
            if (!AssociatedObject.IsLoaded) {
                AssociatedObject.Loaded += OnLoaded;
                return;
            }

            if (!AssociatedObject.IsVisible) {
                AssociatedObject.IsVisibleChanged += OnIsVisibleChanged;
                return;
            }

            AssociatedObject.DialogResult = newValue;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Loaded -= OnLoaded;

            if (!AssociatedObject.IsVisible) {
                AssociatedObject.IsVisibleChanged += OnIsVisibleChanged;
                return;
            }

            AssociatedObject.DialogResult = Binding;
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            AssociatedObject.IsVisibleChanged -= OnIsVisibleChanged;
            if (AssociatedObject.IsVisible)
                AssociatedObject.DialogResult = Binding;
        }
    }
}
