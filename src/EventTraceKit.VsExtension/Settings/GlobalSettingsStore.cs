namespace EventTraceKit.VsExtension.Settings
{
    using System.IO;

    internal class GlobalSettingsStore : SettingsStore
    {
        public GlobalSettingsStore(string appDataRoamingDirectory)
            : base(GetPath(appDataRoamingDirectory), "Global", "Global")
        {
        }

        private static string GetPath(string parent)
        {
            return Path.Combine(parent, "Settings.xml");
        }
    }
}
