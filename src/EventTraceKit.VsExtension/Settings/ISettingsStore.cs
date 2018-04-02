namespace EventTraceKit.VsExtension.Settings
{
    using EventTraceKit.VsExtension.Settings.Persistence;

    public interface ISettingsStoreInfo
    {
        string LayerId { get; }
        string Name { get; }
        string Origin { get; }
    }

    public interface ISettingsStore : ISettingsStoreInfo
    {
        T GetValue<T>(SettingsKey<T> key);
        void ClearValue<T>(SettingsKey<T> key);
        void SetValue<T>(SettingsKey<T> key, T value);
        void Reload();
        void Save();
    }

    public struct SettingsKey<T>
    {
        public SettingsKey(string keyPath)
        {
            KeyPath = keyPath;
        }

        public string KeyPath { get; }
    }

    public static class SettingsKeys
    {
        public static SettingsKey<bool> AutoLog => new SettingsKey<bool>("AutoLog");
        public static SettingsKey<bool> AutoScroll => new SettingsKey<bool>("AutoScroll");
        public static SettingsKey<bool> ShowColumnHeaders => new SettingsKey<bool>("ShowColumnHeaders");
        public static SettingsKey<bool> ShowStatusBar => new SettingsKey<bool>("ShowStatusBar");
        public static SettingsKey<bool> IsFilterEnabled => new SettingsKey<bool>("IsFilterEnabled");
        public static SettingsKey<string> ActiveViewPreset => new SettingsKey<string>("ActiveViewPreset");
        public static SettingsKey<TraceSettings> Tracing => new SettingsKey<TraceSettings>("Tracing");
        public static SettingsKey<ViewPresets> Views => new SettingsKey<ViewPresets>("Views");
    }
}
