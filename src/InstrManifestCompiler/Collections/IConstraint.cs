namespace InstrManifestCompiler.Collections
{
    public interface IConstraint<in T>
    {
        bool IsSatisfiedBy(T entity);
        bool IsSatisfiedBy(T entity, IDiagnostics diags);
        string FormatMessage(T entity);
    }
}
