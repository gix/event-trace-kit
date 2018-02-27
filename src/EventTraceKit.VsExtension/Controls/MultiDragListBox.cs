namespace EventTraceKit.VsExtension.Controls
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    /// <summary>
    ///   ListBox which allows dragging multiple selected items.
    /// </summary>
    public class MultiDragListBox : ListBox
    {
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new MultiDragListBoxItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is MultiDragListBoxItem;
        }

        internal void HandleDeferredSelect(MultiDragListBoxItem clickedItem)
        {
            if (SelectionMode == SelectionMode.Multiple) {
                clickedItem.IsSelected = false;
                return;
            }

            SelectedItems.Clear();
            SelectedIndex = ItemContainerGenerator.IndexFromContainer(clickedItem);
        }
    }

    public class MultiDragListBoxItem : ListBoxItem
    {
        private bool deferredSelect;

        private MultiDragListBox ParentListBox =>
            ItemsControl.ItemsControlFromItemContainer(this) as MultiDragListBox;

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (IsSelected && Keyboard.Modifiers == ModifierKeys.None) {
                // Suppress the default selection behavior and defer it until
                // the next mouse up event.
                e.Handled = true;
                deferredSelect = true;
                if (SelectorUtils.IsSelectable(this))
                    Focus();
            } else
                deferredSelect = false;

            base.OnMouseLeftButtonDown(e);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (deferredSelect) {
                ParentListBox.HandleDeferredSelect(this);
                deferredSelect = false;
            }

            base.OnMouseLeftButtonUp(e);
        }
    }
}
