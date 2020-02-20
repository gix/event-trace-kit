namespace EventTraceKit.EventTracing.Collections
{
    using System.Collections.Generic;
    using EventTraceKit.EventTracing.Support;

    public interface IConstraint<in T>
    {
        bool IsSatisfiedBy(IReadOnlyList<T> collection, T item);
        bool IsSatisfiedBy(IReadOnlyList<T> collection, T item, IDiagnostics diags);
        string FormatMessage(T item);
    }
}
