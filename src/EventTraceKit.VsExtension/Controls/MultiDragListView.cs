namespace EventTraceKit.VsExtension.Controls
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    /// <summary>
    ///   ListView which allows dragging multiple selected items.
    /// </summary>
    public class MultiDragListView : ListView
    {
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new MultiDragListViewItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is MultiDragListViewItem;
        }

        internal void HandleDeferredSelect(MultiDragListViewItem clickedItem)
        {
            if (SelectionMode == SelectionMode.Multiple) {
                clickedItem.IsSelected = false;
                return;
            }

            SelectedItems.Clear();
            SelectedIndex = ItemContainerGenerator.IndexFromContainer(clickedItem);
        }
    }

    public class MultiDragListViewItem : ListViewItem
    {
        private bool deferredSelect;

        private MultiDragListView ParentListView =>
            ItemsControl.ItemsControlFromItemContainer(this) as MultiDragListView;

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
                ParentListView.HandleDeferredSelect(this);
                deferredSelect = false;
            }

            base.OnMouseLeftButtonUp(e);
        }
    }
}
