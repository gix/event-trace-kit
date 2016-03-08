namespace InstrManifestCompiler.Collections
{
    using System;
    using System.Diagnostics.Contracts;

    [ContractClass(typeof(IConstraintContract<>))]
    public interface IConstraint<in T>
    {
        bool IsSatisfiedBy(T entity);
        bool IsSatisfiedBy(T entity, IDiagnostics diags);
        string FormatMessage(T entity);
    }

    [ContractClassFor(typeof(IConstraint<>))]
    internal abstract class IConstraintContract<T> : IConstraint<T>
    {
        bool IConstraint<T>.IsSatisfiedBy(T entity)
        {
            return default(bool);
        }

        bool IConstraint<T>.IsSatisfiedBy(T entity, IDiagnostics diags)
        {
            Contract.Requires<ArgumentNullException>(diags != null);
            return default(bool);
        }

        string IConstraint<T>.FormatMessage(T entity)
        {
            Contract.Ensures(Contract.Result<string>() != null);
            return default(string);
        }
    }
}
