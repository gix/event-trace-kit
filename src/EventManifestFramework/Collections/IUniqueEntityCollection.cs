namespace EventManifestFramework.Collections
{
    using System.Collections.Generic;
    using EventManifestFramework.Support;

    public interface IUniqueEntityCollection<in T>
    {
        bool IsUnique(T entity);
        bool IsUnique(T entity, IDiagnostics diags);
        bool TryAdd(T entity);
        bool TryAdd(T entity, IDiagnostics diags);
    }

    public interface IUniqueEntityList<T>
        : IList<T>, IReadOnlyList<T>, IUniqueEntityCollection<T>
    {
    }
}
