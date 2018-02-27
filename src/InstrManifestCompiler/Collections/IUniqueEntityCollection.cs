namespace InstrManifestCompiler.Collections
{
    using System.Collections;
    using System.Collections.Generic;

    public interface IUniqueEntityCollection<in T>
    {
        bool IsUnique(T entity);
        bool IsUnique(T entity, IDiagnostics diags);
        bool TryAdd(T entity);
        bool TryAdd(T entity, IDiagnostics diags);
    }

    public interface IUniqueEntityList<T>
        : IList<T>, IList, IReadOnlyList<T>, IUniqueEntityCollection<T>
    {
    }
}
