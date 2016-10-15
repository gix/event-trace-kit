namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Controls;
    using Serialization;

    public static class ListExtensions
    {
        private static T[] ConvertToArray<T>(IList list)
        {
            return list.Cast<T>().ToArray();
        }

        public static object[] ConvertToTypedArray(this IList list)
        {
            Type itemType = DetermineItemType(list);
            var convertMethod = typeof(ListExtensions).GetMethod(
                nameof(ConvertToArray), BindingFlags.Static | BindingFlags.NonPublic);

            var genericMethod = convertMethod.MakeGenericMethod(itemType);
            return (object[])genericMethod.Invoke(null, new object[] { list });
        }

        private static Type DetermineItemType(IList list)
        {
            Type itemType;
            if (TypeHelper.TryGetGenericListItemType(list.GetType(), out itemType) &&
                itemType != typeof(object))
                return itemType;

            if (list.Count > 0) {
                itemType = null;
                foreach (var item in list) {
                    if (item == null)
                        continue;
                    if (itemType == null)
                        itemType = item.GetType();
                    else if (itemType != item.GetType()) {
                        itemType = null;
                        break;
                    }
                }

                if (itemType != null)
                    return itemType;
            }

            return typeof(object);
        }
    }

    public static class DataObjectExtensions
    {
        public static bool TryGetArray<T>(this IDataObject data, out T[] payload)
        {
            if (data.GetDataPresent(typeof(T))) {
                payload = new[] { (T)data.GetData(typeof(T)) };
                return true;
            }

            if (data.GetDataPresent(typeof(T[]))) {
                payload = (T[])data.GetData(typeof(T[]));
                return true;
            }

            payload = null;
            return false;
        }
    }

    public static class VectorUtils
    {
        public static Vector Abs(Vector vector)
        {
            return new Vector(Math.Abs(vector.X), Math.Abs(vector.Y));
        }
    }

    public static class ItemsControlExtensions
    {
        public static object[] GetOrderedSelectedItemsArray(
            this ListBox listBox)
        {
            return listBox.SelectedItems.Cast<object>().OrderBy(x => listBox.Items.IndexOf(x)).ToList().ConvertToTypedArray();
        }

        public static ListBox GetParentListBox(
            this ListBoxItem item)
        {
            return (ListBox)ItemsControl.ItemsControlFromItemContainer(item);
        }
    }
}
