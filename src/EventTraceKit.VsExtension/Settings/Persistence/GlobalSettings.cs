namespace EventTraceKit.VsExtension.Settings.Persistence
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;

    public class GlobalSettings : SettingsElement
    {
        private Collection<ViewPreset> userPresets;
        private Collection<ViewPreset> persistedPresets;
        private Collection<TraceProfile> profiles;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<ViewPreset> UserPresets =>
            userPresets ?? (userPresets = new Collection<ViewPreset>());

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<ViewPreset> PersistedPresets =>
            persistedPresets ?? (persistedPresets = new Collection<ViewPreset>());

        public Guid ActiveProfile { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<TraceProfile> Profiles =>
            profiles ?? (profiles = new Collection<TraceProfile>());
    }
}
