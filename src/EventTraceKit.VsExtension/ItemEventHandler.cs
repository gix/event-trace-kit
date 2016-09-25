namespace EventTraceKit.VsExtension
{
    using System;

    public delegate void ItemEventHandler<T>(object sender, ItemEventArgs<T> e);

    public class ItemEventArgs<T> : EventArgs
    {
        public ItemEventArgs(T item)
        {
            Item = item;
        }

        public T Item { get; }
    }
}
