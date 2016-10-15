namespace EventTraceKit.VsExtension.Controls
{
    using System.Windows;
    using System.Windows.Controls;

    public static class SelectorUtils
    {
        public static bool IsSelectable(DependencyObject container)
        {
            if (container == null)
                return false;

            if (!ItemGetIsSelectable(container))
                return false;

            var control = ItemsControl.ItemsControlFromItemContainer(container);
            if (control == null)
                return false;

            object item = control.ItemContainerGenerator.ItemFromContainer(container);
            if (!ReferenceEquals(item, container))
                return ItemGetIsSelectable(item);

            return true;
        }

        public static bool ItemGetIsSelectable(object item)
        {
            return item != null && !(item is Separator);
        }
    }
}
