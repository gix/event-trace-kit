namespace EventTraceKit.VsExtension
{
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Serialization;

    [ComVisible(true)]
    [Guid("9619B7BF-69E2-4F5F-B95C-F2E6EDA02205")]
    public sealed class EventTraceKitProfileManager : Component, IProfileManager
    {
        private EventTraceKitPackage package;

        public override ISite Site
        {
            get { return base.Site; }
            set
            {
                base.Site = value;
                package = Site?.GetService<EventTraceKitPackage>();
            }
        }

        public void LoadSettingsFromStorage()
        {
        }

        public void LoadSettingsFromXml(IVsSettingsReader reader)
        {
            if (package == null)
                return;

            string xml;
            if (reader.ReadSettingXmlAsString("GlobalSettings", out xml) != VSConstants.S_OK)
                return;

            var serializer = new SettingsSerializer();
            try {
                package.GlobalSettings = serializer.LoadFromString<GlobalSettings>(xml);
            } catch {
            }
        }

        public void ResetSettings()
        {
        }

        public void SaveSettingsToStorage()
        {
        }

        public void SaveSettingsToXml(IVsSettingsWriter writer)
        {
            var settings = package?.GlobalSettings;
            if (settings == null)
                return;

            var serializer = new SettingsSerializer();
            var xml = serializer.SaveToString(settings);
            writer.WriteSettingXmlFromString(xml);
        }
    }
}
