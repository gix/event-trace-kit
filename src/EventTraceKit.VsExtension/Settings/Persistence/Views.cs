namespace EventTraceKit.VsExtension.Settings.Persistence
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;

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
