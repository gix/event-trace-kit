namespace EventTraceKit.VsExtension.Views
{
    using System;
    using System.Collections.ObjectModel;

    public class AcqRelObservableCollection<T> : ObservableCollection<T>
        where T : class
    {
        private readonly Action<T> releaseItem;
        private readonly Action<T> acquireItem;

        public AcqRelObservableCollection(Action<T> releaseItem, Action<T> acquireItem)
        {
            static void NoAction(T _)
            {
            }

            this.releaseItem = releaseItem ?? NoAction;
            this.acquireItem = acquireItem ?? NoAction;
        }

        protected override void ClearItems()
        {
            foreach (var oldItem in Items) {
                if (oldItem != null)
                    releaseItem(oldItem);
            }
            base.ClearItems();
        }

        protected override void InsertItem(int index, T item)
        {
            if (item != null)
                acquireItem(item);
            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            var oldItem = Items[index];
            base.RemoveItem(index);
            if (oldItem != null)
                releaseItem(oldItem);
        }

        protected override void SetItem(int index, T item)
        {
            var oldItem = Items[index];
            if (oldItem != null)
                releaseItem(oldItem);
            base.SetItem(index, item);
            if (item != null)
                acquireItem(item);
        }
    }
}
