namespace PerfRunner
{
    using System;
    using System.Text;
    using BenchmarkDotNet.Attributes;
    using EventTraceKit.Tracing;
    using EventTraceKit.VsExtension.Filtering;
    using EventTraceKit.VsExtension.Native;

    public class TraceLogFilterBenchmark
    {
        private BufferAllocator allocator;
        private EventRecordCPtr recordPtr;
        private TraceEventInfoCPtr infoPtr;

        private TraceLogFilterPredicate filterPredicate1;
        private TraceLogFilterPredicate filterPredicate2;
        private TraceLogFilterPredicate filterPredicate3;

        [GlobalSetup]
        public void Setup()
        {
            allocator = new BufferAllocator();

            var record = new EventRecordBuilder(allocator);
            record.Record.EventHeader.ProviderId = new Guid("6D35524C-C587-476A-92D3-F333D223BDCF");
            record.Record.EventHeader.EventDescriptor.Id = 1;
            record.AddArg(32);

            var info = new TraceEventInfoBuilder(allocator);
            info.EventMessage = "Foo %1 Bar";
            info.PropertyCount = 1;
            info.TopLevelPropertyCount = 1;
            info.EventPropertyInfos = new[] {
                new EVENT_PROPERTY_INFO {
                    InType = TDH_IN_TYPE.INT32,
                    OutType = TDH_OUT_TYPE.INT,
                    countAndCountPropertyIndex = 1,
                    lengthAndLengthPropertyIndex = 4
                }
            };

            recordPtr = record.Build();
            infoPtr = info.Build();

            var filter1 = new TraceLogFilter();
            for (int i = 0; i < 30; ++i) {
                filter1.Conditions.Add(
                    new TraceLogFilterCondition("ProviderId == {6D35524C-C587-476A-92D3-F333D223BDCF} && Id == " + (i + 2), true, FilterConditionAction.Exclude));
            }

            var filter2 = new TraceLogFilter();
            var expr = new StringBuilder();
            expr.Append("ProviderId == {6D35524C-C587-476A-92D3-F333D223BDCF} && (");
            for (int i = 0; i < 30; ++i) {
                if (i > 0)
                    expr.Append(" || ");
                expr.Append($"Id == {i + 2}");
            }
            expr.Append(")");
            filter2.Conditions.Add(
                new TraceLogFilterCondition(expr.ToString(), true, FilterConditionAction.Exclude));

            var filter3 = new TraceLogFilter();
            filter3.Conditions.Add(
                new TraceLogFilterCondition("ProviderId == {6D35524C-C587-476A-92D3-F333D223BDCF} && Id != 1", true, FilterConditionAction.Exclude));

            filterPredicate1 = filter1.CreatePredicate();
            filterPredicate2 = filter2.CreatePredicate();
            filterPredicate3 = filter3.CreatePredicate();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            allocator.Dispose();
        }

        [Benchmark]
        public unsafe void Filter1()
        {
            filterPredicate1(new IntPtr(recordPtr.Ptr), new IntPtr(infoPtr.Ptr), new UIntPtr(infoPtr.Size));
        }

        [Benchmark]
        public unsafe void Filter2()
        {
            filterPredicate2(new IntPtr(recordPtr.Ptr), new IntPtr(infoPtr.Ptr), new UIntPtr(infoPtr.Size));
        }

        [Benchmark]
        public unsafe void Filter3()
        {
            filterPredicate3(new IntPtr(recordPtr.Ptr), new IntPtr(infoPtr.Ptr), new UIntPtr(infoPtr.Size));
        }
    }
}
