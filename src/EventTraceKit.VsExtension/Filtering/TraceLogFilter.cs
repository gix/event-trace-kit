namespace EventTraceKit.VsExtension.Filtering
{
    using System.Collections.Generic;
    using System.Linq.Expressions;

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
            Property = property;
            IsEnabled = isEnabled;
            Relation = relation;
            Action = action;
            Value = value;
        }

        public Expression Property { get; }
        public bool IsEnabled { get; }
        public FilterRelationKind Relation { get; }
        public FilterConditionAction Action { get; }
        public object Value { get; }
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
