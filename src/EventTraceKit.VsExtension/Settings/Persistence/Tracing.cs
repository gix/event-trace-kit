namespace EventTraceKit.VsExtension.Settings.Persistence
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using EventTraceKit.VsExtension.Filtering;

    public class TraceSettings : SettingsElement
    {
        private Collection<TraceProfile> profiles;
        private Collection<TraceLogFilter> filters;

        [DefaultValue(null)]
        public Guid? ActiveProfile { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<TraceProfile> Profiles =>
            profiles ?? (profiles = new Collection<TraceProfile>());

        [DefaultValue(null)]
        public TraceLogFilter ActiveFilter { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<TraceLogFilter> Filters =>
            filters ?? (filters = new Collection<TraceLogFilter>());
    }

    public class TraceLogFilter : SettingsElement
    {
        private Collection<TraceLogFilter> conditions;

        public string Name { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<TraceLogFilter> Conditions =>
            conditions ?? (conditions = new Collection<TraceLogFilter>());
    }

    public class TraceLogFilterCondition : SettingsElement
    {
        public string Property { get; set; }

        [DefaultValue(false)]
        public bool IsEnabled { get; set; }

        public FilterRelationKind Relation { get; set; }
        public FilterConditionAction Action { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public object Value { get; set; }
    }

    public class TraceProfile : SettingsElement
    {
        private Collection<Collector> collectors;

        public Guid Id { get; set; }

        [DefaultValue(null)]
        public string Name { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<Collector> Collectors =>
            collectors ?? (collectors = new Collection<Collector>());
    }

    public abstract class Collector : SettingsElement
    {
    }

    public class SystemCollector : Collector
    {
    }

    public class EventCollector : Collector
    {
        private Collection<EventProvider> providers;

        [DefaultValue(null)]
        public string Name { get; set; }
        [DefaultValue(null)]
        public uint? BufferSize { get; set; }
        [DefaultValue(null)]
        public uint? MinimumBuffers { get; set; }
        [DefaultValue(null)]
        public uint? MaximumBuffers { get; set; }
        [DefaultValue(null)]
        public string LogFileName { get; set; }
        [DefaultValue(null)]
        public TimeSpan? FlushPeriod { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<EventProvider> Providers =>
            providers ?? (providers = new Collection<EventProvider>());
    }

    public class EventProvider : SettingsElement
    {
        private Collection<string> executableNames;
        private Collection<uint> processIds;
        private Collection<ushort> eventIds;
        private Collection<string> startupProjects;
        private Collection<ushort> stackWalkEventIds;

        public Guid Id { get; set; }
        [DefaultValue(null)]
        public string Name { get; set; }
        [DefaultValue(false)]
        public bool IsEnabled { get; set; }
        [DefaultValue((byte)0)]
        public byte Level { get; set; }
        [DefaultValueEx(0UL)]
        public ulong MatchAnyKeyword { get; set; }
        [DefaultValueEx(0UL)]
        public ulong MatchAllKeyword { get; set; }

        [DefaultValue(false)]
        public bool IncludeSecurityId { get; set; }
        [DefaultValue(false)]
        public bool IncludeTerminalSessionId { get; set; }
        [DefaultValue(false)]
        public bool IncludeStackTrace { get; set; }

        [DefaultValue(null)]
        public string Manifest { get; set; }

        [DefaultValue(false)]
        public bool FilterExecutableNames { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<string> ExecutableNames =>
            executableNames ?? (executableNames = new Collection<string>());

        [DefaultValue(false)]
        public bool FilterProcessIds { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<uint> ProcessIds =>
            processIds ?? (processIds = new Collection<uint>());

        [DefaultValue(false)]
        public bool FilterEventIds { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<ushort> EventIds =>
            eventIds ?? (eventIds = new Collection<ushort>());

        [DefaultValue(true)]
        public bool EventIdsFilterIn { get; set; } = true;

        public Collection<string> StartupProjects =>
            startupProjects ?? (startupProjects = new Collection<string>());

        [DefaultValue(false)]
        public bool FilterStackWalkEventIds { get; set; }
        [DefaultValue(true)]
        public bool StackWalkEventIdsFilterIn { get; set; } = true;
        public Collection<ushort> StackWalkEventIds =>
            stackWalkEventIds ?? (stackWalkEventIds = new Collection<ushort>());

        [DefaultValue(false)]
        public bool FilterStackWalkLevelKeyword { get; set; }
        [DefaultValue(true)]
        public bool StackWalkFilterIn { get; set; } = true;
        [DefaultValue((byte)0)]
        public byte StackWalkLevel { get; set; }
        [DefaultValueEx(0UL)]
        public ulong StackWalkMatchAnyKeyword { get; set; }
        [DefaultValueEx(0UL)]
        public ulong StackWalkMatchAllKeyword { get; set; }
    }
}
