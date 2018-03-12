namespace EventTraceKit.VsExtension.Filtering
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Runtime.InteropServices;
    using EventTraceKit.VsExtension.Extensions;
    using EventTraceKit.VsExtension.Native;

    public class TraceLogFilterBuilder
    {
        public static readonly TraceLogFilterBuilder Instance = new TraceLogFilterBuilder();

        private TraceLogFilterBuilder()
        {
            EvtPtr = Expression.Parameter(typeof(IntPtr), "evtPtr");
            InfoPtr = Expression.Parameter(typeof(IntPtr), "infoPtr");
            InfoSize = Expression.Parameter(typeof(UIntPtr), "infoSize");
            Evt = Expression.Parameter(typeof(FastEventRecordCPtr), "evt");
            Info = Expression.Parameter(typeof(TraceEventInfoCPtr), "info");
            RelatedActivityId = Expression.Call(Evt, nameof(EventRecordCPtr.TryGetRelatedActivityId), null);
            EventHeader = Expression.Property(Evt, nameof(FastEventRecordCPtr.EventHeader));
            EventDescriptor = Expression.Property(EventHeader, nameof(EventHeaderCPtr.EventDescriptor));
            TimePoint = Expression.Property(Evt, nameof(FastEventRecordCPtr.TimePoint));
            UserDataLength = Expression.Property(Evt, nameof(FastEventRecordCPtr.UserDataLength));
            DecodingSource = Expression.Property(Info, nameof(TraceEventInfoCPtr.DecodingSource));
            ProviderId = Expression.Property(EventHeader, nameof(EventHeaderCPtr.ProviderId));
            ProcessId = Expression.Property(EventHeader, nameof(EventHeaderCPtr.ProcessId));
            ThreadId = Expression.Property(EventHeader, nameof(EventHeaderCPtr.ThreadId));
            ActivityId = Expression.Property(EventHeader, nameof(EventHeaderCPtr.ActivityId));
            Id = Expression.Field(EventDescriptor, nameof(EVENT_DESCRIPTOR.Id));
            Version = Expression.Field(EventDescriptor, nameof(EVENT_DESCRIPTOR.Version));
            Channel = Expression.Field(EventDescriptor, nameof(EVENT_DESCRIPTOR.Channel));
            Level = Expression.Field(EventDescriptor, nameof(EVENT_DESCRIPTOR.Level));
            Task = Expression.Field(EventDescriptor, nameof(EVENT_DESCRIPTOR.Task));
            Opcode = Expression.Field(EventDescriptor, nameof(EVENT_DESCRIPTOR.Opcode));
            Keyword = Expression.Field(EventDescriptor, nameof(EVENT_DESCRIPTOR.Keyword));
        }

        public ParameterExpression EvtPtr { get; }
        public ParameterExpression InfoPtr { get; }
        public ParameterExpression InfoSize { get; }

        public ParameterExpression Evt { get; }
        public ParameterExpression Info { get; }

        private Expression EventHeader { get; }
        private Expression EventDescriptor { get; }

        public Expression TimePoint { get; }
        public Expression UserDataLength { get; }
        public Expression DecodingSource { get; }

        public Expression ProviderId { get; }
        public Expression ProcessId { get; }
        public Expression ThreadId { get; }
        public Expression ActivityId { get; }

        public Expression Id { get; }
        public Expression Version { get; }
        public Expression Channel { get; }
        public Expression Level { get; }
        public Expression Task { get; }
        public Expression Opcode { get; }
        public Expression Keyword { get; }

        public Expression RelatedActivityId { get; }

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

        public Expression<TraceLogFilterPredicate> CreatePredicateExpr(TraceLogFilter filter)
        {
            var enabled = filter.Conditions.Where(x => x.IsEnabled).ToList();
            var includedConds = enabled.Where(x => x.Action == FilterConditionAction.Include).ToList();
            var excludedConds = enabled.Where(x => x.Action == FilterConditionAction.Exclude).ToList();

            var includeExpr = ExpressionEx.OrElse(includedConds.Select(CreateComparisonExpr), true);
            var excludeExpr = ExpressionEx.OrElse(excludedConds.Select(CreateComparisonExpr), false);
            var filterExpr = Expression.AndAlso(includeExpr, Expression.Not(excludeExpr));

            return CreateLambda(filterExpr);
        }

        public TraceLogFilterPredicate CreatePredicate(TraceLogFilter filter)
        {
            // Lambdas compiled with LambdaExpression.Compile perform costly
            // security checks (SecurityTransparent, APTCA, class access checks
            // like SecurityCritical or SecuritySafeCritical, and LinkDemands),
            // see clr!JIT_MethodAccessCheck in vm/jithelpers.cpp.
            // The only way to avoid these seems to be to use a proper (dynamic)
            // assembly. We use a GC-collectable transient assembly to host the
            // delegate.
            var predicate = CreatePredicateExpr(filter).CompileToTransientAssembly();

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

        private Expression CreateComparisonExpr(TraceLogFilterCondition condition)
        {
            var property = condition.Property;
            var value = Expression.Constant(condition.Value);
            switch (condition.Relation) {
                case FilterRelationKind.Equal:
                    return Expression.Equal(property, value);
                case FilterRelationKind.NotEqual:
                    return Expression.NotEqual(property, value);
                case FilterRelationKind.GreaterThan:
                    return Expression.GreaterThan(property, value);
                case FilterRelationKind.GreaterThanOrEqual:
                    return Expression.GreaterThanOrEqual(property, value);
                case FilterRelationKind.LessThan:
                    return Expression.LessThan(property, value);
                case FilterRelationKind.LessThanorEqual:
                    return Expression.LessThanOrEqual(property, value);
                case FilterRelationKind.StartsWith:
                    return property.IfNull(
                        Expression.Call(property, nameof(string.StartsWith), null, value),
                        Expression.Constant(false));
                case FilterRelationKind.EndsWith:
                    return property.IfNull(
                        Expression.Call(property, nameof(string.EndsWith), null, value),
                        Expression.Constant(false));
                case FilterRelationKind.Contains:
                    return property.IfNull(
                        Expression.Call(property, nameof(string.Contains), null, value),
                        Expression.Constant(false));
                case FilterRelationKind.Excludes:
                    return property.IfNull(
                        Expression.Not(Expression.Call(property, nameof(string.Contains), null, value)),
                        Expression.Constant(false));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}