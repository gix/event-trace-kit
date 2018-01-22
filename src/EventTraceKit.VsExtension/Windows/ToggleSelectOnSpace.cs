namespace EventTraceKit.VsExtension.Windows
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Interactivity;

    public class ToggleSelectOnSpace : Behavior<DataGrid>
    {
        public static readonly DependencyProperty ToggleSelectCommandProperty =
            DependencyProperty.Register(
                nameof(ToggleSelectCommand),
                typeof(ICommand),
                typeof(ToggleSelectOnSpace));

        private bool isEditing;

        public ICommand ToggleSelectCommand
        {
            get => (ICommand)GetValue(ToggleSelectCommandProperty);
            set => SetValue(ToggleSelectCommandProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewKeyUp += OnPreviewKeyUp;
            AssociatedObject.PreviewTextInput += OnPreviewTextInput;
            AssociatedObject.BeginningEdit += OnBeginningEdit;
            AssociatedObject.CellEditEnding += OnCellEditEnding;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.CellEditEnding -= OnCellEditEnding;
            AssociatedObject.BeginningEdit -= OnBeginningEdit;
            AssociatedObject.PreviewTextInput -= OnPreviewTextInput;
            AssociatedObject.PreviewKeyUp -= OnPreviewKeyUp;
            base.OnDetaching();
        }

        private void OnBeginningEdit(object s, DataGridBeginningEditEventArgs e)
        {
            isEditing = true;
        }

        private void OnCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            isEditing = false;
        }

        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!isEditing)
                e.Handled = true;
        }

        private void OnPreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (!isEditing && e.Key == Key.Space && Keyboard.Modifiers == ModifierKeys.None) {
                if (ToggleSelectCommand != null) {
                    ToggleSelectCommand.Execute(AssociatedObject.SelectedItems);
                    e.Handled = true;
                }
            }
        }
    }
}
