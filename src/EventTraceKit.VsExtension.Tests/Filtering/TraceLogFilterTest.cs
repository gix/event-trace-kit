namespace EventTraceKit.VsExtension.Tests.Filtering
{
    using System;
    using System.Runtime.InteropServices;
    using EventTraceKit.VsExtension.Filtering;
    using EventTraceKit.VsExtension.Native;
    using Xunit;

    public class TraceLogFilterTest
    {
        [Theory]
        [InlineData("E53CD252-C0DA-49DD-93B0-28744D498D0F", 1, false)]
        [InlineData("E53CD252-C0DA-49DD-93B0-28744D498D0F", 2, true)]
        [InlineData("FF4B1F18-0D41-4DE6-A576-7FD58477A869", 1, true)]
        public unsafe void Create(string providerId, ushort id, bool expected)
        {
            var builder = TraceLogFilterBuilder.Instance;

            var filter = new TraceLogFilter();
            //filter.Conditions.Add(new TraceLogFilterCondition(
            //    builder.ProviderId, true, FilterRelationKind.NotEqual, FilterConditionAction.Exclude, new Guid("E53CD252-C0DA-49DD-93B0-28744D498D0F")));
            //filter.Conditions.Add(new TraceLogFilterCondition(
            //    builder.Id, true, FilterRelationKind.Equal, FilterConditionAction.Exclude, (ushort)1));

            filter.Conditions.Add(new TraceLogFilterCondition(
                "ProviderId == {FF4B1F18-0D41-4DE6-A576-7FD58477A869} || id != 1", true, FilterConditionAction.Include));

            var record = new EVENT_RECORD();
            var tei = new TRACE_EVENT_INFO();
            var teiSize = new UIntPtr((uint)Marshal.SizeOf<TRACE_EVENT_INFO>());

            record.EventHeader.ProviderId = new Guid(providerId);
            record.EventHeader.EventDescriptor.Id = id;

            Assert.Equal(expected, filter.CreatePredicate()(new IntPtr(&record), new IntPtr(&tei), teiSize));
        }
    }
}
