namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Security.Principal;
    using System.Text;
    using System.Windows;
    using EventTraceKit.VsExtension.Controls;
    using EventTraceKit.VsExtension.Windows;
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
        private readonly HdvColumnViewModelPreset timePointGeneratorPreset;
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
                    Name = "Opcode/Type",
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
                    Name = "ProcessId",
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
            timePointGeneratorPreset =
                new HdvColumnViewModelPreset {
                    Id = new Guid("9C75AA69-046E-42AE-B594-B4AD24335A0A"),
                    Name = "Time",
                    IsVisible = true,
                    Width = 80,
                    TextAlignment = TextAlignment.Right,
                    CellFormat = TimePointFormatter.FormatSecondsGrouped
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
            var formatter = new NativeTdhFormatter();
            var info = new CrimsonEventsInfo(eventInfoSource, formatter);

            AddColumn(table, defaultPreset, timePointGeneratorPreset, DataColumn.Create(info.ProjectTimePoint));
            AddColumn(table, defaultPreset, datetimeGeneratorPreset, DataColumn.Create(info.ProjectDateTime));
            AddColumn(table, defaultPreset, providerIdPreset, DataColumn.Create(info.ProjectProviderId));
            AddColumn(table, defaultPreset, providerNamePreset, DataColumn.Create(info.ProjectProviderName));
            AddColumn(table, defaultPreset, idPreset, DataColumn.Create(info.ProjectId));
            AddColumn(table, defaultPreset, versionPreset, DataColumn.Create(info.ProjectVersion));
            AddColumn(table, defaultPreset, channelPreset, DataColumn.Create(info.ProjectChannel));
            AddColumn(table, defaultPreset, channelNamePreset, DataColumn.Create(info.ProjectChannelName));
            AddColumn(table, defaultPreset, taskPreset, DataColumn.Create(info.ProjectTask));
            AddColumn(table, defaultPreset, taskNamePreset, DataColumn.Create(info.ProjectTaskName));
            AddColumn(table, defaultPreset, opcodeNamePreset, DataColumn.Create(info.ProjectOpCodeName));
            AddColumn(table, defaultPreset, opcodeOrTypePreset, DataColumn.Create(info.ProjectOpCode));
            AddColumn(table, defaultPreset, levelPreset, DataColumn.Create(info.ProjectLevel));
            AddColumn(table, defaultPreset, levelNamePreset, DataColumn.Create(info.ProjectLevelName));
            AddColumn(table, defaultPreset, keywordPreset, DataColumn.Create(info.ProjectKeyword));
            AddColumn(table, defaultPreset, processIdPreset, DataColumn.Create(info.ProjectProcessId));
            AddColumn(table, defaultPreset, threadIdPreset, DataColumn.Create(info.ProjectThreadId));
            AddColumn(table, defaultPreset, messagePreset, DataColumn.Create(info.ProjectMessage));
            AddColumn(table, defaultPreset, eventNamePreset, DataColumn.Create(info.ProjectEventName));
            AddColumn(table, defaultPreset, eventTypePreset, DataColumn.Create(info.ProjectEventType));
            AddColumn(table, defaultPreset, cpuPreset, DataColumn.Create(info.ProjectCpu));
            AddColumn(table, defaultPreset, userDataLengthPreset, DataColumn.Create(info.ProjectUserDataLength));
            AddColumn(table, defaultPreset, activityIdPreset, DataColumn.Create(info.ProjectActivityId));
            AddColumn(table, defaultPreset, relatedActivityIdPreset, DataColumn.Create(info.ProjectRelatedActivityId));
            AddColumn(table, defaultPreset, userSecurityIdentifierPreset, DataColumn.Create(info.ProjectUserSecurityIdentifier));
            AddColumn(table, defaultPreset, sessionIdPreset, DataColumn.Create(info.ProjectSessionId));
            AddColumn(table, defaultPreset, eventKeyPreset, DataColumn.Create(info.ProjectEventKey));
            //AddColumn(table, defaultPreset, modernProcessDataPreset, DataColumn.Create<object>());
            //AddColumn(table, defaultPreset, processNamePreset, DataColumn.Create<string>());
            //AddColumn(table, defaultPreset, stackTopPreset, DataColumn.Create<object>());
            //AddColumn(table, defaultPreset, threadStartModulePreset, DataColumn.Create<string>());
            //AddColumn(table, defaultPreset, threadStartFunctionPreset, DataColumn.Create<string>());

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
            private readonly IMessageFormatter messageFormatter;

            public CrimsonEventsInfo(
                IEventInfoSource eventInfoSource, IMessageFormatter messageFormatter)
            {
                this.eventInfoSource = eventInfoSource;
                this.messageFormatter = messageFormatter;
            }

            private EventInfo GetEventInfo(int index)
            {
                return eventInfoSource.GetEvent(index);
            }

            public unsafe EventRecordCPtr GetEventRecord(int index)
            {
                return new EventRecordCPtr(
                    (EVENT_RECORD*)GetEventInfo(index).EventRecord);
            }

            public unsafe TraceEventInfoCPtr GetTraceEventInfo(int index)
            {
                var info = GetEventInfo(index);
                return new TraceEventInfoCPtr(
                    (TRACE_EVENT_INFO*)info.TraceEventInfo, (uint)info.TraceEventInfoSize);
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

                return messageFormatter.GetMessageForEvent(
                    info, tdhContext, CultureInfo.CurrentCulture);
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

            private readonly ParseTdhContext tdhContext = new ParseTdhContext();

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
                var sessionInfo = eventInfoSource.GetInfo();
                var startTime = new TimePoint(sessionInfo.StartTime);
                var time = GetEventRecord(index).TimePoint;
                return new TimePoint(time.ToNanoseconds - startTime.ToNanoseconds);
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

    public class ManagedTdhFormatter : IMessageFormatter
    {
        public unsafe string GetMessageForEvent(
            EventInfo eventInfo,
            ParseTdhContext parseTdhContext,
            IFormatProvider formatProvider)
        {
            var eventRecord = new EventRecordCPtr(
                (EVENT_RECORD*)eventInfo.EventRecord);

            var traceEventInfo = new TraceEventInfoCPtr(
                (TRACE_EVENT_INFO*)eventInfo.TraceEventInfo,
                (uint)eventInfo.TraceEventInfoSize);

            return TdhHelper.GetMessageForEventRecord(
                eventRecord, traceEventInfo, parseTdhContext, formatProvider);
        }
    }

    public class NativeTdhFormatter : IMessageFormatter
    {
        private readonly EtwMessageFormatter formatter = new EtwMessageFormatter();

        public unsafe string GetMessageForEvent(
            EventInfo eventInfo,
            ParseTdhContext parseTdhContext,
            IFormatProvider formatProvider)
        {
            return formatter.FormatEventMessage(
                (void*)eventInfo.EventRecord,
                (void*)eventInfo.TraceEventInfo,
                (uint)eventInfo.TraceEventInfoSize,
                (uint)parseTdhContext.NativePointerSize);
        }
    }
}
