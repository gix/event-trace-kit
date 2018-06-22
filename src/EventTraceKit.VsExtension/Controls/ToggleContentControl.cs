namespace EventTraceKit.VsExtension.Controls
{
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Input;

    public class ToggleContentControl : TransitioningControl
    {
        public ToggleContentControl()
        {
            DefaultStyleKey = typeof(ToggleContentControl);
            SetBinding(ContentProperty, new Binding());
        }

        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register(
                nameof(IsChecked),
                typeof(bool),
                typeof(ToggleContentControl),
                new PropertyMetadata(false, OnIsCheckedChanged));

        public static readonly DependencyProperty CheckedTemplateProperty =
            DependencyProperty.Register(
                nameof(CheckedTemplate),
                typeof(DataTemplate),
                typeof(ToggleContentControl),
                new PropertyMetadata(null, OnCheckedTemplateChanged));

        public static readonly DependencyProperty UncheckedTemplateProperty =
            DependencyProperty.Register(
                nameof(UncheckedTemplate),
                typeof(DataTemplate),
                typeof(ToggleContentControl),
                new PropertyMetadata(null, OnUncheckedTemplateChanged));

        public bool IsChecked
        {
            get => (bool)GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }

        public DataTemplate CheckedTemplate
        {
            get => (DataTemplate)GetValue(CheckedTemplateProperty);
            set => SetValue(CheckedTemplateProperty, value);
        }

        public DataTemplate UncheckedTemplate
        {
            get => (DataTemplate)GetValue(UncheckedTemplateProperty);
            set => SetValue(UncheckedTemplateProperty, value);
        }

        private static void OnIsCheckedChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (ToggleContentControl)d;
            var isChecked = (bool)e.NewValue;
            source.ContentTemplate = isChecked ? source.CheckedTemplate : source.UncheckedTemplate;
            Keyboard.Focus(source);
        }

        private static void OnCheckedTemplateChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (ToggleContentControl)d;
            if (source.IsChecked)
                source.ContentTemplate = (DataTemplate)e.NewValue;
        }

        private static void OnUncheckedTemplateChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (ToggleContentControl)d;
            if (!source.IsChecked)
                source.ContentTemplate = (DataTemplate)e.NewValue;
        }
    }
}
