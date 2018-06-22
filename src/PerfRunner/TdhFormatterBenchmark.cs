namespace PerfRunner
{
    using System.Diagnostics;
    using System.Globalization;
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Attributes.Jobs;
    using EventTraceKit;
    using EventTraceKit.VsExtension.Native;

    [ClrJob(true)]
    public class TdhFormatterBenchmark
    {
        private BufferAllocator allocator;
        private ParseTdhContext context;
        private EventRecordCPtr recordPtr;
        private TraceEventInfoCPtr infoPtr;
        private CultureInfo formatProvider;
        private NativeTdhFormatter nativeFormatter;

        [GlobalSetup]
        public void Setup()
        {
            allocator = new BufferAllocator();
            context = new ParseTdhContext();
            formatProvider = CultureInfo.InvariantCulture;
            nativeFormatter = new NativeTdhFormatter();

            var record = new EventRecordBuilder(allocator);
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
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            allocator.Dispose();
        }

        [Benchmark]
        public unsafe void Native()
        {
            var message = nativeFormatter.FormatEventMessage(
                recordPtr.Ptr, infoPtr.Ptr, infoPtr.Size, (uint)context.NativePointerSize);
            Debug.Assert(message == "Foo 32 Bar");
        }
    }
}
