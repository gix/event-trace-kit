namespace EventTraceKit.VsExtension.Extensions
{
    using System.Collections.ObjectModel;

    public static class CollectionUtils
    {
        public static ReadOnlyObservableCollection<T> InitializeReadOnly<T>(
            out ObservableCollection<T> collection)
        {
            collection = new ObservableCollection<T>();
            return new ReadOnlyObservableCollection<T>(collection);
        }
    }
}
