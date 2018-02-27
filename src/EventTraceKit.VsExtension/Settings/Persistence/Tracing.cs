namespace EventTraceKit.VsExtension.Settings.Persistence
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;

    public class TraceSettings : SettingsElement
    {
        private Collection<TraceSession> sessions;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<TraceSession> Sessions =>
            sessions ?? (sessions = new Collection<TraceSession>());
    }

    public class TraceSession : SettingsElement
    {
        private Collection<TraceProvider> providers;

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
        public Collection<TraceProvider> Providers =>
            providers ?? (providers = new Collection<TraceProvider>());
    }

    public class TraceProvider : SettingsElement
    {
        private Collection<uint> processIds;
        private Collection<TraceEvent> events;

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

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<uint> ProcessIds =>
            processIds ?? (processIds = new Collection<uint>());

        public bool FilterEvents { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<TraceEvent> Events =>
            events ?? (events = new Collection<TraceEvent>());
    }

    public class TraceEvent : SettingsElement
    {
        [DefaultValue(false)]
        public bool IsEnabled { get; set; }
        [DefaultValue(0)]
        public ushort Id { get; set; }
        [DefaultValue(0)]
        public byte Version { get; set; }
        [DefaultValue(null)]
        public string Symbol { get; set; }
        [DefaultValue(null)]
        public string Level { get; set; }
        [DefaultValue(null)]
        public string Channel { get; set; }
        [DefaultValue(null)]
        public string Task { get; set; }
        [DefaultValue(null)]
        public string Opcode { get; set; }
        [DefaultValue(null)]
        public string Keywords { get; set; }
    }
}
