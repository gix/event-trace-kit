namespace EventTraceKit.VsExtension.Filtering
{
    using System.Linq;

    public abstract class FilterConditionViewModel : ObservableModel
    {
        private string displayName;
        private bool isEnabled;
        private FilterConditionAction action;

        public string DisplayName
        {
            get => displayName;
            set => SetProperty(ref displayName, value);
        }

        public bool IsEnabled
        {
            get => isEnabled;
            set => SetProperty(ref isEnabled, value);
        }

        public FilterConditionAction Action
        {
            get => action;
            set => SetProperty(ref action, value);
        }

        public abstract TraceLogFilterCondition CreateCondition();
    }

    public class SimpleFilterConditionViewModel : FilterConditionViewModel
    {
        private FilterRelation relation;
        private object value;

        public SimpleFilterConditionViewModel(IModelProperty property)
        {
            Property = property;
            DisplayName = property.Name;
        }

        public SimpleFilterConditionViewModel(
            IModelProperty property, TraceLogFilterCondition condition)
            : this(property)
        {
            IsEnabled = condition.IsEnabled;
            Action = condition.Action;
            Relation = property.Relations.Single(x => x.Kind == condition.Relation);
            Value = condition.Value;
        }

        public IModelProperty Property { get; }

        public FilterRelation Relation
        {
            get => relation;
            set
            {
                if (SetProperty(ref relation, value))
                    UpdateDisplayName();
            }
        }

        public object Value
        {
            get => value;
            set
            {
                if (SetProperty(ref this.value, value))
                    UpdateDisplayName();
            }
        }

        public override TraceLogFilterCondition CreateCondition()
        {
            return new TraceLogFilterCondition(
                Property.Expression, IsEnabled, Relation.Kind, Action, Value);
        }

        private void UpdateDisplayName()
        {
            DisplayName = $"{Property.Name} {Relation?.DisplayName ?? "??"} {Value}";
        }
    }

    public class AdvancedFilterConditionViewModel : FilterConditionViewModel
    {
        private string expression;

        public AdvancedFilterConditionViewModel()
        {
        }

        public AdvancedFilterConditionViewModel(TraceLogFilterCondition condition)
        {
            IsEnabled = condition.IsEnabled;
            Action = condition.Action;
            Expression = condition.Expression;
        }

        public string Expression
        {
            get => expression;
            set
            {
                if (SetProperty(ref expression, value))
                    OnExpressionChanged();
            }
        }

        private void OnExpressionChanged()
        {
            DisplayName = expression;
        }

        public override TraceLogFilterCondition CreateCondition()
        {
            return new TraceLogFilterCondition(Expression, IsEnabled, Action);
        }
    }
}
