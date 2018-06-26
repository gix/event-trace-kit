namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Security.Principal;
    using System.Text;
    using System.Windows;
    using EventTraceKit.VsExtension.Controls;
    using EventTraceKit.VsExtension.Extensions;
    using EventTraceKit.VsExtension.Formatting;
    using EventTraceKit.VsExtension.Native;
    using EventTraceKit.VsExtension.Utilities;
    using EventTraceKit.VsExtension.Windows;

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
        private readonly ColumnViewModelPreset keywordNamePreset;
        private readonly ColumnViewModelPreset eventNamePreset;
        private readonly ColumnViewModelPreset messagePreset;
        private readonly ColumnViewModelPreset eventTypePreset;
        private readonly ColumnViewModelPreset symbolPreset;
        private readonly ColumnViewModelPreset cpuPreset;
        private readonly ColumnViewModelPreset processIdPreset;
        private readonly ColumnViewModelPreset threadIdPreset;
        private readonly ColumnViewModelPreset userDataPreset;
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

        public static readonly Guid ProviderIdColumnId = new Guid(0x9B9DAF0F, 0xEAC6, 0x43FE, 0xB6, 0x8F, 0xEA, 0xF0, 0xD9, 0xA4, 0xAF, 0xB9);
        public static readonly Guid ProviderNameColumnId = new Guid(0x934D2438, 0x65F3, 0x4AE9, 0x8F, 0xEA, 0x94, 0xB8, 0x1A, 0xA5, 0xA4, 0xA6);
        public static readonly Guid IdColumnId = new Guid(0x0FE03A19, 0xFBCB, 0x4514, 0x94, 0x41, 0x2D, 0x0B, 0x1A, 0xB5, 0xE2, 0xE1);
        public static readonly Guid VersionColumnId = new Guid(0x215AB0D7, 0xBEC9, 0x4A70, 0x96, 0xC4, 0x02, 0x8E, 0xE3, 0x40, 0x4F, 0x09);
        public static readonly Guid ChannelColumnId = new Guid(0xCF9373E2, 0x5876, 0x4F84, 0xBB, 0x3A, 0xF6, 0xC8, 0x78, 0xD3, 0x6F, 0x86);
        public static readonly Guid ChannelNameColumnId = new Guid(0xFAC4B329, 0xDD59, 0x41D2, 0x8A, 0xA8, 0x83, 0xB6, 0x6D, 0xFB, 0xAE, 0xCC);
        public static readonly Guid LevelColumnId = new Guid(0x388591F3, 0x43B2, 0x4E68, 0xB0, 0x80, 0x0B, 0x1A, 0x48, 0xD3, 0x35, 0x59);
        public static readonly Guid LevelNameColumnId = new Guid(0x1B2ADB63, 0x7C73, 0x4330, 0x92, 0x7D, 0x4F, 0xF3, 0x7A, 0x60, 0xB2, 0x49);
        public static readonly Guid TaskColumnId = new Guid(0xCE90F4D8, 0x0FDE, 0x4324, 0x8D, 0x39, 0x5B, 0xF7, 0x4C, 0x8F, 0x4D, 0x9B);
        public static readonly Guid TaskNameColumnId = new Guid(0x730765B3, 0x2E42, 0x43E7, 0x8B, 0x26, 0xBA, 0xB7, 0xF4, 0x99, 0x9E, 0x69);
        public static readonly Guid OpcodeColumnId = new Guid(0xF08CCD14, 0xFE1E, 0x4D9E, 0xBE, 0x6C, 0xB5, 0x27, 0xEA, 0x4B, 0x25, 0xDA);
        public static readonly Guid OpcodeNameColumnId = new Guid(0x99C0A192, 0x174F, 0x4DD5, 0xAF, 0xD8, 0x32, 0xF5, 0x13, 0x50, 0x6E, 0x88);
        public static readonly Guid KeywordColumnId = new Guid(0x62DC8843, 0xC7BF, 0x45F0, 0xAC, 0x61, 0x64, 0x43, 0x95, 0xD5, 0x34, 0x09);
        public static readonly Guid KeywordNameColumnId = new Guid(0x0E603B2B, 0x2443, 0x45FF, 0x9E, 0x72, 0x76, 0x79, 0x81, 0x56, 0x26, 0x62);
        public static readonly Guid ThreadIdColumnId = new Guid(0x6BEB4F24, 0x53DC, 0x4A9D, 0x8E, 0xEA, 0xED, 0x8F, 0x69, 0x99, 0x03, 0x49);
        public static readonly Guid ProcessIdColumnId = new Guid(0x7600E8FD, 0xD7C2, 0x4BA4, 0x9D, 0xE4, 0xAA, 0xDE, 0x52, 0x30, 0xDC, 0x53);
        public static readonly Guid DecodingSourceColumnId = new Guid(0xD06EBF0F, 0xA744, 0x4E27, 0xB6, 0x35, 0xF2, 0xE4, 0xA5, 0x6B, 0x9B, 0x50);
        public static readonly Guid MessageColumnId = new Guid(0x89F731F6, 0xD4D2, 0x40E8, 0x96, 0x15, 0x6E, 0xB5, 0xA5, 0xA6, 0x8A, 0x75);
        public static readonly Guid TimeAbsoluteColumnId = new Guid(0xFC87155E, 0xAD2A, 0x4294, 0xA4, 0x25, 0x55, 0xE9, 0x14, 0xFA, 0x18, 0x21);
        public static readonly Guid TimeRelativeColumnId = new Guid(0x8823874B, 0x917D, 0x4D64, 0xAB, 0xDF, 0xEA, 0x29, 0xE6, 0xC8, 0x77, 0x89);

        public GenericEventsViewModelSource()
        {
            providerIdPreset =
                new ColumnViewModelPreset {
                    Id = ProviderIdColumnId,
                    Name = "Provider Id",
                    IsVisible = false,
                    Width = 100
                }.EnsureFrozen();
            providerNamePreset =
                new ColumnViewModelPreset {
                    Id = ProviderNameColumnId,
                    Name = "Provider",
                    IsVisible = true,
                    Width = 150
                }.EnsureFrozen();
            idPreset =
                new ColumnViewModelPreset {
                    Id = IdColumnId,
                    Name = "Id",
                    IsVisible = true,
                    Width = 50
                }.EnsureFrozen();
            versionPreset =
                new ColumnViewModelPreset {
                    Id = VersionColumnId,
                    Name = "Version",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            channelPreset =
                new ColumnViewModelPreset {
                    Id = ChannelColumnId,
                    Name = "Channel",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            channelNamePreset =
                new ColumnViewModelPreset {
                    Id = ChannelNameColumnId,
                    Name = "Channel Name",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            levelPreset =
                new ColumnViewModelPreset {
                    Id = LevelColumnId,
                    Name = "Level",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            levelNamePreset =
                new ColumnViewModelPreset {
                    Id = LevelNameColumnId,
                    Name = "Level Name",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            taskPreset =
                new ColumnViewModelPreset {
                    Id = TaskColumnId,
                    Name = "Task",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            taskNamePreset =
                new ColumnViewModelPreset {
                    Id = TaskNameColumnId,
                    Name = "Task Name",
                    IsVisible = true,
                    Width = 80
                }.EnsureFrozen();
            opcodeOrTypePreset =
                new ColumnViewModelPreset {
                    Id = OpcodeColumnId,
                    Name = "Opcode/Type",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            opcodeNamePreset =
                new ColumnViewModelPreset {
                    Id = OpcodeNameColumnId,
                    Name = "Opcode Name",
                    IsVisible = true,
                    Width = 80
                }.EnsureFrozen();
            keywordPreset =
                new ColumnViewModelPreset {
                    Id = KeywordColumnId,
                    Name = "Keywords",
                    IsVisible = false,
                    Width = 115,
                    TextAlignment = TextAlignment.Right,
                    CellFormat = "X"
                }.EnsureFrozen();
            keywordNamePreset =
                new ColumnViewModelPreset {
                    Id = KeywordNameColumnId,
                    Name = "Keywords Name",
                    IsVisible = false,
                    Width = 115,
                }.EnsureFrozen();
            messagePreset =
                new ColumnViewModelPreset {
                    Id = MessageColumnId,
                    Name = "Message",
                    IsVisible = true,
                    Width = 500
                }.EnsureFrozen();
            eventNamePreset =
                new ColumnViewModelPreset {
                    Id = new Guid(0xB82277B9, 0x7066, 0x4938, 0xA9, 0x59, 0xEA, 0xBF, 0x0C, 0x68, 0x90, 0x87),
                    Name = "Event Name",
                    IsVisible = false,
                    Width = 100
                }.EnsureFrozen();
            eventTypePreset =
                new ColumnViewModelPreset {
                    Id = new Guid(0xAC2A6011, 0xBCB3, 0x4721, 0xBE, 0xF1, 0xE1, 0xDE, 0xC5, 0x0C, 0x07, 0x3D),
                    Name = "Event Type",
                    IsVisible = false,
                    Width = 100
                }.EnsureFrozen();
            symbolPreset =
                new ColumnViewModelPreset {
                    Id = new Guid(0x79423887, 0x739E, 0x4DFF, 0x90, 0x45, 0x3D, 0xCF, 0x24, 0x3E, 0x29, 0x22),
                    Name = "Symbol",
                    IsVisible = false,
                    Width = 100
                }.EnsureFrozen();
            cpuPreset =
                new ColumnViewModelPreset {
                    Id = new Guid(0x452A05E3, 0xA1C0, 0x4686, 0xBB, 0x6B, 0xC3, 0x9A, 0xFF, 0x2F, 0x24, 0xBE),
                    Name = "CPU",
                    IsVisible = false,
                    Width = 30
                }.EnsureFrozen();
            threadIdPreset =
                new ColumnViewModelPreset {
                    Id = ThreadIdColumnId,
                    Name = "TID",
                    IsVisible = true,
                    Width = 40,
                    TextAlignment = TextAlignment.Right,
                    HelpText = "Thread ID",
                    CellFormat = NumericalFormatProvider.Decimal
                }.EnsureFrozen();
            processIdPreset =
                new ColumnViewModelPreset {
                    Id = ProcessIdColumnId,
                    Name = "PID",
                    IsVisible = true,
                    Width = 40,
                    TextAlignment = TextAlignment.Right,
                    HelpText = "Process ID (0 = PID Not Found)",
                    CellFormat = NumericalFormatProvider.Decimal
                }.EnsureFrozen();
            userDataPreset =
                new ColumnViewModelPreset {
                    Id = new Guid(0x2E47C924, 0x663F, 0x422A, 0x92, 0x32, 0xB1, 0xBC, 0xB1, 0x60, 0x22, 0x80),
                    Name = "UserData",
                    IsVisible = false,
                    Width = 200
                }.EnsureFrozen();
            userDataLengthPreset =
                new ColumnViewModelPreset {
                    Id = new Guid(0x813F4638, 0x8D41, 0x4EAD, 0x94, 0xDD, 0x9A, 0x4A, 0xFF, 0xEF, 0xA7, 0x01),
                    Name = "UserData (Bytes)",
                    IsVisible = false,
                    Width = 30
                }.EnsureFrozen();
            activityIdPreset =
                new ColumnViewModelPreset {
                    Id = new Guid(0x21695563, 0xAC1B, 0x4953, 0x9B, 0x9B, 0x99, 0x13, 0x53, 0xDB, 0xC0, 0x82),
                    Name = "etw:ActivityId",
                    IsVisible = false,
                    Width = 60
                }.EnsureFrozen();
            relatedActivityIdPreset =
                new ColumnViewModelPreset {
                    Id = new Guid(0x83B1BF6F, 0x5E8D, 0x4143, 0xA8, 0x4B, 0x8C, 0x16, 0xED, 0x1E, 0xF6, 0xBD),
                    Name = "etw:Related ActivityId",
                    IsVisible = false,
                    Width = 60
                }.EnsureFrozen();
            userSecurityIdentifierPreset =
                new ColumnViewModelPreset {
                    Id = new Guid(0xF979E52D, 0xEE1B, 0x4A7E, 0x95, 0x0F, 0x28, 0x10, 0x39, 0x90, 0xD1, 0x1B),
                    Name = "etw:UserSid",
                    IsVisible = false,
                    Width = 60
                }.EnsureFrozen();
            sessionIdPreset =
                new ColumnViewModelPreset {
                    Id = new Guid(0x84FC6D0C, 0x5FFD, 0x40D9, 0x8C, 0x3B, 0xF0, 0xEB, 0x8F, 0x8F, 0x2D, 0x1B),
                    Name = "etw:SessionId",
                    IsVisible = false,
                    Width = 60
                }.EnsureFrozen();
            eventKeyPreset =
                new ColumnViewModelPreset {
                    Id = new Guid(0x4F0679D2, 0xB5E7, 0x4AB1, 0xAD, 0xF7, 0xFC, 0xDE, 0xBE, 0xEF, 0x80, 0x1B),
                    Name = "etw:EventKey",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            timePointGeneratorPreset =
                new ColumnViewModelPreset {
                    Id = new Guid(0x9C75AA69, 0x046E, 0x42AE, 0xB5, 0x94, 0xB4, 0xAD, 0x24, 0x33, 0x5A, 0x0A),
                    Name = "Time (Raw)",
                    IsVisible = false,
                    Width = 145,
                    TextAlignment = TextAlignment.Right,
                    CellFormat = TimePointFormatter.FormatSecondsGrouped
                }.EnsureFrozen();
            timeAbsoluteGeneratorPreset =
                new ColumnViewModelPreset {
                    Id = TimeAbsoluteColumnId,
                    Name = "Time",
                    IsVisible = false,
                    Width = 100,
                    CellFormat = "HH:mm:ss.fffffff"
                }.EnsureFrozen();
            timeRelativeGeneratorPreset =
                new ColumnViewModelPreset {
                    Id = TimeRelativeColumnId,
                    Name = "Time Elapsed",
                    IsVisible = true,
                    Width = 120,
                    CellFormat = "HH:mm:ss.fffffff"
                }.EnsureFrozen();
            decodingSourcePreset =
                new ColumnViewModelPreset {
                    Id = DecodingSourceColumnId,
                    Name = "Decoding Source",
                    Width = 150,
                }.EnsureFrozen();
            modernProcessDataPreset =
                new ColumnViewModelPreset {
                    Id = new Guid(0xDC7E68B0, 0xE753, 0x47DF, 0x83, 0x57, 0x61, 0xBE, 0xC0, 0x93, 0xE4, 0x05),
                    Name = "Process",
                    IsVisible = true,
                    Width = 150
                }.EnsureFrozen();
            processNamePreset =
                new ColumnViewModelPreset {
                    Id = new Guid(0xBB09F706, 0xFE79, 0x43AA, 0xA1, 0x03, 0x12, 0x08, 0x01, 0xDA, 0xC2, 0x8F),
                    Name = "Process Name",
                    IsVisible = true,
                    Width = 150
                }.EnsureFrozen();
            stackTopPreset =
                new ColumnViewModelPreset {
                    Id = new Guid(0xD55383F4, 0xD0ED, 0x404B, 0x98, 0xA8, 0xDC, 0x9C, 0xF4, 0x53, 0x3F, 0xBF),
                    Name = "Stack",
                    IsVisible = false,
                    Width = 100
                }.EnsureFrozen();
            threadStartModulePreset =
                new ColumnViewModelPreset {
                    Id = new Guid(0xD58C42B0, 0x818D, 0x4D83, 0xBD, 0x99, 0x9D, 0xA8, 0x72, 0xE7, 0x7B, 0x54),
                    Name = "Thread Start Module",
                    IsVisible = false,
                    Width = 100
                }.EnsureFrozen();
            threadStartFunctionPreset =
                new ColumnViewModelPreset {
                    Id = new Guid(0x125BB527, 0x34C6, 0x4A33, 0x82, 0xB8, 0x05, 0xE3, 0xB0, 0xC7, 0xA5, 0x91),
                    Name = "Thread Start Function",
                    IsVisible = false,
                    Width = 100
                }.EnsureFrozen();
        }

        public static AsyncDataViewModelPreset CreateDefaultPreset()
        {
            var providerIdPreset =
                new ColumnViewModelPreset {
                    Id = ProviderIdColumnId,
                    Name = "Provider Id",
                    IsVisible = false,
                    Width = 100
                }.EnsureFrozen();
            var providerNamePreset =
                new ColumnViewModelPreset {
                    Id = ProviderNameColumnId,
                    Name = "Provider",
                    IsVisible = true,
                    Width = 150
                }.EnsureFrozen();
            var idPreset =
                new ColumnViewModelPreset {
                    Id = IdColumnId,
                    Name = "Id",
                    IsVisible = true,
                    Width = 50
                }.EnsureFrozen();
            var versionPreset =
                new ColumnViewModelPreset {
                    Id = VersionColumnId,
                    Name = "Version",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            var channelPreset =
                new ColumnViewModelPreset {
                    Id = ChannelColumnId,
                    Name = "Channel",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            var channelNamePreset =
                new ColumnViewModelPreset {
                    Id = ChannelNameColumnId,
                    Name = "Channel Name",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            var levelPreset =
                new ColumnViewModelPreset {
                    Id = LevelColumnId,
                    Name = "Level",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            var levelNamePreset =
                new ColumnViewModelPreset {
                    Id = LevelNameColumnId,
                    Name = "Level Name",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            var taskPreset =
                new ColumnViewModelPreset {
                    Id = TaskColumnId,
                    Name = "Task",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            var taskNamePreset =
                new ColumnViewModelPreset {
                    Id = TaskNameColumnId,
                    Name = "Task Name",
                    IsVisible = true,
                    Width = 80
                }.EnsureFrozen();
            var opcodeOrTypePreset =
                new ColumnViewModelPreset {
                    Id = OpcodeColumnId,
                    Name = "Opcode/Type",
                    IsVisible = false,
                    Width = 80
                }.EnsureFrozen();
            var opcodeNamePreset =
                new ColumnViewModelPreset {
                    Id = OpcodeNameColumnId,
                    Name = "Opcode Name",
                    IsVisible = true,
                    Width = 80
                }.EnsureFrozen();
            var keywordPreset =
                new ColumnViewModelPreset {
                    Id = KeywordColumnId,
                    Name = "Keywords",
                    IsVisible = false,
                    Width = 115,
                    TextAlignment = TextAlignment.Right,
                    CellFormat = "X"
                }.EnsureFrozen();
            var messagePreset =
                new ColumnViewModelPreset {
                    Id = MessageColumnId,
                    Name = "Message",
                    IsVisible = true,
                    Width = 500
                }.EnsureFrozen();
            var processIdPreset =
                new ColumnViewModelPreset {
                    Id = ProcessIdColumnId,
                    Name = "PID",
                    IsVisible = true,
                    Width = 40,
                    TextAlignment = TextAlignment.Right,
                    HelpText = "Process ID",
                    CellFormat = NumericalFormatProvider.Decimal
                }.EnsureFrozen();
            var threadIdPreset =
                new ColumnViewModelPreset {
                    Id = ThreadIdColumnId,
                    Name = "TID",
                    IsVisible = true,
                    Width = 40,
                    TextAlignment = TextAlignment.Right,
                    HelpText = "Thread ID",
                    CellFormat = NumericalFormatProvider.Decimal
                }.EnsureFrozen();
            var timeAbsoluteGeneratorPreset =
                new ColumnViewModelPreset {
                    Id = TimeAbsoluteColumnId,
                    Name = "Time",
                    IsVisible = false,
                    Width = 120,
                    CellFormat = "HH:mm:ss.fffffff"
                }.EnsureFrozen();
            var timeRelativeGeneratorPreset =
                new ColumnViewModelPreset {
                    Id = TimeRelativeColumnId,
                    Name = "Time Elapsed",
                    IsVisible = true,
                    Width = 120,
                    CellFormat = "G"
                }.EnsureFrozen();

            var preset = new AsyncDataViewModelPreset {
                Name = "Default",
                LeftFrozenColumnCount = 2,
                ConfigurableColumns = {
                    timeAbsoluteGeneratorPreset,
                    timeRelativeGeneratorPreset,
                    providerIdPreset,
                    providerNamePreset,
                    idPreset,
                    versionPreset,
                    channelPreset,
                    channelNamePreset,
                    taskPreset,
                    taskNamePreset,
                    opcodeNamePreset,
                    opcodeOrTypePreset,
                    levelPreset,
                    levelNamePreset,
                    keywordPreset,
                    processIdPreset,
                    threadIdPreset,
                    messagePreset,
                }
            };
            preset.Freeze();

            return preset;
        }

        public Tuple<DataTable, AsyncDataViewModelPreset> CreateTable(
            IEventInfoSource eventInfoSource, EventSymbolSource symbolSource = null)
        {
            var formatterPool = new ObjectPool<IMessageFormatter>(() => new NativeTdhFormatter(), 10);
            var info = new CrimsonEventsInfo(eventInfoSource, formatterPool, symbolSource);

            var table = new DataTable("Generic Events");
            var templatePreset = new AsyncDataViewModelPreset();

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
            AddColumn(table, templatePreset, opcodeOrTypePreset, DataColumn.Create(info.ProjectOpCode));
            AddColumn(table, templatePreset, opcodeNamePreset, DataColumn.Create(info.ProjectOpCodeName));
            AddColumn(table, templatePreset, levelPreset, DataColumn.Create(info.ProjectLevel));
            AddColumn(table, templatePreset, levelNamePreset, DataColumn.Create(info.ProjectLevelName));
            AddColumn(table, templatePreset, keywordPreset, DataColumn.Create(info.ProjectKeyword));
            AddColumn(table, templatePreset, keywordNamePreset, DataColumn.Create(info.ProjectKeywordName));
            AddColumn(table, templatePreset, processIdPreset, DataColumn.Create(info.ProjectProcessId));
            AddColumn(table, templatePreset, threadIdPreset, DataColumn.Create(info.ProjectThreadId));
            AddColumn(table, templatePreset, messagePreset, DataColumn.Create(info.ProjectMessage));
            AddColumn(table, templatePreset, eventNamePreset, DataColumn.Create(info.ProjectEventName));
            AddColumn(table, templatePreset, eventTypePreset, DataColumn.Create(info.ProjectEventType));
            AddColumn(table, templatePreset, cpuPreset, DataColumn.Create(info.ProjectCpu));
            AddColumn(table, templatePreset, userDataPreset, DataColumn.Create(info.ProjectUserData));
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
            column.TextAlignment = preset.TextAlignment;
            column.IsResizable = true;
            table.Add(column);
            templatePreset.ConfigurableColumns.Add(preset);
        }

        private sealed class CrimsonEventsInfo
        {
            private static readonly Guid ActivityIdSentinel =
                new Guid(0xD733D8B0, 0x7D18, 0x4AEB, 0xA3, 0xFC, 0x8C, 0x46, 0x13, 0xBC, 0x2A, 0x40);

            private readonly IEventInfoSource eventInfoSource;
            private readonly ObjectPool<IMessageFormatter> messageFormatterPool;
            private readonly IEventSymbolSource eventSymbolSource;
            private readonly Dictionary<int, SafeBstrHandle> winmetaOpcodeNames;

            private readonly ParseTdhContext tdhContext = new ParseTdhContext();

            public CrimsonEventsInfo(
                IEventInfoSource eventInfoSource,
                ObjectPool<IMessageFormatter> messageFormatterPool,
                IEventSymbolSource eventSymbolSource)
            {
                this.eventInfoSource = eventInfoSource;
                this.messageFormatterPool = messageFormatterPool;
                this.eventSymbolSource = eventSymbolSource;

                // Standard Windows system opcodes taken from winmeta.xml in the
                // Windows SDK
                winmetaOpcodeNames = new Dictionary<int, SafeBstrHandle> {
                    {0, SafeBstrHandle.Create("win:Info")},
                    {1, SafeBstrHandle.Create("win:Start")},
                    {2, SafeBstrHandle.Create("win:Stop")},
                    {3, SafeBstrHandle.Create("win:DC_Start")},
                    {4, SafeBstrHandle.Create("win:DC_Stop")},
                    {5, SafeBstrHandle.Create("win:Extension")},
                    {6, SafeBstrHandle.Create("win:Reply")},
                    {7, SafeBstrHandle.Create("win:Resume")},
                    {8, SafeBstrHandle.Create("win:Suspend")},
                    {9, SafeBstrHandle.Create("win:Send")},
                    {240, SafeBstrHandle.Create("win:Receive")},
                    {241, SafeBstrHandle.Create("win:ReservedOpcode241")},
                    {242, SafeBstrHandle.Create("win:ReservedOpcode242")},
                    {243, SafeBstrHandle.Create("win:ReservedOpcode243")},
                    {244, SafeBstrHandle.Create("win:ReservedOpcode244")},
                    {245, SafeBstrHandle.Create("win:ReservedOpcode245")},
                    {246, SafeBstrHandle.Create("win:ReservedOpcode246")},
                    {247, SafeBstrHandle.Create("win:ReservedOpcode247")},
                    {248, SafeBstrHandle.Create("win:ReservedOpcode248")},
                    {249, SafeBstrHandle.Create("win:ReservedOpcode249")},
                    {250, SafeBstrHandle.Create("win:ReservedOpcode250")},
                    {251, SafeBstrHandle.Create("win:ReservedOpcode251")},
                    {252, SafeBstrHandle.Create("win:ReservedOpcode252")},
                    {253, SafeBstrHandle.Create("win:ReservedOpcode253")},
                    {254, SafeBstrHandle.Create("win:ReservedOpcode254")},
                    {255, SafeBstrHandle.Create("win:ReservedOpcode255")}
                };
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
                var eventRecord = GetEventRecord(index);
                if (eventRecord.IsTraceLoggingEvent()) {
                    int opcode = eventRecord.EventHeader.EventDescriptor.Opcode;
                    if (winmetaOpcodeNames.TryGetValue(opcode, out var name))
                        return name.Get();
                }

                return GetTraceEventInfo(index).GetOpcodeName();
            }

            public Keyword ProjectKeyword(int index)
            {
                return GetEventRecord(index).EventHeader.EventDescriptor.Keyword;
            }

            public unsafe string ProjectKeywordName(int index)
            {
                var name = GetTraceEventInfo(index).GetKeywordsName();

                var ptr = (char*)name;
                if (ptr[0] == 0 && ptr[1] == 0)
                    return string.Empty;

                var buffer = new StringBuilder();
                while (*ptr != 0) {
                    if (buffer.Length != 0)
                        buffer.Append(", ");
                    var s = new string(ptr);
                    buffer.Append(s);
                    ptr += s.Length + 1;
                }

                return buffer.ToString();
            }

            public string ProjectMessage(int index)
            {
                var info = GetEventInfo(index);

                var formatter = messageFormatterPool.Acquire();
                try {
                    return formatter.GetMessageForEvent(
                        info, tdhContext, CultureInfo.CurrentCulture);
                } finally {
                    messageFormatterPool.Release(formatter);
                }
            }

            public EventType ProjectEventType(int index)
            {
                var record = GetEventRecord(index);
                if (record.IsClassicEvent())
                    return EventType.Classic;
                if (record.IsTraceLoggingEvent())
                    return EventType.TraceLogging;
                if (record.IsWppEvent())
                    return EventType.WPP;
                return EventType.Manifested;
            }

            public string ProjectSymbol(int index)
            {
                if (eventSymbolSource == null)
                    return null;
                var info = GetTraceEventInfo(index);
                var key = new EventKey(info.ProviderId, info.Id, info.Version);
                return eventSymbolSource.TryGetSymbol(key);
            }

            public unsafe string ProjectUserData(int index)
            {
                var record = GetEventRecord(index);
                if (record.UserDataLength == 0)
                    return string.Empty;

                var buffer = new StringBuilder(record.UserDataLength * 3 - 1);
                var data = (byte*)record.UserData.ToPointer();
                for (int i = 0; i < record.UserDataLength; ++i) {
                    if (i > 0) buffer.Append(' ');
                    buffer.AppendHexByte(data[i]);
                }

                return buffer.ToString();
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

            public TimeSpan ProjectTimeRelative(int index)
            {
                var startTime = GetStartTime();
                var time = GetEventRecord(index).TimePoint;
                var elapsedTicks = time.Ticks - startTime.Ticks;
                return new TimeSpan(elapsedTicks);
            }

            public ulong ProjectCpu(int index)
            {
                return GetEventRecord(index).ProcessorIndex;
            }

            public string ProjectEventName(int index)
            {
                return null; // FIXME
            }

            public DecodingSource ProjectDecodingSource(int index)
            {
                return GetTraceEventInfo(index).DecodingSource;
            }

            public bool HasStackTrace(int index)
            {
                return GetEventRecord(index).HasStackTrace;
            }

            public unsafe StackTraceInfo ProjectStackTrace(int index)
            {
                var item = GetEventRecord(index).FindExtendedData(EVENT_HEADER_EXT_TYPE.STACK_TRACE64);
                if (item != null) {
                    var trace64 = (EVENT_EXTENDED_ITEM_STACK_TRACE64*)item->Data;
                    var addressCount = (item->DataSize - sizeof(ulong)) / sizeof(ulong);

                    var addresses = new ulong[addressCount];
                    for (int i = 0; i < addressCount; ++i)
                        addresses[i] = trace64->Address[i];

                    return new StackTraceInfo(trace64->MatchId, addresses, true);
                }

                item = GetEventRecord(index).FindExtendedData(EVENT_HEADER_EXT_TYPE.STACK_TRACE32);
                if (item != null) {
                    var trace32 = (EVENT_EXTENDED_ITEM_STACK_TRACE32*)item->Data;
                    var addressCount = (item->DataSize - sizeof(ulong)) / sizeof(uint);

                    var addresses = new ulong[addressCount];
                    for (int i = 0; i < addressCount; ++i)
                        addresses[i] = trace32->Address[i];

                    return new StackTraceInfo(trace32->MatchId, addresses, false);
                }

                return null;
            }
        }
    }

    public sealed class StackTraceInfo
    {
        private readonly bool is64Bit;

        public StackTraceInfo(ulong matchId, IReadOnlyList<ulong> addresses, bool is64Bit)
        {
            MatchId = matchId;
            Addresses = addresses;
            this.is64Bit = is64Bit;
        }

        public ulong MatchId { get; }
        public IReadOnlyList<ulong> Addresses { get; }
    }

    public enum EventType
    {
        Unknown,
        Classic,
        Manifested,
        TraceLogging,
        WPP,
        MaxValue
    }
}
