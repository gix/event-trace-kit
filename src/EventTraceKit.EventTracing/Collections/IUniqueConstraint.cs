namespace EventTraceKit.EventTracing.Collections
{
    public interface IUniqueConstraint<in T> : IConstraint<T>
    {
        bool Changed(T oldItem, T newItem);
        void NotifyAdd(T item);
        void NotifyRemove(T item);
    }
}
