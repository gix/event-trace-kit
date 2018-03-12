namespace EventTraceKit.VsExtension.Tests
{
    using EventTraceKit.VsExtension.Filtering;
    using Xunit;

    public class FilterTest
    {
        [Fact]
        public void Create()
        {
            var builder = TraceLogFilterBuilder.Instance;

            var filter = new TraceLogFilter();
            filter.Conditions.Add(new TraceLogFilterCondition(
                builder.Id, true, FilterRelationKind.Equal, FilterConditionAction.Include, (ushort)1));
            filter.CreatePredicate();
        }
    }
}
