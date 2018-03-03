namespace EventManifestFramework.Collections
{
    using System.Collections.Generic;
    using EventManifestFramework.Support;

    public interface IConstraint<in T>
    {
        bool IsSatisfiedBy(IReadOnlyList<T> collection, T item);
        bool IsSatisfiedBy(IReadOnlyList<T> collection, T item, IDiagnostics diags);
        string FormatMessage(T item);
    }
}
