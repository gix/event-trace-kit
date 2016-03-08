namespace InstrManifestCompiler.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;

    [ContractClass(typeof(IUniqueEntityCollectionContract<>))]
    public interface IUniqueEntityCollection<in T>
    {
        bool IsUnique(T entity);
        bool IsUnique(T entity, IDiagnostics diags);
        bool TryAdd(T entity);
        bool TryAdd(T entity, IDiagnostics diags);
    }

    [ContractClassFor(typeof(IUniqueEntityCollection<>))]
    internal abstract class IUniqueEntityCollectionContract<T> : IUniqueEntityCollection<T>
    {
        private IUniqueEntityCollectionContract()
        {
        }

        bool IUniqueEntityCollection<T>.IsUnique(T entity)
        {
            Contract.Requires<ArgumentNullException>(default(T) != null || entity != null);
            return default(bool);
        }

        bool IUniqueEntityCollection<T>.IsUnique(T entity, IDiagnostics diags)
        {
            Contract.Requires<ArgumentNullException>(default(T) != null || entity != null);
            Contract.Requires<ArgumentNullException>(diags != null);
            return default(bool);
        }

        bool IUniqueEntityCollection<T>.TryAdd(T entity)
        {
            Contract.Requires<ArgumentNullException>(default(T) != null || entity != null);
            return default(bool);
        }

        bool IUniqueEntityCollection<T>.TryAdd(T entity, IDiagnostics diags)
        {
            Contract.Requires<ArgumentNullException>(default(T) != null || entity != null);
            Contract.Requires<ArgumentNullException>(diags != null);
            return default(bool);
        }
    }

    public interface IUniqueEntityList<T>
        : IList<T>, IList, IReadOnlyList<T>, IUniqueEntityCollection<T>
    {
    }
}
