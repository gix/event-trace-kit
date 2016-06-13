namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Security.Principal;
    using System.Text;
    using System.Windows;
    using EventTraceKit.VsExtension.Controls;
    using EventTraceKit.VsExtension.Controls.Hdv;
    using Microsoft.VisualStudio.Shell.Interop;

    public sealed class GenericEventsViewModelSource
    {
        private readonly HdvColumnViewModelPreset providerIdPreset;
        private readonly HdvColumnViewModelPreset providerNamePreset;
        private readonly HdvColumnViewModelPreset idPreset;
        private readonly HdvColumnViewModelPreset versionPreset;
        private readonly HdvColumnViewModelPreset channelPreset;
        private readonly HdvColumnViewModelPreset channelNamePreset;
        private readonly HdvColumnViewModelPreset levelPreset;
        private readonly HdvColumnViewModelPreset levelNamePreset;
        private readonly HdvColumnViewModelPreset taskPreset;
        private readonly HdvColumnViewModelPreset taskNamePreset;
        private readonly HdvColumnViewModelPreset opcodeOrTypePreset;
        private readonly HdvColumnViewModelPreset opcodeNamePreset;
        private readonly HdvColumnViewModelPreset keywordPreset;
        private readonly HdvColumnViewModelPreset eventNamePreset;
        private readonly HdvColumnViewModelPreset messagePreset;
        private readonly HdvColumnViewModelPreset eventTypePreset;
        private readonly HdvColumnViewModelPreset cpuPreset;
        private readonly HdvColumnViewModelPreset processIdPreset;
        private readonly HdvColumnViewModelPreset threadIdPreset;
        private readonly HdvColumnViewModelPreset userDataLengthPreset;
        private readonly HdvColumnViewModelPreset activityIdPreset;
        private readonly HdvColumnViewModelPreset relatedActivityIdPreset;
        private readonly HdvColumnViewModelPreset userSecurityIdentifierPreset;
        private readonly HdvColumnViewModelPreset sessionIdPreset;
        private readonly HdvColumnViewModelPreset eventKeyPreset;
        private readonly HdvColumnViewModelPreset timestampGeneratorPreset;
        private readonly HdvColumnViewModelPreset datetimeGeneratorPreset;
        private readonly HdvColumnViewModelPreset modernProcessDataPreset;
        private readonly HdvColumnViewModelPreset processNamePreset;
        private readonly HdvColumnViewModelPreset stackTopPreset;
        private readonly HdvColumnViewModelPreset threadStartModulePreset;
        private readonly HdvColumnViewModelPreset threadStartFunctionPreset;

        public GenericEventsViewModelSource()
        {
            providerIdPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("9B9DAF0F-EAC6-43FE-B68F-EAF0D9A4AFB9"),
                    Name = "Provider Id",
                    IsVisible = false,
                    Width = 100
                }.EnsureFrozen();
            providerNamePreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("934D2438-65F3-4AE9-8FEA-94B81AA5A4A6"),
                    Name = "Provider Name",
                    IsVisible = true,
                    Width = 200
                }.EnsureFrozen();
            idPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("0FE03A19-FBCB-4514-9441-2D0B1AB5E2E1"),
                    Name = "Id",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            versionPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("215AB0D7-BEC9-4A70-96C4-028EE3404F09"),
                    Name = "Version",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            channelPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("CF9373E2-5876-4F84-BB3A-F6C878D36F86"),
                    Name = "Channel",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            channelNamePreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("FAC4B329-DD59-41D2-8AA8-83B66DFBAECC"),
                    Name = "Channel Name",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            levelPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("388591F3-43B2-4E68-B080-0B1A48D33559"),
                    Name = "Level",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            levelNamePreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("1B2ADB63-7C73-4330-927D-4FF37A60B249"),
                    Name = "Level Name",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            taskPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("CE90F4D8-0FDE-4324-8D39-5BF74C8F4D9B"),
                    Name = "Task",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            taskNamePreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("730765B3-2E42-43E7-8B26-BAB7F4999E69"),
                    Name = "Task Name",
                    IsVisible = true,
                    Width = 80
                }.EnsureFrozen();
            opcodeOrTypePreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("F08CCD14-FE1E-4D9E-BE6C-B527EA4B25DA"),
                    Name = "Opcode/Type ",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            opcodeNamePreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("99C0A192-174F-4DD5-AFD8-32F513506E88"),
                    Name = "Opcode Name",
                    IsVisible = true,
                    Width = 80
                }.EnsureFrozen();
            keywordPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("62DC8843-C7BF-45F0-AC61-644395D53409"),
                    Name = "Keyword",
                    IsVisible = false,
                    Width = 80,
                    TextAlignment = TextAlignment.Right,
                    CellFormat = "x"
                }.EnsureFrozen();
            messagePreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("89F731F6-D4D2-40E8-9615-6EB5A5A68A75"),
                    Name = "Message",
                    IsVisible = true,
                    Width = 100
                }.EnsureFrozen();
            eventNamePreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("B82277B9-7066-4938-A959-EABF0C689087"),
                    Name = "Event Name",
                    IsVisible = true,
                    Width = 100
                }.EnsureFrozen();
            eventTypePreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("AC2A6011-BCB3-4721-BEF1-E1DEC50C073D"),
                    Name = "Event Type",
                    IsVisible = false,
                    Width = 100
                }.EnsureFrozen();
            cpuPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("452A05E3-A1C0-4686-BB6B-C39AFF2F24BE"),
                    Name = "Cpu",
                    IsVisible = true,
                    Width = 30
                }.EnsureFrozen();
            threadIdPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("6BEB4F24-53DC-4A9D-8EEA-ED8F69990349"),
                    Name = "ThreadId",
                    IsVisible = true,
                    Width = 50
                }.EnsureFrozen();
            processIdPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("7600E8FD-D7C2-4BA4-9DE4-AADE5230DC53"),
                    Name = "Event Header ProcessId",
                    IsVisible = true,
                    Width = 50,
                    HelpText = "(0 = PID Not Found)"
                }.EnsureFrozen();
            userDataLengthPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("813F4638-8D41-4EAD-94DD-9A4AFFEFA701"),
                    Name = "UserDataLength",
                    IsVisible = false,
                    Width = 30
                }.EnsureFrozen();
            activityIdPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("21695563-AC1B-4953-9B9B-991353DBC082"),
                    Name = "etw:ActivityId",
                    IsVisible = false,
                    Width = 60
                }.EnsureFrozen();
            relatedActivityIdPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("83B1BF6F-5E8D-4143-A84B-8C16ED1EF6BD"),
                    Name = "etw:Related ActivityId",
                    IsVisible = false,
                    Width = 60
                }.EnsureFrozen();
            userSecurityIdentifierPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("F979E52D-EE1B-4A7E-950F-28103990D11B"),
                    Name = "etw:UserSid",
                    IsVisible = false,
                    Width = 60
                }.EnsureFrozen();
            sessionIdPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("84FC6D0C-5FFD-40D9-8C3B-F0EB8F8F2D1B"),
                    Name = "etw:SessionId",
                    IsVisible = false,
                    Width = 60
                }.EnsureFrozen();
            eventKeyPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("4F0679D2-B5E7-4AB1-ADF7-FCDEBEEF801B"),
                    Name = "etw:EventKey",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            timestampGeneratorPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("9C75AA69-046E-42AE-B594-B4AD24335A0A"),
                    Name = "Time",
                    IsVisible = true,
                    Width = 80,
                    TextAlignment = TextAlignment.Right,
                    CellFormat = "sN"
                }.EnsureFrozen();
            datetimeGeneratorPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("8823874B-917D-4D64-ABDF-EA29E6C87789"),
                    Name = "DateTime (Local)",
                    Width = 150,
                }.EnsureFrozen();
            modernProcessDataPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("DC7E68B0-E753-47DF-8357-61BEC093E405"),
                    Name = "Process",
                    IsVisible = true,
                    Width = 150
                }.EnsureFrozen();
            processNamePreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("BB09F706-FE79-43AA-A103-120801DAC28F"),
                    Name = "Process Name",
                    IsVisible = true,
                    Width = 150
                }.EnsureFrozen();
            stackTopPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("D55383F4-D0ED-404B-98A8-DC9CF4533FBF"),
                    Name = "Stack",
                    IsVisible = false,
                    Width = 100
                }.EnsureFrozen();
            threadStartModulePreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("D58C42B0-818D-4D83-BD99-9DA872E77B54"),
                    Name = "Thread Start Module",
                    IsVisible = false,
                    Width = 100
                }.EnsureFrozen();
            threadStartFunctionPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("125BB527-34C6-4A33-82B8-05E3B0C7A591"),
                    Name = "Thread Start Function",
                    IsVisible = false,
                    Width = 100
                }.EnsureFrozen();
        }

        public Tuple<DataTable, HdvViewModelPreset> CreateTable(
            IEventInfoSource eventInfoSource)
        {
            var table = new DataTable("Generic Events");
            var defaultPreset = new HdvViewModelPreset();
            var info = new CrimsonEventsInfo(eventInfoSource);

            AddColumn(table, defaultPreset, providerIdPreset, DataColumn.Create(info.ProjectProviderId));
            AddColumn(table, defaultPreset, providerNamePreset, DataColumn.Create(info.ProjectProviderName));
            AddColumn(table, defaultPreset, taskNamePreset, DataColumn.Create(info.ProjectTaskName));
            AddColumn(table, defaultPreset, opcodeOrTypePreset, DataColumn.Create(info.ProjectOpCode));
            AddColumn(table, defaultPreset, levelPreset, DataColumn.Create(info.ProjectLevel));
            AddColumn(table, defaultPreset, versionPreset, DataColumn.Create(info.ProjectVersion));
            AddColumn(table, defaultPreset, taskPreset, DataColumn.Create(info.ProjectTask));
            AddColumn(table, defaultPreset, keywordPreset, DataColumn.Create(info.ProjectKeyword));
            AddColumn(table, defaultPreset, channelPreset, DataColumn.Create(info.ProjectChannel));
            AddColumn(table, defaultPreset, channelNamePreset, DataColumn.Create(info.ProjectChannelName));
            AddColumn(table, defaultPreset, idPreset, DataColumn.Create(info.ProjectId));
            AddColumn(table, defaultPreset, opcodeNamePreset, DataColumn.Create(info.ProjectOpCodeName));
            AddColumn(table, defaultPreset, messagePreset, DataColumn.Create(info.ProjectMessage));
            AddColumn(table, defaultPreset, eventNamePreset, DataColumn.Create(info.ProjectEventName));
            AddColumn(table, defaultPreset, eventTypePreset, DataColumn.Create(info.ProjectEventType));
            AddColumn(table, defaultPreset, cpuPreset, DataColumn.Create(info.ProjectCpu));
            AddColumn(table, defaultPreset, threadIdPreset, DataColumn.Create(info.ProjectThreadId));
            AddColumn(table, defaultPreset, processIdPreset, DataColumn.Create(info.ProjectProcessId));
            AddColumn(table, defaultPreset, userDataLengthPreset, DataColumn.Create(info.ProjectUserDataLength));
            AddColumn(table, defaultPreset, activityIdPreset, DataColumn.Create(info.ProjectActivityId));
            AddColumn(table, defaultPreset, relatedActivityIdPreset, DataColumn.Create(info.ProjectRelatedActivityId));
            AddColumn(table, defaultPreset, userSecurityIdentifierPreset, DataColumn.Create(info.ProjectUserSecurityIdentifier));
            AddColumn(table, defaultPreset, sessionIdPreset, DataColumn.Create(info.ProjectSessionId));
            AddColumn(table, defaultPreset, eventKeyPreset, DataColumn.Create(info.ProjectEventKey));
            AddColumn(table, defaultPreset, timestampGeneratorPreset, DataColumn.Create(info.ProjectTimePoint));
            AddColumn(table, defaultPreset, datetimeGeneratorPreset, DataColumn.Create(info.ProjectDateTime));
            AddColumn(table, defaultPreset, modernProcessDataPreset, DataColumn.Create<object>());
            AddColumn(table, defaultPreset, processNamePreset, DataColumn.Create<string>());
            AddColumn(table, defaultPreset, stackTopPreset, DataColumn.Create<object>());
            AddColumn(table, defaultPreset, threadStartModulePreset, DataColumn.Create<string>());
            AddColumn(table, defaultPreset, threadStartFunctionPreset, DataColumn.Create<string>());

            return Tuple.Create(table, defaultPreset);
        }

        private void AddColumn(
            DataTable table, HdvViewModelPreset defaultPreset,
            HdvColumnViewModelPreset preset, DataColumn column)
        {
            column.Id = preset.Id;
            column.Name = preset.Name;
            column.Width = preset.Width;
            column.IsVisible = preset.IsVisible;
            column.IsResizable = true;
            column.TextAlignment = preset.TextAlignment;
            table.Add(column);
            defaultPreset.ConfigurableColumns.Add(preset);
        }

        private sealed class CrimsonEventsInfo
        {
            private readonly IEventInfoSource eventInfoSource;

            public CrimsonEventsInfo(IEventInfoSource eventInfoSource)
            {
                this.eventInfoSource = eventInfoSource;
            }

            private EventInfo GetEventInfo(int index)
            {
                return eventInfoSource.GetEvent(index);
            }

            public unsafe TraceEventInfoCPtr GetTraceEventInfo(int index)
            {
                var info = GetEventInfo(index);
                return new TraceEventInfoCPtr(
                    (TRACE_EVENT_INFO*)info.TraceEventInfo, (uint)info.TraceEventInfoSize);
                //return new TraceEventInfoCPtr(traceEventInfos[index], 0);
            }

            public unsafe EventRecordCPtr GetEventRecord(int index)
            {
                return new EventRecordCPtr(
                    (EVENT_RECORD*)GetEventInfo(index).EventRecord);
                //return new EventRecordCPtr(eventRecords[index]);
            }

            public Guid ProjectProviderId(int index)
            {
                return GetEventRecord(index).EventHeader.ProviderId;
            }

            public string ProjectProviderName(int index)
            {
                TraceEventInfoCPtr eventInfo = GetTraceEventInfo(index);
                if (eventInfo.HasValue)
                    return eventInfo.ProviderName.ToString();

                return ProjectProviderId(index).ToString();
            }

            public uint ProjectProcessId(int index)
            {
                return GetEventRecord(index).EventHeader.ProcessId;
            }

            public ushort ProjectId(int index)
            {
                return GetEventRecord(index).EventHeader.EventDescriptor.Id;
            }

            public byte ProjectVersion(int index)
            {
                return GetEventRecord(index).EventHeader.EventDescriptor.Version;
            }

            public byte ProjectChannel(int index)
            {
                return GetEventRecord(index).EventHeader.EventDescriptor.Channel;
            }

            public UnmanagedString ProjectChannelName(int index)
            {
                return GetTraceEventInfo(index).GetChannelName();
            }

            public byte ProjectLevel(int index)
            {
                return GetEventRecord(index).EventHeader.EventDescriptor.Level;
            }

            public UnmanagedString ProjectLevelName(int index)
            {
                return GetTraceEventInfo(index).GetLevelName();
            }

            public ushort ProjectTask(int index)
            {
                return GetEventRecord(index).EventHeader.EventDescriptor.Task;
            }

            public UnmanagedString ProjectTaskName(int index)
            {
                return GetTraceEventInfo(index).GetTaskName();
            }

            public byte ProjectOpCode(int index)
            {
                return GetEventRecord(index).EventHeader.EventDescriptor.Opcode;
            }

            public UnmanagedString ProjectOpCodeName(int index)
            {
                //var eventRecord = GetEventRecord(index);
                //if (eventRecord.IsTraceLoggingEvent()) {
                //    int opcode = eventRecord.EventHeader.EventDescriptor.Opcode;
                //    return this.winmetaOpcodeService.GetOpcodeName(opcode);
                //}
                return GetTraceEventInfo(index).GetOpcodeName();
            }

            public Keyword ProjectKeyword(int index)
            {
                return GetEventRecord(index).EventHeader.EventDescriptor.Keyword;
            }

            public unsafe string ProjectMessage(int index)
            {
                var info = GetEventInfo(index);

                var eventRecord = new EventRecordCPtr(
                    (EVENT_RECORD*)GetEventInfo(index).EventRecord);
                var traceEventInfo = new TraceEventInfoCPtr(
                    (TRACE_EVENT_INFO*)info.TraceEventInfo, (uint)info.TraceEventInfoSize);

                TimePoint timestamp = ProjectTimePoint(index);
                var parseTdhContext = new ParseTdhContext();
                return TdhHelper.GetMessageForEventRecord(
                    eventRecord, timestamp, traceEventInfo, parseTdhContext, CultureInfo.CurrentCulture);
            }

            public EventType ProjectEventType(int index)
            {
                if (!GetEventRecord(index).IsTraceLoggingEvent())
                    return EventType.Manifested;
                return EventType.TraceLogging;
            }

            public ushort ProjectUserDataLength(int index)
            {
                return GetEventRecord(index).UserDataLength;
            }

            public Guid ProjectActivityId(int index)
            {
                return GetEventRecord(index).EventHeader.ActivityId;
            }

            private static readonly Guid ActivityIdSentinel =
                new Guid("D733D8B0-7D18-4AEB-A3FC-8C4613BC2A40");

            public unsafe Guid ProjectRelatedActivityId(int index)
            {
                var item = GetEventRecord(index).FindExtendedData(EVENT_HEADER_EXT_TYPE.RELATED_ACTIVITYID);
                if (item == null)
                    return ActivityIdSentinel;
                return item->RelatedActivityId;
            }

            public unsafe string ProjectUserSecurityIdentifier(int index)
            {
                var item = GetEventRecord(index).FindExtendedData(EVENT_HEADER_EXT_TYPE.SID);
                if (item == null)
                    return string.Empty;

                SecurityIdentifier sid = item->UserSecurityIdentifier;
                return sid?.ToString() ?? string.Empty;
            }

            public unsafe uint ProjectSessionId(int index)
            {
                var item = GetEventRecord(index).FindExtendedData(EVENT_HEADER_EXT_TYPE.TS_ID);
                if (item == null)
                    return uint.MaxValue;
                return item->SessionId;
            }

            public unsafe ulong ProjectEventKey(int index)
            {
                var item = GetEventRecord(index).FindExtendedData(EVENT_HEADER_EXT_TYPE.EVENT_KEY);
                if (item == null)
                    return 0;
                return item->EventKey;
            }

            public uint ProjectThreadId(int index)
            {
                return GetEventRecord(index).EventHeader.ThreadId;
            }

            public TimePoint ProjectTimePoint(int index)
            {
                return GetEventRecord(index).TimePoint;
            }

            public DateTime ProjectDateTime(int index)
            {
                return DateTime.MaxValue;
            }

            public ulong ProjectCpu(int index)
            {
                return GetEventRecord(index).ProcessorIndex;
            }

            public string ProjectEventName(int index)
            {
                return null; // FIXME
            }
        }
    }

    [Flags]
    public enum FormatMessageFlags
    {
        /// <native>FORMAT_MESSAGE_IGNORE_INSERTS</native>
        IgnoreInserts = 0x00000200,

        /// <native>FORMAT_MESSAGE_FROM_STRING</native>
        FromString = 0x00000400,

        /// <native>FORMAT_MESSAGE_FROM_HMODULE</native>
        FromHmodule = 0x00000800,

        /// <native>FORMAT_MESSAGE_FROM_SYSTEM</native>
        FromSystem = 0x00001000,

        /// <native>FORMAT_MESSAGE_ARGUMENT_ARRAY</native>
        ArgumentArray = 0x00002000,

        /// <native>FORMAT_MESSAGE_MAX_WIDTH_MASK</native>
        MaxWidthMask = 0x000000FF,
    }

    internal static class TdhNativeMethods
    {
        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern uint FormatMessageW(
            [MarshalAs(UnmanagedType.U4)] FormatMessageFlags dwFlags,
            UnmanagedString lpSource,
            uint dwMessageId,
            uint dwLanguageId,
            IntPtr lpBuffer,
            uint nSize,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)]
            string[] Arguments);

        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern bool FileTimeToSystemTime(
            [In] ref System.Runtime.InteropServices.ComTypes.FILETIME lpFileTime, out SYSTEMTIME lpSystemTime);

        [DllImport("WS2_32.dll", EntryPoint = "WSAAddressToStringW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int WSAAddressToString(
            IntPtr pSockAddressStruct, uint cbSockAddressStruct, IntPtr lpProtocolInfo, IntPtr resultString, ref int cbResultStringLength);
    }

    internal static class CharCPtrUtils
    {
        public static unsafe int GetLength(char* wsz, int cchMax)
        {
            if (wsz == null)
                throw new ArgumentNullException(nameof(wsz));

            if (cchMax < 0)
                throw new ArgumentOutOfRangeException(nameof(cchMax), "must be non-negative");

            int num = 0;
            while (num < cchMax) {
                if (wsz[0] == '\0') {
                    return num;
                }
                wsz++;
                num++;
            }

            return num;
        }
    }

    public sealed class TimePointConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return (sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string str = value as string;
            if (str != null)
                return TimePoint.Parse(str.Trim());
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
                throw new ArgumentNullException(nameof(destinationType));
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    [TypeConverter(typeof(TimePointConverter))]
    public struct TimePoint
        : IComparable<TimePoint>
        , IEquatable<TimePoint>
        , IFormattable
    {
        private readonly long nanoseconds;

        public TimePoint(long nanoseconds)
        {
            this.nanoseconds = nanoseconds;
        }

        public static TimePoint FromNanoseconds(long nanoseconds)
        {
            return new TimePoint(nanoseconds);
        }

        public long ToNanoseconds => nanoseconds;
        public long ToMicroseconds => nanoseconds / 1000;
        public long ToMilliseconds => nanoseconds / 1000000;
        public long ToSeconds => nanoseconds / 1000000000;

        public static TimePoint Abs(TimePoint value)
        {
            return FromNanoseconds(Math.Abs(value.nanoseconds));
        }

        public static TimePoint Min(TimePoint lhs, TimePoint rhs)
        {
            return new TimePoint(Math.Min(lhs.ToNanoseconds, rhs.ToNanoseconds));
        }

        public static TimePoint Max(TimePoint lhs, TimePoint rhs)
        {
            return new TimePoint(Math.Max(lhs.ToNanoseconds, rhs.ToNanoseconds));
        }

        public static TimePoint Zero => new TimePoint();

        public static TimePoint MinValue => new TimePoint(-9223372036854775808L);

        public static TimePoint MaxValue => new TimePoint(9223372036854775807);

        public int CompareTo(TimePoint other)
        {
            return
                nanoseconds < other.nanoseconds ? -1 :
                nanoseconds <= other.nanoseconds ? 0 :
                1;
        }

        public bool Equals(TimePoint other)
        {
            return nanoseconds == other.nanoseconds;
        }

        public static bool operator ==(TimePoint lhs, TimePoint rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(TimePoint lhs, TimePoint rhs)
        {
            return !lhs.Equals(rhs);
        }

        public static bool operator <(TimePoint lhs, TimePoint rhs)
        {
            return lhs.CompareTo(rhs) < 0;
        }

        public static bool operator >(TimePoint lhs, TimePoint rhs)
        {
            return lhs.CompareTo(rhs) > 0;
        }

        public static bool operator <=(TimePoint lhs, TimePoint rhs)
        {
            return lhs.CompareTo(rhs) <= 0;
        }

        public static bool operator >=(TimePoint lhs, TimePoint rhs)
        {
            return lhs.CompareTo(rhs) >= 0;
        }

        public override bool Equals(object other)
        {
            return other is TimePoint && Equals((TimePoint)other);
        }

        public override int GetHashCode()
        {
            return nanoseconds.GetHashCode();
        }

        public override string ToString()
        {
            return nanoseconds.ToString("F0");
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return nanoseconds.ToString(format, formatProvider);
        }

        public static TimePoint Parse(string s)
        {
            return new TimePoint(long.Parse(s));
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Keyword
        : IEquatable<Keyword>
        , IComparable<Keyword>
    {
        public Keyword(ulong keywordValue)
        {
            KeywordValue = keywordValue;
        }

        public ulong KeywordValue { get; }

        public static Keyword Zero => new Keyword();
        public static Keyword MinValue => new Keyword(0L);
        public static Keyword MaxValue => new Keyword(ulong.MaxValue);

        public static bool operator ==(Keyword lhs, Keyword rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(Keyword lhs, Keyword rhs)
        {
            return !lhs.Equals(rhs);
        }

        public static bool operator <(Keyword lhs, Keyword rhs)
        {
            return lhs.CompareTo(rhs) < 0;
        }

        public static bool operator >(Keyword lhs, Keyword rhs)
        {
            return lhs.CompareTo(rhs) > 0;
        }

        public static bool operator <=(Keyword lhs, Keyword rhs)
        {
            return lhs.CompareTo(rhs) <= 0;
        }

        public static bool operator >=(Keyword lhs, Keyword rhs)
        {
            return lhs.CompareTo(rhs) >= 0;
        }

        public static Keyword operator &(Keyword lhs, Keyword rhs)
        {
            return new Keyword(lhs.KeywordValue & rhs.KeywordValue);
        }

        public static implicit operator Keyword(ulong keywordValue)
        {
            return new Keyword(keywordValue);
        }

        public bool Equals(Keyword other)
        {
            return KeywordValue == other.KeywordValue;
        }

        public int CompareTo(Keyword other)
        {
            return KeywordValue.CompareTo(other.KeywordValue);
        }

        public override bool Equals(object other)
        {
            return other is Keyword && Equals((Keyword)other);
        }

        public override int GetHashCode()
        {
            return KeywordValue.GetHashCode();
        }

        public override string ToString()
        {
            return KeywordValue.ToString(CultureInfo.InvariantCulture);
        }
    }
}
