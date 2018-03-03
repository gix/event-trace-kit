namespace EventTraceKit.VsExtension.Settings.Persistence
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;

    public class TraceSettings : SettingsElement
    {
        private Collection<TraceProfile> profiles;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<TraceProfile> Profiles =>
            profiles ?? (profiles = new Collection<TraceProfile>());
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

    public class EventCollector : Collector
    {
        private Collection<EventProvider> providers;

        public Guid Id { get; set; }
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

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<EventProvider> Providers =>
            providers ?? (providers = new Collection<EventProvider>());
    }

    public class EventProvider : SettingsElement
    {
        private Collection<string> executableNames;
        private Collection<uint> processIds;
        private Collection<ushort> eventIds;

        public Guid Id { get; set; }
        [DefaultValue(null)]
        public string Name { get; set; }
        [DefaultValue(false)]
        public bool IsEnabled { get; set; }
        [DefaultValue(0)]
        public byte Level { get; set; }
        [DefaultValue(0)]
        public ulong MatchAnyKeyword { get; set; }
        [DefaultValue(0)]
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
        public bool EnableEvents { get; set; } = true;

        [DefaultValue(null)]
        public string StartupProject { get; set; }
    }
}
