namespace EventTraceKit.VsExtension.Settings.Persistence
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows.Markup;

    [ContentProperty(nameof(Entries))]
    public sealed class Settings : SettingsElement
    {
        private Dictionary<string, object> entries;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Dictionary<string, object> Entries =>
            entries ?? (entries = new Dictionary<string, object>());
    }
}
