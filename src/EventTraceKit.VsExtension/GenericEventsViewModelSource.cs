namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Security.Principal;
    using System.Threading;
    using System.Windows;
    using Controls;
    using Windows;

    public sealed class GenericEventsViewModelSource
    {
        private readonly ColumnViewModelPreset providerIdPreset;
        private readonly ColumnViewModelPreset providerNamePreset;
        private readonly ColumnViewModelPreset idPreset;
        private readonly ColumnViewModelPreset versionPreset;
        private readonly ColumnViewModelPreset channelPreset;
        private readonly ColumnViewModelPreset channelNamePreset;
        private readonly ColumnViewModelPreset levelPreset;
        private readonly ColumnViewModelPreset levelNamePreset;
        private readonly ColumnViewModelPreset taskPreset;
        private readonly ColumnViewModelPreset taskNamePreset;
        private readonly ColumnViewModelPreset opcodeOrTypePreset;
        private readonly ColumnViewModelPreset opcodeNamePreset;
        private readonly ColumnViewModelPreset keywordPreset;
        private readonly ColumnViewModelPreset eventNamePreset;
        private readonly ColumnViewModelPreset messagePreset;
        private readonly ColumnViewModelPreset eventTypePreset;
        private readonly ColumnViewModelPreset symbolPreset;
        private readonly ColumnViewModelPreset cpuPreset;
        private readonly ColumnViewModelPreset processIdPreset;
        private readonly ColumnViewModelPreset threadIdPreset;
        private readonly ColumnViewModelPreset userDataLengthPreset;
        private readonly ColumnViewModelPreset activityIdPreset;
        private readonly ColumnViewModelPreset relatedActivityIdPreset;
        private readonly ColumnViewModelPreset userSecurityIdentifierPreset;
        private readonly ColumnViewModelPreset sessionIdPreset;
        private readonly ColumnViewModelPreset eventKeyPreset;
        private readonly ColumnViewModelPreset timePointGeneratorPreset;
        private readonly ColumnViewModelPreset timeAbsoluteGeneratorPreset;
        private readonly ColumnViewModelPreset timeRelativeGeneratorPreset;
        private readonly ColumnViewModelPreset decodingSourcePreset;
        private readonly ColumnViewModelPreset modernProcessDataPreset;
        private readonly ColumnViewModelPreset processNamePreset;
        private readonly ColumnViewModelPreset stackTopPreset;
        private readonly ColumnViewModelPreset threadStartModulePreset;
        private readonly ColumnViewModelPreset threadStartFunctionPreset;

        public GenericEventsViewModelSource()
        {
            providerIdPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("9B9DAF0F-EAC6-43FE-B68F-EAF0D9A4AFB9"),
                    Name = "Provider Id",
                    IsVisible = false,
                    Width = 100
                }.EnsureFrozen();
            providerNamePreset =
                new ColumnViewModelPreset {
                    Id = new Guid("934D2438-65F3-4AE9-8FEA-94B81AA5A4A6"),
                    Name = "Provider",
                    IsVisible = true,
                    Width = 150
                }.EnsureFrozen();
            idPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("0FE03A19-FBCB-4514-9441-2D0B1AB5E2E1"),
                    Name = "Id",
                    IsVisible = true,
                    Width = 50
                }.EnsureFrozen();
            versionPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("215AB0D7-BEC9-4A70-96C4-028EE3404F09"),
                    Name = "Version",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            channelPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("CF9373E2-5876-4F84-BB3A-F6C878D36F86"),
                    Name = "Channel",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            channelNamePreset =
                new ColumnViewModelPreset {
                    Id = new Guid("FAC4B329-DD59-41D2-8AA8-83B66DFBAECC"),
                    Name = "Channel Name",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            levelPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("388591F3-43B2-4E68-B080-0B1A48D33559"),
                    Name = "Level",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            levelNamePreset =
                new ColumnViewModelPreset {
                    Id = new Guid("1B2ADB63-7C73-4330-927D-4FF37A60B249"),
                    Name = "Level Name",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            taskPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("CE90F4D8-0FDE-4324-8D39-5BF74C8F4D9B"),
                    Name = "Task",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            taskNamePreset =
                new ColumnViewModelPreset {
                    Id = new Guid("730765B3-2E42-43E7-8B26-BAB7F4999E69"),
                    Name = "Task Name",
                    IsVisible = true,
                    Width = 80
                }.EnsureFrozen();
            opcodeOrTypePreset =
                new ColumnViewModelPreset {
                    Id = new Guid("F08CCD14-FE1E-4D9E-BE6C-B527EA4B25DA"),
                    Name = "Opcode/Type",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            opcodeNamePreset =
                new ColumnViewModelPreset {
                    Id = new Guid("99C0A192-174F-4DD5-AFD8-32F513506E88"),
                    Name = "Opcode Name",
                    IsVisible = true,
                    Width = 80
                }.EnsureFrozen();
            keywordPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("62DC8843-C7BF-45F0-AC61-644395D53409"),
                    Name = "Keyword",
                    IsVisible = false,
                    Width = 80,
                    TextAlignment = TextAlignment.Right,
                    CellFormat = "x"
                }.EnsureFrozen();
            messagePreset =
                new ColumnViewModelPreset {
                    Id = new Guid("89F731F6-D4D2-40E8-9615-6EB5A5A68A75"),
                    Name = "Message",
                    IsVisible = true,
                    Width = 500
                }.EnsureFrozen();
            eventNamePreset =
                new ColumnViewModelPreset {
                    Id = new Guid("B82277B9-7066-4938-A959-EABF0C689087"),
                    Name = "Event Name",
                    IsVisible = false,
                    Width = 100
                }.EnsureFrozen();
            eventTypePreset =
                new ColumnViewModelPreset {
                    Id = new Guid("AC2A6011-BCB3-4721-BEF1-E1DEC50C073D"),
                    Name = "Event Type",
                    IsVisible = false,
                    Width = 100
                }.EnsureFrozen();
            symbolPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("79423887-739E-4DFF-9045-3DCF243E2922"),
                    Name = "Symbol",
                    IsVisible = false,
                    Width = 100
                }.EnsureFrozen();
            cpuPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("452A05E3-A1C0-4686-BB6B-C39AFF2F24BE"),
                    Name = "CPU",
                    IsVisible = false,
                    Width = 30
                }.EnsureFrozen();
            threadIdPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("6BEB4F24-53DC-4A9D-8EEA-ED8F69990349"),
                    Name = "TID",
                    IsVisible = true,
                    Width = 40,
                    HelpText = "Thread ID"
                }.EnsureFrozen();
            processIdPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("7600E8FD-D7C2-4BA4-9DE4-AADE5230DC53"),
                    Name = "PID",
                    IsVisible = true,
                    Width = 40,
                    HelpText = "Process ID (0 = PID Not Found)"
                }.EnsureFrozen();
            userDataLengthPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("813F4638-8D41-4EAD-94DD-9A4AFFEFA701"),
                    Name = "UserData (Bytes)",
                    IsVisible = false,
                    Width = 30
                }.EnsureFrozen();
            activityIdPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("21695563-AC1B-4953-9B9B-991353DBC082"),
                    Name = "etw:ActivityId",
                    IsVisible = false,
                    Width = 60
                }.EnsureFrozen();
            relatedActivityIdPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("83B1BF6F-5E8D-4143-A84B-8C16ED1EF6BD"),
                    Name = "etw:Related ActivityId",
                    IsVisible = false,
                    Width = 60
                }.EnsureFrozen();
            userSecurityIdentifierPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("F979E52D-EE1B-4A7E-950F-28103990D11B"),
                    Name = "etw:UserSid",
                    IsVisible = false,
                    Width = 60
                }.EnsureFrozen();
            sessionIdPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("84FC6D0C-5FFD-40D9-8C3B-F0EB8F8F2D1B"),
                    Name = "etw:SessionId",
                    IsVisible = false,
                    Width = 60
                }.EnsureFrozen();
            eventKeyPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("4F0679D2-B5E7-4AB1-ADF7-FCDEBEEF801B"),
                    Name = "etw:EventKey",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            timePointGeneratorPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("9C75AA69-046E-42AE-B594-B4AD24335A0A"),
                    Name = "Time (Raw)",
                    IsVisible = false,
                    Width = 145,
                    TextAlignment = TextAlignment.Right,
                    CellFormat = TimePointFormatter.FormatSecondsGrouped
                }.EnsureFrozen();
            timeAbsoluteGeneratorPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("FC87155E-AD2A-4294-A425-55E914FA1821"),
                    Name = "Time",
                    IsVisible = false,
                    Width = 100,
                    CellFormat = "HH:mm:ss.fffffff"
                }.EnsureFrozen();
            timeRelativeGeneratorPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("8823874B-917D-4D64-ABDF-EA29E6C87789"),
                    Name = "Time Elapsed",
                    IsVisible = true,
                    Width = 100,
                    CellFormat = "HH:mm:ss.fffffff"
                }.EnsureFrozen();
            decodingSourcePreset =
                new ColumnViewModelPreset {
                    Id = new Guid("D06EBF0F-A744-4E27-B635-F2E4A56B9B50"),
                    Name = "Decoding Source",
                    Width = 150,
                }.EnsureFrozen();
            modernProcessDataPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("DC7E68B0-E753-47DF-8357-61BEC093E405"),
                    Name = "Process",
                    IsVisible = true,
                    Width = 150
                }.EnsureFrozen();
            processNamePreset =
                new ColumnViewModelPreset {
                    Id = new Guid("BB09F706-FE79-43AA-A103-120801DAC28F"),
                    Name = "Process Name",
                    IsVisible = true,
                    Width = 150
                }.EnsureFrozen();
            stackTopPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("D55383F4-D0ED-404B-98A8-DC9CF4533FBF"),
                    Name = "Stack",
                    IsVisible = false,
                    Width = 100
                }.EnsureFrozen();
            threadStartModulePreset =
                new ColumnViewModelPreset {
                    Id = new Guid("D58C42B0-818D-4D83-BD99-9DA872E77B54"),
                    Name = "Thread Start Module",
                    IsVisible = false,
                    Width = 100
                }.EnsureFrozen();
            threadStartFunctionPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("125BB527-34C6-4A33-82B8-05E3B0C7A591"),
                    Name = "Thread Start Function",
                    IsVisible = false,
                    Width = 100
                }.EnsureFrozen();
        }

        public static AsyncDataViewModelPreset CreateDefaultPreset()
        {
            var providerIdPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("9B9DAF0F-EAC6-43FE-B68F-EAF0D9A4AFB9"),
                    Name = "Provider Id",
                    IsVisible = false,
                    Width = 100
                }.EnsureFrozen();
            var providerNamePreset =
                new ColumnViewModelPreset {
                    Id = new Guid("934D2438-65F3-4AE9-8FEA-94B81AA5A4A6"),
                    Name = "Provider",
                    IsVisible = true,
                    Width = 150
                }.EnsureFrozen();
            var idPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("0FE03A19-FBCB-4514-9441-2D0B1AB5E2E1"),
                    Name = "Id",
                    IsVisible = true,
                    Width = 50
                }.EnsureFrozen();
            var versionPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("215AB0D7-BEC9-4A70-96C4-028EE3404F09"),
                    Name = "Version",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            var channelPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("CF9373E2-5876-4F84-BB3A-F6C878D36F86"),
                    Name = "Channel",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            var channelNamePreset =
                new ColumnViewModelPreset {
                    Id = new Guid("FAC4B329-DD59-41D2-8AA8-83B66DFBAECC"),
                    Name = "Channel Name",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            var levelPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("388591F3-43B2-4E68-B080-0B1A48D33559"),
                    Name = "Level",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            var levelNamePreset =
                new ColumnViewModelPreset {
                    Id = new Guid("1B2ADB63-7C73-4330-927D-4FF37A60B249"),
                    Name = "Level Name",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            var taskPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("CE90F4D8-0FDE-4324-8D39-5BF74C8F4D9B"),
                    Name = "Task",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            var taskNamePreset =
                new ColumnViewModelPreset {
                    Id = new Guid("730765B3-2E42-43E7-8B26-BAB7F4999E69"),
                    Name = "Task Name",
                    IsVisible = true,
                    Width = 80
                }.EnsureFrozen();
            var opcodeOrTypePreset =
                new ColumnViewModelPreset {
                    Id = new Guid("F08CCD14-FE1E-4D9E-BE6C-B527EA4B25DA"),
                    Name = "Opcode/Type",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            var opcodeNamePreset =
                new ColumnViewModelPreset {
                    Id = new Guid("99C0A192-174F-4DD5-AFD8-32F513506E88"),
                    Name = "Opcode Name",
                    IsVisible = true,
                    Width = 80
                }.EnsureFrozen();
            var keywordPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("62DC8843-C7BF-45F0-AC61-644395D53409"),
                    Name = "Keyword",
                    IsVisible = false,
                    Width = 80,
                    TextAlignment = TextAlignment.Right,
                    CellFormat = "x"
                }.EnsureFrozen();
            var messagePreset =
                new ColumnViewModelPreset {
                    Id = new Guid("89F731F6-D4D2-40E8-9615-6EB5A5A68A75"),
                    Name = "Message",
                    IsVisible = true,
                    Width = 500
                }.EnsureFrozen();
            var processIdPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("7600E8FD-D7C2-4BA4-9DE4-AADE5230DC53"),
                    Name = "PID",
                    IsVisible = true,
                    Width = 40,
                    HelpText = "Process ID (0 = PID Not Found)"
                }.EnsureFrozen();
            var threadIdPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("6BEB4F24-53DC-4A9D-8EEA-ED8F69990349"),
                    Name = "TID",
                    IsVisible = true,
                    Width = 40,
                    HelpText = "Thread ID"
                }.EnsureFrozen();
            var timeAbsoluteGeneratorPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("FC87155E-AD2A-4294-A425-55E914FA1821"),
                    Name = "Time",
                    IsVisible = false,
                    Width = 100,
                    CellFormat = "HH:mm:ss.fffffff"
                }.EnsureFrozen();
            var timeRelativeGeneratorPreset =
                new ColumnViewModelPreset {
                    Id = new Guid("8823874B-917D-4D64-ABDF-EA29E6C87789"),
                    Name = "Time Elapsed",
                    IsVisible = true,
                    Width = 100,
                    CellFormat = "HH:mm:ss.fffffff"
                }.EnsureFrozen();

            var preset = new AsyncDataViewModelPreset();
            preset.Name = "Default";
            preset.ConfigurableColumns.Add(timeAbsoluteGeneratorPreset);
            preset.ConfigurableColumns.Add(timeRelativeGeneratorPreset);
            preset.ConfigurableColumns.Add(providerIdPreset);
            preset.ConfigurableColumns.Add(providerNamePreset);
            preset.ConfigurableColumns.Add(idPreset);
            preset.ConfigurableColumns.Add(versionPreset);
            preset.ConfigurableColumns.Add(channelPreset);
            preset.ConfigurableColumns.Add(channelNamePreset);
            preset.ConfigurableColumns.Add(taskPreset);
            preset.ConfigurableColumns.Add(taskNamePreset);
            preset.ConfigurableColumns.Add(opcodeNamePreset);
            preset.ConfigurableColumns.Add(opcodeOrTypePreset);
            preset.ConfigurableColumns.Add(levelPreset);
            preset.ConfigurableColumns.Add(levelNamePreset);
            preset.ConfigurableColumns.Add(keywordPreset);
            preset.ConfigurableColumns.Add(processIdPreset);
            preset.ConfigurableColumns.Add(threadIdPreset);
            preset.ConfigurableColumns.Add(messagePreset);
            preset.Freeze();

            return preset;
        }

        public Tuple<DataTable, AsyncDataViewModelPreset> CreateTable(
            IEventInfoSource eventInfoSource, EventSymbolSource symbolSource)
        {
            var table = new DataTable("Generic Events");
            var templatePreset = new AsyncDataViewModelPreset();
            var formatter = new NativeTdhFormatter();
            var info = new CrimsonEventsInfo(eventInfoSource, formatter, symbolSource);

            AddColumn(table, templatePreset, timePointGeneratorPreset, DataColumn.Create(info.ProjectTimePoint));
            AddColumn(table, templatePreset, timeAbsoluteGeneratorPreset, DataColumn.Create(info.ProjectTimeAbsolute));
            AddColumn(table, templatePreset, timeRelativeGeneratorPreset, DataColumn.Create(info.ProjectTimeRelative));
            AddColumn(table, templatePreset, providerIdPreset, DataColumn.Create(info.ProjectProviderId));
            AddColumn(table, templatePreset, providerNamePreset, DataColumn.Create(info.ProjectProviderName));
            AddColumn(table, templatePreset, idPreset, DataColumn.Create(info.ProjectId));
            AddColumn(table, templatePreset, versionPreset, DataColumn.Create(info.ProjectVersion));
            AddColumn(table, templatePreset, symbolPreset, DataColumn.Create(info.ProjectSymbol));
            AddColumn(table, templatePreset, channelPreset, DataColumn.Create(info.ProjectChannel));
            AddColumn(table, templatePreset, channelNamePreset, DataColumn.Create(info.ProjectChannelName));
            AddColumn(table, templatePreset, taskPreset, DataColumn.Create(info.ProjectTask));
            AddColumn(table, templatePreset, taskNamePreset, DataColumn.Create(info.ProjectTaskName));
            AddColumn(table, templatePreset, opcodeNamePreset, DataColumn.Create(info.ProjectOpCodeName));
            AddColumn(table, templatePreset, opcodeOrTypePreset, DataColumn.Create(info.ProjectOpCode));
            AddColumn(table, templatePreset, levelPreset, DataColumn.Create(info.ProjectLevel));
            AddColumn(table, templatePreset, levelNamePreset, DataColumn.Create(info.ProjectLevelName));
            AddColumn(table, templatePreset, keywordPreset, DataColumn.Create(info.ProjectKeyword));
            AddColumn(table, templatePreset, processIdPreset, DataColumn.Create(info.ProjectProcessId));
            AddColumn(table, templatePreset, threadIdPreset, DataColumn.Create(info.ProjectThreadId));
            AddColumn(table, templatePreset, messagePreset, DataColumn.Create(info.ProjectMessage));
            AddColumn(table, templatePreset, eventNamePreset, DataColumn.Create(info.ProjectEventName));
            AddColumn(table, templatePreset, eventTypePreset, DataColumn.Create(info.ProjectEventType));
            AddColumn(table, templatePreset, cpuPreset, DataColumn.Create(info.ProjectCpu));
            AddColumn(table, templatePreset, userDataLengthPreset, DataColumn.Create(info.ProjectUserDataLength));
            AddColumn(table, templatePreset, activityIdPreset, DataColumn.Create(info.ProjectActivityId));
            AddColumn(table, templatePreset, relatedActivityIdPreset, DataColumn.Create(info.ProjectRelatedActivityId));
            AddColumn(table, templatePreset, userSecurityIdentifierPreset, DataColumn.Create(info.ProjectUserSecurityIdentifier));
            AddColumn(table, templatePreset, sessionIdPreset, DataColumn.Create(info.ProjectSessionId));
            AddColumn(table, templatePreset, eventKeyPreset, DataColumn.Create(info.ProjectEventKey));
            AddColumn(table, templatePreset, decodingSourcePreset, DataColumn.Create(info.ProjectDecodingSource));
            //AddColumn(table, templatePreset, modernProcessDataPreset, DataColumn.Create<object>());
            //AddColumn(table, templatePreset, processNamePreset, DataColumn.Create<string>());
            //AddColumn(table, templatePreset, stackTopPreset, DataColumn.Create<object>());
            //AddColumn(table, templatePreset, threadStartModulePreset, DataColumn.Create<string>());
            //AddColumn(table, templatePreset, threadStartFunctionPreset, DataColumn.Create<string>());

            return Tuple.Create(table, templatePreset);
        }

        private void AddColumn(
            DataTable table, AsyncDataViewModelPreset templatePreset,
            ColumnViewModelPreset preset, DataColumn column)
        {
            column.Id = preset.Id;
            column.Name = preset.Name;
            column.Width = preset.Width;
            column.IsVisible = preset.IsVisible;
            column.IsResizable = true;
            column.TextAlignment = preset.TextAlignment;
            table.Add(column);
            templatePreset.ConfigurableColumns.Add(preset);
        }

        private sealed class CrimsonEventsInfo
        {
            private static readonly Guid ActivityIdSentinel =
                new Guid("D733D8B0-7D18-4AEB-A3FC-8C4613BC2A40");

            private readonly IEventInfoSource eventInfoSource;
            private readonly IMessageFormatter messageFormatter;
            private readonly EventSymbolSource eventSymbolSource;

            private readonly ParseTdhContext tdhContext = new ParseTdhContext();

            public CrimsonEventsInfo(
                IEventInfoSource eventInfoSource, IMessageFormatter messageFormatter,
                EventSymbolSource eventSymbolSource)
            {
                this.eventInfoSource = eventInfoSource;
                this.messageFormatter = messageFormatter;
                this.eventSymbolSource = eventSymbolSource;
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

            public string ProjectMessage(int index)
            {
                var info = GetEventInfo(index);

                return messageFormatter.GetMessageForEvent(
                    info, tdhContext, CultureInfo.CurrentCulture);
            }

            public EventType ProjectEventType(int index)
            {
                var record = GetEventRecord(index);
                if (record.IsClassicEvent())
                    return EventType.Classic;
                if (record.IsTraceLoggingEvent())
                    return EventType.TraceLogging;
                return EventType.Manifested;
            }

            public string ProjectSymbol(int index)
            {
                var info = GetTraceEventInfo(index);
                var key = new EventKey(info.ProviderId, info.Id, info.Version);
                return eventSymbolSource.TryGetSymbol(key);
            }

            public ushort ProjectUserDataLength(int index)
            {
                return GetEventRecord(index).UserDataLength;
            }

            public Guid ProjectActivityId(int index)
            {
                return GetEventRecord(index).EventHeader.ActivityId;
            }

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

            private TimePoint GetStartTime()
            {
                var sessionInfo = eventInfoSource.GetInfo();
                if (sessionInfo.StartTime != 0)
                    return new TimePoint(sessionInfo.StartTime);

                return GetEventRecord(0).TimePoint;
            }

            public DateTime ProjectTimeAbsolute(int index)
            {
                var timePoint = GetEventRecord(index).TimePoint;
                return new DateTime(timePoint.Ticks, DateTimeKind.Utc).ToLocalTime();
            }

            public DateTime ProjectTimeRelative(int index)
            {
                var startTime = GetStartTime();
                var time = GetEventRecord(index).TimePoint;
                var elapsedTicks = time.Ticks - startTime.Ticks;
                return new DateTime(elapsedTicks, DateTimeKind.Unspecified);
            }

            public ulong ProjectCpu(int index)
            {
                return GetEventRecord(index).ProcessorIndex;
            }

            public string ProjectEventName(int index)
            {
                return null; // FIXME
            }

            public DECODING_SOURCE ProjectDecodingSource(int index)
            {
                return GetTraceEventInfo(index).DecodingSource;
            }
        }
    }

    public struct EventKey : IEquatable<EventKey>
    {
        public EventKey(Guid providerId, ushort id, byte version)
        {
            ProviderId = providerId;
            EventIdAndVersion = (uint)(id << 16) | version;
        }

        public Guid ProviderId { get; }
        public uint EventIdAndVersion { get; }

        public bool Equals(EventKey other)
        {
            return ProviderId.Equals(other.ProviderId) && EventIdAndVersion == other.EventIdAndVersion;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is EventKey && Equals((EventKey)obj);
        }

        public override int GetHashCode()
        {
            unchecked {
                return (ProviderId.GetHashCode() * 397) ^ (int)EventIdAndVersion;
            }
        }

        public static bool operator ==(EventKey left, EventKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EventKey left, EventKey right)
        {
            return !left.Equals(right);
        }
    }

    public class EventSymbolSource
    {
        private Dictionary<EventKey, string> symbols = new Dictionary<EventKey, string>();

        public string TryGetSymbol(EventKey eventKey)
        {
            string symbol;
            symbols.TryGetValue(eventKey, out symbol);
            return symbol;
        }

        public void Update(Dictionary<EventKey, string> newSymbols)
        {
            Interlocked.Exchange(ref symbols, newSymbols);
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
