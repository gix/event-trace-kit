﻿namespace EventTraceKit.VsExtension.Extensions
{
    using System;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;

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