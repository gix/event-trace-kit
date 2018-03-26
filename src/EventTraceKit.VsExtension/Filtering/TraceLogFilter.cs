namespace EventTraceKit.VsExtension.Filtering
{
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using EventTraceKit.Tracing;

    public class TraceLogFilter
    {
        public IList<TraceLogFilterCondition> Conditions { get; } =
            new List<TraceLogFilterCondition>();

        public TraceLogFilterPredicate CreatePredicate()
        {
            return TraceLogFilterBuilder.Instance.CreatePredicate(this);
        }
    }

    public class TraceLogFilterCondition
    {
        public TraceLogFilterCondition(
            Expression property, bool isEnabled, FilterRelationKind relation,
            FilterConditionAction action, object value)
        {
            IsEnabled = isEnabled;
            Action = action;

            Property = property;
            Relation = relation;
            Value = value;
        }

        public TraceLogFilterCondition(
            string expression, bool isEnabled, FilterConditionAction action)
        {
            IsEnabled = isEnabled;
            Action = action;

            Expression = expression;
        }

        public bool IsEnabled { get; }
        public FilterConditionAction Action { get; }

        public Expression Property { get; }
        public FilterRelationKind Relation { get; }
        public object Value { get; }

        public string Expression { get; }
    }

    public enum FilterConditionAction
    {
        Include,
        Exclude
    }

    public enum FilterRelationKind
    {
        Equal,
        NotEqual,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanorEqual,
        StartsWith,
        EndsWith,
        Contains,
        Excludes
    }
}
