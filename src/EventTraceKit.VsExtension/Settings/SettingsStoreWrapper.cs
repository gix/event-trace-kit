namespace EventTraceKit.VsExtension.Settings
{
    public struct SettingsStoreWrapper : ISettingsStore
    {
        private readonly ISettingsStore store;

        public SettingsStoreWrapper(ISettingsStore store)
        {
            this.store = store;
        }

        public string ActiveViewPreset
        {
            get => store.GetValue(SettingsKeys.ActiveViewPreset);
            set => store.SetValue(SettingsKeys.ActiveViewPreset, value);
        }

        public bool AutoLog
        {
            get => store.GetValue(SettingsKeys.AutoLog);
            set => store.SetValue(SettingsKeys.AutoLog, value);
        }

        public bool ShowColumnHeaders
        {
            get => store.GetValue(SettingsKeys.ShowColumnHeaders);
            set => store.SetValue(SettingsKeys.ShowColumnHeaders, value);
        }

        public bool ShowStatusBar
        {
            get => store.GetValue(SettingsKeys.ShowStatusBar);
            set => store.SetValue(SettingsKeys.ShowStatusBar, value);
        }

        public bool IsFilterEnabled
        {
            get => store.GetValue(SettingsKeys.IsFilterEnabled);
            set => store.SetValue(SettingsKeys.IsFilterEnabled, value);
        }

        public string LayerId => store.LayerId;
        public string Name => store.Name;
        public string Origin => store.Origin;

        public T GetValue<T>(SettingsKey<T> key)
        {
            return store.GetValue(key);
        }

        public void ClearValue<T>(SettingsKey<T> key)
        {
            store.ClearValue(key);
        }

        public void SetValue<T>(SettingsKey<T> key, T value)
        {
            store.SetValue(key, value);
        }

        public void Reload()
        {
            store.Reload();
        }

        public void Save()
        {
            store.Save();
        }
    }

    public static class SettingsServiceExtensions
    {
        public static SettingsStoreWrapper GetAmbientStoreWrapper(this ISettingsService settingsService)
        {
            return new SettingsStoreWrapper(settingsService.GetAmbientStore());
        }
    }
}
