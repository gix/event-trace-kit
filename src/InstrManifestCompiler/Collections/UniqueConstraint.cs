namespace InstrManifestCompiler.Collections
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using InstrManifestCompiler.Support;

    internal sealed class UniqueConstraint<T, TProperty>
        : IUniqueConstraint<T>
        , IUniqueConstraintOptions<T, TProperty>
    {
        private readonly Func<T, TProperty> selector;
        private ISet<TProperty> uniqueValues;
        private bool optional;
        private IEqualityComparer<TProperty> comparer = EqualityComparer<TProperty>.Default;

        private string customMessage;
        private Func<T, object>[] customMessageArgs;
        private Func<T, string> customMessageFormatter;
        private Action<T, TProperty, IDiagnostics, IUniqueConstraint<T>> reportDiagnostic;

        public UniqueConstraint(Func<T, TProperty> selector)
        {
            this.selector = selector;
            uniqueValues = new HashSet<TProperty>(comparer);
            reportDiagnostic = DefaultReportDiagnostic;
        }

        public bool IsSatisfiedBy(T entity)
        {
            TProperty value = selector(entity);
            if (optional && (value == null || value.Equals(null)))
                return true;
            return !uniqueValues.Contains(value);
        }

        public bool IsSatisfiedBy(T entity, IDiagnostics diags)
        {
            TProperty value = selector(entity);
            if (optional && (value == null || value.Equals(null)))
                return true;
            if (!uniqueValues.Contains(value))
                return true;

            reportDiagnostic(entity, value, diags, this);
            return false;
        }

        public string FormatMessage(T entity)
        {
            if (customMessageFormatter != null)
                return customMessageFormatter(entity);
            if (customMessage != null) {
                if (customMessageArgs == null)
                    return customMessage;
                var args = customMessageArgs.Select(sel => sel(entity)).ToArray();
                return string.Format(customMessage, args);
            }

            return $"Duplicate entity '{typeof(T).Name}'";
        }

        public bool Changed(T oldEntity, T newEntity)
        {
            return comparer.Equals(selector(oldEntity), selector(newEntity));
        }

        public void NotifyAdd(T entity)
        {
            uniqueValues.Add(selector(entity));
        }

        public void NotifyRemove(T entity)
        {
            uniqueValues.Remove(selector(entity));
        }

        public IUniqueConstraintOptions<T, TProperty>
            Using(IEqualityComparer<TProperty> comparer)
        {
            if (uniqueValues != null && uniqueValues.Count != 0)
                throw new InvalidOperationException();
            this.comparer = comparer;
            uniqueValues = new HashSet<TProperty>(comparer);
            return this;
        }

        public IUniqueConstraintOptions<T, TProperty> IfNotNull()
        {
            optional = true;
            return this;
        }

        public IUniqueConstraintOptions<T, TProperty> WithMessage(
            string format, params Func<T, object>[] args)
        {
            customMessage = format;
            customMessageArgs = args;
            customMessageFormatter = null;
            return this;
        }

        public IUniqueConstraintOptions<T, TProperty> WithMessage(Func<T, string> formatter)
        {
            customMessage = null;
            customMessageArgs = null;
            customMessageFormatter = formatter;
            return this;
        }

        public IUniqueConstraintOptions<T, TProperty> DiagnoseUsing(
            Action<T, TProperty, IDiagnostics, IUniqueConstraint<T>> reporter)
        {
            reportDiagnostic = reporter ?? DefaultReportDiagnostic;
            return this;
        }

        private static void DefaultReportDiagnostic(
            T entity, TProperty value, IDiagnostics diags, IUniqueConstraint<T> constraint)
        {
            diags.ReportError(new SourceLocation(), constraint.FormatMessage(entity));
        }
    }
}
