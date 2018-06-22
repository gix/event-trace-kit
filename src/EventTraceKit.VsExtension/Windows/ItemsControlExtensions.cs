namespace EventTraceKit.VsExtension.Windows
{
    using System.Linq;
    using System.Windows.Controls;
    using EventTraceKit.VsExtension.Extensions;

    public static class ItemsControlExtensions
    {
        public static object[] GetOrderedSelectedItemsArray(
            this ListBox listBox)
        {
            return listBox.SelectedItems.Cast<object>().OrderBy(
                x => listBox.Items.IndexOf(x)).ToList().ConvertToTypedArray();
        }

        public static ListBox GetParentListBox(
            this ListBoxItem item)
        {
            return (ListBox)ItemsControl.ItemsControlFromItemContainer(item);
        }
    }
}
