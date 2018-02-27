namespace EventTraceKit.VsExtension.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Text;
    using Native;
    using Xunit;
    using Xunit.Abstractions;

    public class MessageFormatterTest
    {
        private readonly ITestOutputHelper output;

        public MessageFormatterTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        private class EventRecordBuilder
        {
            private readonly BufferAllocator allocator;
            private readonly List<byte> userData = new List<byte>();

            public EventRecordBuilder(BufferAllocator allocator)
            {
                this.allocator = allocator;
            }

            public void AddArg(int value)
            {
                AddArg(BitConverter.GetBytes(value));
            }

            public void AddArg(byte[] bytes)
            {
                foreach (var b in bytes)
                    userData.Add(b);
            }

            public unsafe EventRecordCPtr Build()
            {
                if (userData.Count > ushort.MaxValue)
                    throw new OverflowException();

                ushort userDataLength = (ushort)userData.Count;
                var record = (EVENT_RECORD*)allocator.Allocate(sizeof(EVENT_RECORD) + userDataLength);
                *record = new EVENT_RECORD();
                record->UserData = (IntPtr)((byte*)record + sizeof(EVENT_RECORD));
                record->UserDataLength = userDataLength;
                Marshal.Copy(userData.ToArray(), 0, record->UserData, userDataLength);

                return new EventRecordCPtr(record);
            }
        }

        private class TraceEventInfoBuilder
        {
            private readonly BufferAllocator allocator;

            public TraceEventInfoBuilder(BufferAllocator allocator)
            {
                this.allocator = allocator;
            }

            public Guid ProviderGuid { get; set; }
            public Guid EventGuid { get; set; }
            public EVENT_DESCRIPTOR EventDescriptor { get; set; }
            public DecodingSource DecodingSource { get; set; }

            public string ProviderName { get; set; }
            public string LevelName { get; set; }
            public string ChannelName { get; set; }
            public string KeywordsName { get; set; }
            public string TaskName { get; set; }
            public string OpcodeName { get; set; }
            public string EventMessage { get; set; }
            public string ProviderMessage { get; set; }
            //public uint BinaryXMLOffset { get; set; }
            //public uint BinaryXMLSize { get; set; }
            public string ActivityIDName { get; set; }
            public string RelatedActivityIDName { get; set; }
            public uint PropertyCount { get; set; }
            public uint TopLevelPropertyCount { get; set; }
            public TEMPLATE_FLAGS Flags { get; set; }
            public EVENT_PROPERTY_INFO[] EventPropertyInfos { get; set; }

            public unsafe TraceEventInfoCPtr Build()
            {
                var strings = new[] {
                    ProviderName,
                    LevelName,
                    ChannelName,
                    KeywordsName,
                    TaskName,
                    OpcodeName,
                    EventMessage,
                    ProviderMessage,
                    ActivityIDName,
                    RelatedActivityIDName
                };

                var size = sizeof(TRACE_EVENT_INFO);
                size += sizeof(EVENT_PROPERTY_INFO) * Math.Max(0, (int)PropertyCount - 1);
                int stringsOffset = size;
                foreach (var s in strings) {
                    if (!string.IsNullOrEmpty(s))
                        size += (s.Length + 1) * 2;
                }

                var info = (TRACE_EVENT_INFO*)allocator.Allocate(size);
                *info = new TRACE_EVENT_INFO();
                info->ProviderGuid = ProviderGuid;
                info->EventGuid = EventGuid;
                info->EventDescriptor = EventDescriptor;
                info->DecodingSource = DecodingSource;
                info->PropertyCount = PropertyCount;
                info->TopLevelPropertyCount = TopLevelPropertyCount;
                info->Flags = Flags;
                for (int i = 0; i < EventPropertyInfos.Length; ++i)
                    (&info->EventPropertyInfoArray)[i] = EventPropertyInfos[i];

                uint offset = (uint)stringsOffset;
                info->ProviderNameOffset = AddString(info, ref offset, ProviderName);
                info->LevelNameOffset = AddString(info, ref offset, LevelName);
                info->ChannelNameOffset = AddString(info, ref offset, ChannelName);
                info->KeywordsNameOffset = AddString(info, ref offset, KeywordsName);
                info->TaskNameOffset = AddString(info, ref offset, TaskName);
                info->OpcodeNameOffset = AddString(info, ref offset, OpcodeName);
                info->EventMessageOffset = AddString(info, ref offset, EventMessage);
                info->ProviderMessageOffset = AddString(info, ref offset, ProviderMessage);
                info->ActivityIDNameOffset = AddString(info, ref offset, ActivityIDName);
                info->RelatedActivityIDNameOffset = AddString(info, ref offset, RelatedActivityIDName);

                return new TraceEventInfoCPtr(info, (uint)size);
            }

            private unsafe uint AddString(TRACE_EVENT_INFO* buffer, ref uint offset, string str)
            {
                if (string.IsNullOrEmpty(str))
                    return 0;

                var bytes = Encoding.Unicode.GetBytes(str);
                Marshal.Copy(bytes, 0, (IntPtr)((byte*)buffer + offset), bytes.Length);
                ((byte*)buffer)[offset + bytes.Length] = 0;
                ((byte*)buffer)[offset + bytes.Length + 1] = 0;
                uint result = offset;
                offset += (uint)(bytes.Length + 2);
                return result;
            }
        }

        private sealed class CoTaskMemHandle : SafeHandle
        {
            public CoTaskMemHandle(IntPtr handle)
                : base(handle, true)
            {
            }

            public override bool IsInvalid => handle == IntPtr.Zero;

            protected override bool ReleaseHandle()
            {
                Marshal.FreeCoTaskMem(handle);
                return true;
            }
        }

        private sealed class BufferAllocator : IDisposable
        {
            private readonly List<CoTaskMemHandle> buffers = new List<CoTaskMemHandle>();

            public IntPtr Allocate(int size)
            {
                var buffer = Marshal.AllocCoTaskMem(size);
                buffers.Add(new CoTaskMemHandle(buffer));
                return buffer;
            }

            public void Dispose()
            {
                foreach (var buffer in buffers)
                    buffer.Dispose();
            }
        }

        [Fact]
        public unsafe void GetMessageForEventRecord()
        {
            var allocator = new BufferAllocator();

            var record = new EventRecordBuilder(allocator);
            record.AddArg(32);
            var recordPtr = record.Build();

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
            var infoPtr = info.Build();

            var context = new ParseTdhContext();
            var formatProvider = CultureInfo.InvariantCulture;
            string message;

            var sw = new Stopwatch();

            sw.Restart();
            for (int i = 0; i < 100000; ++i) {
                message = TdhHelper.GetMessageForEventRecord(
                    recordPtr, infoPtr, context, formatProvider);
                Assert.Equal("Foo 32 Bar", message);
            }
            sw.Stop();
            output.WriteLine("TdhHelper: {0}", sw.ElapsedMilliseconds);

            var formatter = new NativeTdhFormatter();

            sw.Restart();
            for (int i = 0; i < 100000; ++i) {
                var eventInfo = new EventInfo {
                    EventRecord = (IntPtr)recordPtr.Ptr,
                    TraceEventInfo = (IntPtr)infoPtr.Ptr,
                    TraceEventInfoSize = (UIntPtr)infoPtr.Size
                };
                message = formatter.GetMessageForEvent(
                    eventInfo, context, formatProvider);
                Assert.Equal("Foo 32 Bar", message);
            }
            sw.Stop();
            output.WriteLine("EMF: {0}", sw.ElapsedMilliseconds);

            allocator.Dispose();
        }
    }
}
