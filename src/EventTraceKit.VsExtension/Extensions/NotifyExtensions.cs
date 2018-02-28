namespace EventTraceKit.VsExtension.Extensions
{
    using System;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;

    public static class NotifyExtensions
    {
        public static void HandleChanges<T>(
            this ObservableCollection<T> collection,
            NotifyCollectionChangedEventArgs args,
            Action<T> oldAction,
            Action<T> newAction)
        {
            switch (args.Action) {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                    if (args.OldItems != null) {
                        foreach (T item in args.OldItems)
                            oldAction(item);
                    }
                    if (args.NewItems != null) {
                        foreach (T item in args.NewItems)
                            newAction(item);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    foreach (var item in collection)
                        newAction(item);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
