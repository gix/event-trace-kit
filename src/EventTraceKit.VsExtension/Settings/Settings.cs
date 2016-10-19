[assembly: System.Windows.Markup.XmlnsDefinition(
    "urn:schemas-eventtracekit:settings",
    "EventTraceKit.VsExtension.Settings")]

namespace EventTraceKit.VsExtension.Settings
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;

    public abstract class SettingsElement
    {
    }

    public class GlobalSettings : SettingsElement
    {
        private Collection<ViewPreset> userPresets;
        private Collection<ViewPreset> persistedPresets;
        private Collection<TraceSession> sessions;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<ViewPreset> UserPresets =>
            userPresets ?? (userPresets = new Collection<ViewPreset>());

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<ViewPreset> PersistedPresets =>
            persistedPresets ?? (persistedPresets = new Collection<ViewPreset>());

        public Guid ActiveSession { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<TraceSession> Sessions =>
            sessions ?? (sessions = new Collection<TraceSession>());
    }

    public class ViewPresets : SettingsElement
    {
        private Collection<ViewPreset> userPresets;
        private Collection<ViewPreset> persistedPresets;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<ViewPreset> UserPresets =>
            userPresets ?? (userPresets = new Collection<ViewPreset>());

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<ViewPreset> PersistedPresets =>
            persistedPresets ?? (persistedPresets = new Collection<ViewPreset>());
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

    public class ViewPreset : SettingsElement
    {
        private Collection<ViewColumn> columns;

        [DefaultValue(null)]
        public string Name { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<ViewColumn> Columns =>
            columns ?? (columns = new Collection<ViewColumn>());

        [DefaultValue(0)]
        public int LeftFrozenColumnCount { get; set; }
        [DefaultValue(0)]
        public int RightFrozenColumnCount { get; set; }
    }

    public class ViewColumn : SettingsElement
    {
        public Guid Id { get; set; }
        [DefaultValue(null)]
        public string CellFormat { get; set; }
        [DefaultValue(null)]
        public string HelpText { get; set; }
        [DefaultValue(false)]
        public bool IsVisible { get; set; }
        [DefaultValue(null)]
        public string Name { get; set; }
        [DefaultValue(null)]
        public string SortOrder { get; set; }
        [DefaultValue(0)]
        public int SortPriority { get; set; }
        [DefaultValue(null)]
        public string TextAlignment { get; set; }
        [DefaultValue(0)]
        public int Width { get; set; }
    }
}
