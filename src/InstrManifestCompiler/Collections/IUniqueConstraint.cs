namespace InstrManifestCompiler.Collections
{
    public interface IUniqueConstraint<in T> : IConstraint<T>
    {
        bool Changed(T oldEntity, T newEntity);
        void NotifyAdd(T entity);
        void NotifyRemove(T entity);
    }
}
