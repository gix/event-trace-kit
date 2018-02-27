namespace InstrManifestCompiler.Collections
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;

    [ContractClass(typeof(IUniqueConstraintOptionsContract<,>))]
    public interface IUniqueConstraintOptions<T, TProperty>
    {
        IUniqueConstraintOptions<T, TProperty> Using(IEqualityComparer<TProperty> comparer);
        IUniqueConstraintOptions<T, TProperty> IfNotNull();
        IUniqueConstraintOptions<T, TProperty> WithMessage(string format, params Func<T, object>[] args);
        IUniqueConstraintOptions<T, TProperty> WithMessage(Func<T, string> formatter);
        IUniqueConstraintOptions<T, TProperty> DiagnoseUsing(
            Action<T, TProperty, IDiagnostics, IUniqueConstraint<T>> reporter);
    }

    [ContractClassFor(typeof(IUniqueConstraintOptions<,>))]
    internal abstract class IUniqueConstraintOptionsContract<T, TProperty>
        : IUniqueConstraintOptions<T, TProperty>
    {
        private IUniqueConstraintOptionsContract()
        {
        }

        IUniqueConstraintOptions<T, TProperty>
            IUniqueConstraintOptions<T, TProperty>.Using(
            IEqualityComparer<TProperty> comparer)
        {
            Contract.Requires<ArgumentNullException>(comparer != null);
            return this;
        }

        IUniqueConstraintOptions<T, TProperty>
            IUniqueConstraintOptions<T, TProperty>.IfNotNull()
        {
            return this;
        }

        IUniqueConstraintOptions<T, TProperty>
            IUniqueConstraintOptions<T, TProperty>.WithMessage(
                string format, params Func<T, object>[] args)
        {
            Contract.Requires<ArgumentNullException>(format != null);
            return this;
        }

        IUniqueConstraintOptions<T, TProperty>
            IUniqueConstraintOptions<T, TProperty>.WithMessage(
                Func<T, string> formatter)
        {
            Contract.Requires<ArgumentNullException>(formatter != null);
            return this;
        }

        IUniqueConstraintOptions<T, TProperty>
            IUniqueConstraintOptions<T, TProperty>.DiagnoseUsing(
                Action<T, TProperty, IDiagnostics, IUniqueConstraint<T>> reporter)
        {
            return this;
        }
    }
}
