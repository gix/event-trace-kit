namespace EventTraceKit.VsExtension.Views
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Collections;
    using Extensions;
    using Native;

    public class FilterDialogViewModel : ViewModel
    {
        private bool? dialogResult;

        private IModelProperty selectedProperty;
        private FilterRelation selectedRelation;
        private ValueHolder valueHolder;
        private FilterConditionAction selectedAction;
        private FilterCondition selectedCondition;

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

            Properties.Add(new NumericProperty<ushort>("Id", numericRelations, x => x.Id));
            Properties.Add(new NumericProperty<byte>("Version", numericRelations, x => x.Version));
            Properties.Add(new NumericProperty<byte>("Channel", numericRelations, x => x.Channel));
            Properties.Add(new NumericProperty<byte>("Level", numericRelations, x => x.Level));
            Properties.Add(new NumericProperty<byte>("Opcode", numericRelations, x => x.Opcode));
            Properties.Add(new NumericProperty<ushort>("Task", numericRelations, x => x.Task));
            Properties.Add(new NumericProperty<ulong>("Keyword", numericRelations, x => x.Keyword));

            //ushort HeaderType;
            //ushort Flags;
            //ushort EventProperty;
            Properties.Add(new NumericProperty<uint>("ThreadId", numericRelations, x => x.ThreadId));
            Properties.Add(new NumericProperty<uint>("ProcessId", numericRelations, x => x.ProcessId));
            //properties.Add(new TimePointProperty("TimeStamp", builder.TimePoint));
            Properties.Add(new GuidProperty("ProviderId", simpleRelations, x => x.ProviderId));
            //TimeUnionStruct TimeUnion;
            Properties.Add(new GuidProperty("ActivityId", simpleRelations, x => x.ActivityId));
            Properties.Add(new NumericProperty<ushort>("UserDataLength", numericRelations, x => x.UserDataLength));
            //Properties.Add(new EnumProperty<DecodingSource>("DecodingSource", builder.DecodingSource, simpleRelations));

            Properties.Add(new GuidProperty("RelatedActivityId", simpleRelations, x => x.RelatedActivityId));

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

        public ObservableCollection<FilterCondition> Conditions { get; } =
            new ObservableCollection<FilterCondition>();

        public ObservableCollection<IModelProperty> Properties { get; } =
            new ObservableCollection<IModelProperty>();

        public ObservableCollection<FilterRelation> Relations { get; } =
            new ObservableCollection<FilterRelation>();

        public ICommand ResetCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand AcceptCommand { get; }
        public ICommand ApplyCommand { get; }

        public bool? DialogResult
        {
            get => dialogResult;
            set => SetProperty(ref dialogResult, value);
        }

        public TraceLogFilter GetFilter()
        {
            return new TraceLogFilter(Conditions);
        }

        public void SetFilter(TraceLogFilter filter)
        {
            Conditions.Clear();
            if (filter != null)
                Conditions.AddRange(filter.Conditions.Select(x => x.Clone()));
        }

        private Task ResetConditions()
        {
            Conditions.Clear();
            return Task.CompletedTask;
        }

        private bool CanAddCondition()
        {
            return
                SelectedProperty != null &&
                SelectedRelation != null &&
                ValueHolder != null;
        }

        private Task AddCondition()
        {
            if (!CanAddCondition())
                return Task.CompletedTask;

            var condition = new FilterCondition(SelectedProperty) {
                IsEnabled = true,
                Relation = SelectedRelation,
                Value = ValueHolder.RawValue,
                Action = SelectedAction
            };

            int idx = Conditions.BinarySearch(condition, CompareCondition);
            if (idx < 0)
                idx = ~idx;

            Conditions.Insert(idx, condition);

            return Task.CompletedTask;
        }

        private int CompareCondition(FilterCondition x, FilterCondition y)
        {
            if (x.Action != y.Action)
                return x.Action == FilterConditionAction.Include ? -1 : 1;

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
                SelectedProperty = SelectedCondition.Property;
                SelectedRelation = SelectedCondition.Relation;
                var value = SelectedProperty.CreateValue();
                value.RawValue = SelectedCondition.Value;
                ValueHolder = value;
                SelectedAction = SelectedCondition.Action;
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

        public FilterCondition SelectedCondition
        {
            get => selectedCondition;
            set => SetProperty(ref selectedCondition, value);
        }
    }

    public class TraceLogFilter
    {
        public TraceLogFilter()
            : this(Enumerable.Empty<FilterCondition>())
        {
        }

        public TraceLogFilter(IEnumerable<FilterCondition> conditions)
        {
            Conditions = new List<FilterCondition>(conditions);
        }

        public IList<FilterCondition> Conditions { get; }

        public Expression<TraceLogFilterPredicate> CreatePredicateExpr()
        {
            var builder = new TraceLogFilterBuilder();

            var enabled = Conditions.Where(x => x.IsEnabled).ToList();
            var includedConds = enabled.Where(x => x.Action == FilterConditionAction.Include).ToList();
            var excludedConds = enabled.Where(x => x.Action == FilterConditionAction.Exclude).ToList();

            var includeExpr = ExprUtils.OrElse(includedConds.Select(x => CreateComparisonExpr(x, builder)), true);
            var excludeExpr = ExprUtils.OrElse(excludedConds.Select(x => CreateComparisonExpr(x, builder)), false);
            var filterExpr = Expression.AndAlso(includeExpr, Expression.Not(excludeExpr));

            return builder.CreateLambda(filterExpr);
        }

        public TraceLogFilterPredicate CreatePredicate()
        {
            // Lambdas compiled with LambdaExpression.Compile perform costly
            // security checks (SecurityTransparent, APTCA, class access checks
            // like SecurityCritical or SecuritySafeCritical, and LinkDemands),
            // see clr!JIT_MethodAccessCheck in vm/jithelpers.cpp.
            // The only way to avoid these seems to be to use a proper (dynamic)
            // assembly. We use a GC-collectable transient assembly to host the
            // delegate.
            var predicate = CreatePredicateExpr().CompileToTransientAssembly();

            // Force JIT-compiling the delegate, executing the pre-stub.
            // Otherwise calling the delegate involves calls to:
            //   clr!PrecodeFixupThunk
            //   clr!ThePreStub
            //   clr!PreStubWorker
            //   ...
            // This must be done twice, no idea exactly why.
            Warmup(predicate);
            Warmup(predicate);

            return predicate;
        }

        private static unsafe void Warmup(TraceLogFilterPredicate predicate)
        {
            var record = new EVENT_RECORD();
            var info = new TRACE_EVENT_INFO();
            predicate((IntPtr)(&record), (IntPtr)(&info), (UIntPtr)Marshal.SizeOf<TRACE_EVENT_INFO>());
        }

        private Expression CreateComparisonExpr(FilterCondition condition, TraceLogFilterBuilder builder)
        {
            var propertyExpr = condition.Property.CreateExpression(builder);
            var value = Expression.Constant(condition.Value);
            switch (condition.Relation.Kind) {
                case FilterRelationKind.Equal:
                    return Expression.Equal(propertyExpr, value);
                case FilterRelationKind.NotEqual:
                    return Expression.NotEqual(propertyExpr, value);
                case FilterRelationKind.GreaterThan:
                    return Expression.GreaterThan(propertyExpr, value);
                case FilterRelationKind.GreaterThanOrEqual:
                    return Expression.GreaterThanOrEqual(propertyExpr, value);
                case FilterRelationKind.LessThan:
                    return Expression.LessThan(propertyExpr, value);
                case FilterRelationKind.LessThanorEqual:
                    return Expression.LessThanOrEqual(propertyExpr, value);
                case FilterRelationKind.StartsWith:
                    return ExprUtils.NullCheck(
                        propertyExpr,
                        Expression.Call(propertyExpr, nameof(string.StartsWith), null, value),
                        Expression.Constant(false));
                case FilterRelationKind.EndsWith:
                    return ExprUtils.NullCheck(
                        propertyExpr,
                        Expression.Call(propertyExpr, nameof(string.EndsWith), null, value),
                        Expression.Constant(false));
                case FilterRelationKind.Contains:
                    return ExprUtils.NullCheck(
                        propertyExpr,
                        Expression.Call(propertyExpr, nameof(string.Contains), null, value),
                        Expression.Constant(false));
                case FilterRelationKind.Excludes:
                    return ExprUtils.NullCheck(
                        propertyExpr,
                        Expression.Not(Expression.Call(propertyExpr, nameof(string.Contains), null, value)),
                        Expression.Constant(false));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public class FilterCondition : ViewModel
    {
        private bool isEnabled;
        private string displayName;
        private FilterRelation relation;
        private object value;
        private FilterConditionAction action;

        public FilterCondition(IModelProperty property)
        {
            Property = property;
            DisplayName = property.Name;
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

        public object Value
        {
            get => value;
            set => SetProperty(ref this.value, value);
        }

        public FilterConditionAction Action
        {
            get => action;
            set => SetProperty(ref action, value);
        }

        public FilterCondition Clone()
        {
            return new FilterCondition(Property) {
                IsEnabled = IsEnabled,
                Relation = Relation,
                Value = value,
                Action = action,
            };
        }
    }

    public enum FilterConditionAction
    {
        Include,
        Exclude
    }

    public interface IModelProperty
    {
        string Name { get; }
        IEnumerable<FilterRelation> Relations { get; }
        Expression CreateExpression(TraceLogFilterBuilder builder);
        ValueHolder CreateValue();
    }

    public class FilterRelation : IEquatable<FilterRelation>
    {
        public FilterRelation(string displayName, FilterRelationKind kind)
        {
            DisplayName = displayName;
            Kind = kind;
        }

        public string DisplayName { get; }
        public FilterRelationKind Kind { get; }

        public bool Equals(FilterRelation other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(DisplayName, other.DisplayName) && Kind == other.Kind;
        }

        public override bool Equals(object obj)
        {
            var kind = obj as FilterRelation;
            return Equals(kind);
        }

        public override int GetHashCode()
        {
            unchecked {
                return ((DisplayName?.GetHashCode() ?? 0) * 397) ^ (int)Kind;
            }
        }

        public static bool operator ==(FilterRelation left, FilterRelation right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(FilterRelation left, FilterRelation right)
        {
            return !Equals(left, right);
        }
    }

    public abstract class ExpressionProperty<T> : IModelProperty
    {
        private readonly Func<TraceLogFilterBuilder, Expression> exprFactory;

        protected ExpressionProperty(
            string name, IEnumerable<FilterRelation> relations,
            Func<TraceLogFilterBuilder, Expression> exprFactory)
        {
            Name = name;
            this.exprFactory = exprFactory;
            Relations = relations;
        }

        public string Name { get; }
        public IEnumerable<FilterRelation> Relations { get; }

        public Expression CreateExpression(TraceLogFilterBuilder builder)
        {
            return exprFactory(builder);
        }

        public ValueHolder CreateValue()
        {
            return CreateValueCore();
        }

        protected virtual ValueHolder<T> CreateValueCore()
        {
            return new ValueHolder<T>();
        }
    }

    public class NumericProperty<T> : ExpressionProperty<T>
    {
        public NumericProperty(
            string name, IEnumerable<FilterRelation> relations,
            Func<TraceLogFilterBuilder, Expression> exprFactory)
            : base(name, relations, exprFactory)
        {
        }
    }

    public class GuidProperty : ExpressionProperty<Guid>
    {
        public GuidProperty(
            string name, IEnumerable<FilterRelation> relations,
            Func<TraceLogFilterBuilder, Expression> exprFactory)
            : base(name, relations, exprFactory)
        {
        }
    }

    public class EnumProperty<T> : ExpressionProperty<T>
    {
        public EnumProperty(
            string name, IEnumerable<FilterRelation> relations,
            Func<TraceLogFilterBuilder, Expression> exprFactory)
            : base(name, relations, exprFactory)
        {
        }
    }

    public abstract class ValueHolder
    {
        public abstract object RawValue { get; set; }
    }

    public class ValueHolder<T> : ValueHolder
    {
        public T Value { get; set; }
        public override object RawValue
        {
            get => Value;
            set => Value = (T)value;
        }
    }

    public class TraceLogFilterBuilder
    {
        public TraceLogFilterBuilder()
        {
            EvtPtr = Expression.Parameter(typeof(IntPtr), "evtPtr");
            InfoPtr = Expression.Parameter(typeof(IntPtr), "infoPtr");
            InfoSize = Expression.Parameter(typeof(UIntPtr), "infoSize");

            Evt = Expression.Parameter(typeof(FastEventRecordCPtr), "evt");
            Info = Expression.Parameter(typeof(TraceEventInfoCPtr), "info");
        }

        public ParameterExpression EvtPtr { get; }
        public ParameterExpression InfoPtr { get; }
        public ParameterExpression InfoSize { get; }

        public ParameterExpression Evt { get; }
        public ParameterExpression Info { get; }

        private Expression EventHeader => Expression.Property(Evt, nameof(EventRecordCPtr.EventHeader));
        private Expression EventDescriptor => Expression.Property(EventHeader, nameof(EventHeaderCPtr.EventDescriptor));

        public Expression TimePoint => Expression.Field(Evt, nameof(EventRecordCPtr.TimePoint));
        public Expression UserDataLength => Expression.Property(Evt, nameof(EventRecordCPtr.UserDataLength));
        public Expression DecodingSource => Expression.Property(Info, nameof(TraceEventInfoCPtr.DecodingSource));

        public Expression ProviderId => Expression.Property(EventHeader, nameof(EventHeaderCPtr.ProviderId));
        public Expression ProcessId => Expression.Property(EventHeader, nameof(EventHeaderCPtr.ProcessId));
        public Expression ThreadId => Expression.Property(EventHeader, nameof(EventHeaderCPtr.ThreadId));
        public Expression ActivityId => Expression.Property(EventHeader, nameof(EventHeaderCPtr.ActivityId));

        public Expression Id => Expression.Field(EventDescriptor, nameof(EVENT_DESCRIPTOR.Id));
        public Expression Version => Expression.Field(EventDescriptor, nameof(EVENT_DESCRIPTOR.Version));
        public Expression Channel => Expression.Field(EventDescriptor, nameof(EVENT_DESCRIPTOR.Channel));
        public Expression Level => Expression.Field(EventDescriptor, nameof(EVENT_DESCRIPTOR.Level));
        public Expression Task => Expression.Field(EventDescriptor, nameof(EVENT_DESCRIPTOR.Task));
        public Expression Opcode => Expression.Field(EventDescriptor, nameof(EVENT_DESCRIPTOR.Opcode));
        public Expression Keyword => Expression.Field(EventDescriptor, nameof(EVENT_DESCRIPTOR.Keyword));

        public Expression RelatedActivityId => Expression.Call(Evt, nameof(EventRecordCPtr.TryGetRelatedActivityId), null);

        public Expression<TraceLogFilterPredicate> CreateLambda(Expression body)
        {
            var eventRecordCtor = typeof(FastEventRecordCPtr).GetConstructor(new[] { typeof(IntPtr) });
            var traceInfoCtor = typeof(TraceEventInfoCPtr).GetConstructor(new[] { typeof(IntPtr), typeof(UIntPtr) });

            var evtInit = Expression.Assign(Evt, Expression.New(eventRecordCtor, EvtPtr));
            var infoInit = Expression.Assign(Info, Expression.New(traceInfoCtor, InfoPtr, InfoSize));

            var block = Expression.Block(new[] { Evt, Info }, evtInit, infoInit, body);
            var lambda = Expression.Lambda<TraceLogFilterPredicate>(
                block, EvtPtr, InfoPtr, InfoSize);

            return lambda;
        }
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

    public static class ExprUtils
    {
        public static T CompileToTransientAssembly<T>(this Expression<T> lambda)
        {
            var id = Guid.NewGuid();
            var assemblyName = new AssemblyName($"DelegateHostAssembly_{id:N}");
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                assemblyName, AssemblyBuilderAccess.RunAndCollect);

            var moduleBuilder = assemblyBuilder.DefineDynamicModule("transient");
            var typeBuilder = moduleBuilder.DefineType("DelegateHost");
            var methodBuilder = typeBuilder.DefineMethod(
                "Execute", MethodAttributes.Public | MethodAttributes.Static);

            lambda.CompileToMethod(methodBuilder);

            var lambdaHost = typeBuilder.CreateType();
            var execute = lambdaHost.GetMethod(methodBuilder.Name);

            return (T)(object)Delegate.CreateDelegate(typeof(T), execute);
        }

        public static Expression NullCheck(Expression expr, Expression ifTrue, Expression ifFalse)
        {
            return Expression.Condition(IsNull(expr), ifTrue, ifFalse);
        }

        public static Expression IsNull(Expression expr)
        {
            return Expression.Equal(expr, Expression.Constant(null, expr.Type));
        }

        public static Expression AndOr(IEnumerable<Expression> expressions)
        {
            var enumerator = expressions.GetEnumerator();
            try {
                if (!enumerator.MoveNext())
                    return Expression.Constant(true);

                Expression expr = enumerator.Current;
                while (enumerator.MoveNext())
                    expr = Expression.AndAlso(expr, enumerator.Current);
                return expr;
            } finally {
                enumerator.Dispose();
            }
        }

        public static Expression OrElse(IEnumerable<Expression> expressions, bool defaultValue)
        {
            var enumerator = expressions.GetEnumerator();
            try {
                if (!enumerator.MoveNext())
                    return Expression.Constant(defaultValue);

                Expression expr = enumerator.Current;
                while (enumerator.MoveNext())
                    expr = Expression.OrElse(expr, enumerator.Current);
                return expr;
            } finally {
                enumerator.Dispose();
            }
        }
    }
}
