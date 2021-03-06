namespace EventTraceKit.EventTracing.Collections
{
    using System;
    using System.Collections.Generic;
    using EventTraceKit.EventTracing.Support;

    internal interface IUniqueConstraintOptions<T, out TProperty>
    {
        IUniqueConstraintOptions<T, TProperty> Using(IEqualityComparer<TProperty> comparer);
        IUniqueConstraintOptions<T, TProperty> IfNotNull();
        IUniqueConstraintOptions<T, TProperty> WithMessage(string format, params Func<T, object>[] args);
        IUniqueConstraintOptions<T, TProperty> WithMessage(Func<T, string> formatter);
        IUniqueConstraintOptions<T, TProperty> DiagnoseUsing(
            Action<T, TProperty, IDiagnostics, IUniqueConstraint<T>> reporter);
    }
}
