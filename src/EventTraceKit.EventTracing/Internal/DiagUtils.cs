namespace EventTraceKit.EventTracing.Internal
{
    using EventTraceKit.EventTracing.Collections;
    using EventTraceKit.EventTracing.Support;

    internal static class DiagUtils
    {
        public static void ReportError<T, TProperty>(
            T entity,
            LocatedRef<TProperty> value,
            IDiagnostics diags,
            IUniqueConstraint<T> constraint) where TProperty : class
        {
            diags.ReportError(value.Location, constraint.FormatMessage(entity));
        }

        public static void ReportError<T, TProperty>(
            T entity,
            LocatedVal<TProperty> value,
            IDiagnostics diags,
            IUniqueConstraint<T> constraint)
            where TProperty : struct
        {
            diags.ReportError(value.Location, constraint.FormatMessage(entity));
        }

        public static void ReportError<T, TProperty>(
            T entity,
            LocatedNullable<TProperty> value,
            IDiagnostics diags,
            IUniqueConstraint<T> constraint)
            where TProperty : struct
        {
            diags.ReportError(value.Location, constraint.FormatMessage(entity));
        }

        public static void ReportError<T, TProperty>(
            T entity,
            TProperty value,
            IDiagnostics diags,
            IUniqueConstraint<T> constraint)
        {
            diags.ReportError(
                entity is ISourceItem sourceItem ? sourceItem.Location : new SourceLocation(),
                constraint.FormatMessage(entity));
        }
    }
}
