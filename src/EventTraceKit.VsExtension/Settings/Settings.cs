[assembly: System.Windows.Markup.XmlnsDefinition(
    "urn:schemas-eventtracekit:settings",
    "EventTraceKit.VsExtension.Settings")]

namespace EventTraceKit.VsExtension.Settings
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public abstract class SettingsElement
    {
    }

    public static class Init
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetOrCreate<T>(ref T field)
            where T : class, new()
        {
            return field ?? (field = new T());
        }
    }

    public class GlobalSettings : SettingsElement
    {
        private Collection<ProfilePreset> modifiedPresets;
        private Collection<ProfilePreset> persistedPresets;
        private Collection<TraceSession> sessions;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<ProfilePreset> ModifiedPresets => Init.GetOrCreate(ref modifiedPresets);

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<ProfilePreset> PersistedPresets => Init.GetOrCreate(ref persistedPresets);

        public Guid ActiveSession { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<TraceSession> Sessions => Init.GetOrCreate(ref sessions);
    }

    public class ViewPresets : SettingsElement
    {
        private Collection<ProfilePreset> modifiedPresets;
        private Collection<ProfilePreset> persistedPresets;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<ProfilePreset> ModifiedPresets =>
            modifiedPresets ?? (modifiedPresets = new Collection<ProfilePreset>());

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<ProfilePreset> PersistedPresets =>
            persistedPresets ?? (persistedPresets = new Collection<ProfilePreset>());
    }

    public class TraceSession : SettingsElement
    {
        private TraceProviderProfileCollection providers;

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
        public TraceProviderProfileCollection Providers =>
            providers ?? (providers = new TraceProviderProfileCollection());
    }

    public class TraceProviderProfileCollection : Collection<TraceProvider>
    {
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

    public class ProfilePreset : SettingsElement
    {
        private Collection<ProfileColumn> columns;

        [DefaultValue(null)]
        public string Name { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<ProfileColumn> Columns =>
            columns ?? (columns = new Collection<ProfileColumn>());

        [DefaultValue(0)]
        public int LeftFrozenColumnCount { get; set; }
        [DefaultValue(0)]
        public int RightFrozenColumnCount { get; set; }
    }

    public class ProfileColumn : SettingsElement
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
