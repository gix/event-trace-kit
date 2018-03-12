namespace EventTraceKit.VsExtension.Filtering
{
    using System.Linq;

    public class FilterConditionViewModel : ObservableModel
    {
        private bool isEnabled;
        private string displayName;
        private FilterRelation relation;
        private object value;
        private FilterConditionAction action;

        public FilterConditionViewModel(IModelProperty property)
        {
            Property = property;
            DisplayName = property.Name;
        }

        public FilterConditionViewModel(
            IModelProperty property, TraceLogFilterCondition condition)
            : this(property)
        {
            IsEnabled = condition.IsEnabled;
            Action = condition.Action;
            Value = condition.Value;
            Relation = property.Relations.Single(x => x.Kind == condition.Relation);
        }

        public IModelProperty Property { get; }

        public bool IsEnabled
        {
            get => isEnabled;
            set => SetProperty(ref isEnabled, value);
        }

        public string DisplayName
        {
            get => displayName;
            set => SetProperty(ref displayName, value);
        }

        public FilterRelation Relation
        {
            get => relation;
            set => SetProperty(ref relation, value);
        }

        public FilterConditionAction Action
        {
            get => action;
            set => SetProperty(ref action, value);
        }

        public object Value
        {
            get => value;
            set => SetProperty(ref this.value, value);
        }
    }
}
