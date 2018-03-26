namespace EventTraceKit.VsExtension.Filtering
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using EventTraceKit.VsExtension.Extensions;

    public class FilterDialogViewModel : ObservableModel, INotifyDataErrorInfo
    {
        private readonly TraceLogFilterBuilder builder = TraceLogFilterBuilder.Instance;

        private bool advancedMode;
        private bool? dialogResult;

        private IModelProperty selectedProperty;
        private FilterRelation selectedRelation;
        private ValueHolder valueHolder;
        private string expression;
        private FilterConditionAction selectedAction;
        private FilterConditionViewModel selectedCondition;

        public FilterDialogViewModel()
        {
            var simpleRelations = new List<FilterRelation>();
            simpleRelations.Add(new FilterRelation("==", FilterRelationKind.Equal));
            simpleRelations.Add(new FilterRelation("!=", FilterRelationKind.NotEqual));

            var numericRelations = new List<FilterRelation>(simpleRelations);
            numericRelations.Add(new FilterRelation(">", FilterRelationKind.GreaterThan));
            numericRelations.Add(new FilterRelation(">=", FilterRelationKind.GreaterThanOrEqual));
            numericRelations.Add(new FilterRelation("<", FilterRelationKind.LessThan));
            numericRelations.Add(new FilterRelation("<=", FilterRelationKind.LessThanorEqual));

            var stringRelations = new List<FilterRelation>(simpleRelations);
            stringRelations.Add(new FilterRelation("starts with", FilterRelationKind.StartsWith));
            stringRelations.Add(new FilterRelation("ends with", FilterRelationKind.EndsWith));
            stringRelations.Add(new FilterRelation("contains", FilterRelationKind.Contains));
            stringRelations.Add(new FilterRelation("excludes", FilterRelationKind.Excludes));

            Properties.Add(new NumericProperty<ushort>("Id", numericRelations, builder.Id));
            Properties.Add(new NumericProperty<byte>("Version", numericRelations, builder.Version));
            Properties.Add(new NumericProperty<byte>("Channel", numericRelations, builder.Channel));
            Properties.Add(new NumericProperty<byte>("Level", numericRelations, builder.Level));
            Properties.Add(new NumericProperty<byte>("Opcode", numericRelations, builder.Opcode));
            Properties.Add(new NumericProperty<ushort>("Task", numericRelations, builder.Task));
            Properties.Add(new NumericProperty<ulong>("Keyword", numericRelations, builder.Keyword));

            //ushort HeaderType;
            //ushort Flags;
            //ushort EventProperty;
            Properties.Add(new NumericProperty<uint>("ThreadId", numericRelations, builder.ThreadId));
            Properties.Add(new NumericProperty<uint>("ProcessId", numericRelations, builder.ProcessId));
            //properties.Add(new TimePointProperty("TimeStamp", builder.TimePoint));
            Properties.Add(new GuidProperty("ProviderId", simpleRelations, builder.ProviderId));
            //TimeUnionStruct TimeUnion;
            Properties.Add(new GuidProperty("ActivityId", simpleRelations, builder.ActivityId));
            Properties.Add(new NumericProperty<ushort>("UserDataLength", numericRelations, builder.UserDataLength));
            //Properties.Add(new EnumProperty<DecodingSource>("DecodingSource", builder.DecodingSource, simpleRelations));

            Properties.Add(new GuidProperty("RelatedActivityId", simpleRelations, builder.RelatedActivityId));

            // providerNamePreset;
            // channelNamePreset;
            // levelNamePreset;
            // taskNamePreset;
            // opcodeOrTypePreset;
            // opcodeNamePreset;
            // eventNamePreset;
            // messagePreset;
            // eventTypePreset;
            // symbolPreset;
            // cpuPreset;
            // relatedActivityIdPreset;
            // userSecurityIdentifierPreset;
            // sessionIdPreset;
            // eventKeyPreset;
            // timePointGeneratorPreset;
            // timeAbsoluteGeneratorPreset;
            // timeRelativeGeneratorPreset;
            // decodingSourcePreset;

            ResetCommand = new AsyncDelegateCommand(ResetConditions);
            AddCommand = new AsyncDelegateCommand(AddCondition, CanAddCondition);
            RemoveCommand = new AsyncDelegateCommand(RemoveCondition, CanRemoveCondition);

            AcceptCommand = new AsyncDelegateCommand(Accept, CanAccept);
            ApplyCommand = new AsyncDelegateCommand(Apply, CanApply);
        }

        public ObservableCollection<FilterConditionViewModel> Conditions { get; } =
            new ObservableCollection<FilterConditionViewModel>();

        public ObservableCollection<IModelProperty> Properties { get; } =
            new ObservableCollection<IModelProperty>();

        public ObservableCollection<FilterRelation> Relations { get; } =
            new ObservableCollection<FilterRelation>();

        public ICommand ResetCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand AcceptCommand { get; }
        public ICommand ApplyCommand { get; }

        public bool AdvancedMode
        {
            get => advancedMode;
            set => SetProperty(ref advancedMode, value);
        }

        public bool? DialogResult
        {
            get => dialogResult;
            set => SetProperty(ref dialogResult, value);
        }

        public FilterConditionAction SelectedAction
        {
            get => selectedAction;
            set => SetProperty(ref selectedAction, value);
        }

        public IModelProperty SelectedProperty
        {
            get => selectedProperty;
            set
            {
                if (SetProperty(ref selectedProperty, value))
                    OnSelectedPropertyChanged();
            }
        }

        public TraceLogFilter GetFilter()
        {
            var filter = new TraceLogFilter();
            filter.Conditions.AddRange(Conditions.Select(x => x.CreateCondition()));
            return filter;
        }

        public void SetFilter(TraceLogFilter filter)
        {
            Conditions.Clear();
            if (filter != null)
                Conditions.AddRange(filter.Conditions.Select(CreateCondition));
        }

        private FilterConditionViewModel CreateCondition(TraceLogFilterCondition condition)
        {
            if (condition.Expression != null)
                return new AdvancedFilterConditionViewModel(condition);
            var property = Properties.Single(x => x.Expression == condition.Property);
            return new SimpleFilterConditionViewModel(property, condition);
        }

        private Task ResetConditions()
        {
            Conditions.Clear();
            return Task.CompletedTask;
        }

        private bool CanAddCondition()
        {
            if (AdvancedMode)
                return Expression != null;
            return
                SelectedProperty != null &&
                SelectedRelation != null &&
                ValueHolder != null;
        }

        private Task AddCondition()
        {
            if (!CanAddCondition())
                return Task.CompletedTask;

            FilterConditionViewModel condition;
            if (AdvancedMode) {
                condition = new AdvancedFilterConditionViewModel {
                    IsEnabled = true,
                    Expression = Expression,
                    Action = SelectedAction
                };
            } else {
                condition = new SimpleFilterConditionViewModel(SelectedProperty) {
                    IsEnabled = true,
                    Relation = SelectedRelation,
                    Value = ValueHolder.RawValue,
                    Action = SelectedAction
                };
            }

            int idx = Conditions.BinarySearch(condition, CompareCondition);
            if (idx < 0)
                idx = ~idx;

            Conditions.Insert(idx, condition);

            return Task.CompletedTask;
        }

        private static int CompareCondition(FilterConditionViewModel x, FilterConditionViewModel y)
        {
            if (x.Action != y.Action)
                return x.Action == FilterConditionAction.Include ? -1 : 1;

            if (x.GetType() != y.GetType())
                return x is SimpleFilterConditionViewModel ? -1 : 1;

            int cmp = string.Compare(x.DisplayName, y.DisplayName, StringComparison.OrdinalIgnoreCase);
            if (cmp != 0)
                return cmp;

            return 0;
        }

        private bool CanRemoveCondition()
        {
            return SelectedCondition != null;
        }

        private Task RemoveCondition()
        {
            if (SelectedCondition != null) {
                SelectedProperty = null;
                SelectedRelation = null;
                ValueHolder = null;
                Expression = null;

                switch (SelectedCondition) {
                    case AdvancedFilterConditionViewModel advanced:
                        AdvancedMode = true;
                        Expression = advanced.Expression;
                        break;
                    case SimpleFilterConditionViewModel simple:
                        AdvancedMode = false;
                        SelectedProperty = simple.Property;
                        SelectedRelation = simple.Relation;
                        var value = SelectedProperty.CreateValue();
                        value.RawValue = simple.Value;
                        ValueHolder = value;
                        SelectedAction = simple.Action;
                        break;
                }
            }

            Conditions.Remove(SelectedCondition);
            return Task.CompletedTask;
        }

        private bool CanAccept()
        {
            return true;
        }

        private Task Accept()
        {
            DialogResult = true;
            return Task.CompletedTask;
        }

        private bool CanApply()
        {
            return true;
        }

        private Task Apply()
        {
            return Task.CompletedTask;
        }

        private void OnSelectedPropertyChanged()
        {
            var prevRelation = SelectedRelation;
            Relations.Clear();
            if (SelectedProperty != null) {
                Relations.AddRange(SelectedProperty.Relations);
                if (Relations.Contains(prevRelation))
                    SelectedRelation = prevRelation;
                else
                    SelectedRelation = Relations.FirstOrDefault();

                ValueHolder = SelectedProperty.CreateValue();
            }
        }

        public FilterRelation SelectedRelation
        {
            get => selectedRelation;
            set => SetProperty(ref selectedRelation, value);
        }

        public ValueHolder ValueHolder
        {
            get => valueHolder;
            set => SetProperty(ref valueHolder, value);
        }

        public string Expression
        {
            get => expression;
            set
            {
                if (SetProperty(ref expression, value))
                    CheckExpression();
            }
        }

        private string expressionError;

        private void CheckExpression()
        {
            expressionError = null;
            try {
                new ExpressionFactoryVisitor().Visit(
                    FilterSyntaxFactory.ParseExpression(expression));
            } catch (Exception ex) {
                expressionError = ex.Message;
            }

            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Expression)));
        }

        public FilterConditionViewModel SelectedCondition
        {
            get => selectedCondition;
            set => SetProperty(ref selectedCondition, value);
        }

        public IEnumerable GetErrors(string propertyName)
        {
            if (propertyName == nameof(Expression) && expressionError != null)
                return new[] { expressionError };
            return null;
        }

        public bool HasErrors => expressionError != null;
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
    }
}
