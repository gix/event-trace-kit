namespace EventTraceKit.VsExtension.Settings
{
    public struct SettingsStoreWrapper : ISettingsStore
    {
        private readonly ISettingsStore store;

        public SettingsStoreWrapper(ISettingsStore store)
        {
            this.store = store;
        }

        public string LayerId => store.LayerId;
        public string Name => store.Name;
        public string Origin => store.Origin;

        public T GetValue<T>(SettingsKey<T> key)
        {
            if (store == null)
                return default;
            return store.GetValue(key);
        }

        public void ClearValue<T>(SettingsKey<T> key)
        {
            store?.ClearValue(key);
        }

        public void SetValue<T>(SettingsKey<T> key, T value)
        {
            store?.SetValue(key, value);
        }

        public void Reload()
        {
            store?.Reload();
        }

        public void Save()
        {
            store?.Save();
        }

        public string ActiveViewPreset
        {
            get => store?.GetValue(SettingsKeys.ActiveViewPreset);
            set => store?.SetValue(SettingsKeys.ActiveViewPreset, value);
        }

        public bool AutoLog
        {
            get => store?.GetValue(SettingsKeys.AutoLog) ?? default;
            set => store?.SetValue(SettingsKeys.AutoLog, value);
        }

        public bool AutoScroll
        {
            get => store?.GetValue(SettingsKeys.AutoScroll) ?? default;
            set => store?.SetValue(SettingsKeys.AutoScroll, value);
        }

        public bool ShowColumnHeaders
        {
            get => store?.GetValue(SettingsKeys.ShowColumnHeaders) ?? default;
            set => store?.SetValue(SettingsKeys.ShowColumnHeaders, value);
        }

        public bool ShowStatusBar
        {
            get => store?.GetValue(SettingsKeys.ShowStatusBar) ?? default;
            set => store?.SetValue(SettingsKeys.ShowStatusBar, value);
        }

        public bool IsFilterEnabled
        {
            get => store?.GetValue(SettingsKeys.IsFilterEnabled) ?? default;
            set => store?.SetValue(SettingsKeys.IsFilterEnabled, value);
        }
    }
}
