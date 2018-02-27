namespace EventTraceKit.VsExtension.Controls.Primitives
{
    using System.Collections.ObjectModel;

    public static class CollectionDefaults<T>
    {
        public static ReadOnlyObservableCollection<T> ReadOnlyObservable { get; } =
            new ReadOnlyObservableCollection<T>(new ObservableCollection<T>());
    }
}
