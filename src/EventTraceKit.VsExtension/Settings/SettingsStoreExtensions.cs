namespace EventTraceKit.VsExtension.Settings
{
    using AutoMapper;
    using EventTraceKit.VsExtension.Settings.Persistence;

    public static class SettingsStoreExtensions
    {
        public static SettingsStoreWrapper AsWrapper(this ISettingsStore settingsStore)
        {
            return new SettingsStoreWrapper(settingsStore);
        }

        public static AdvmPresetCollection GetViewPresets(
            this ISettingsStore settingsStore, IMapper mapper)
        {
            var viewPresets = settingsStore.GetValue(SettingsKeys.Views) ?? new ViewPresets();
            return mapper.Map<AdvmPresetCollection>(viewPresets);
        }

        public static void SetViewPresets(
            this ISettingsStore settingsStore, AdvmPresetCollection presets, IMapper mapper)
        {
            settingsStore.SetValue(SettingsKeys.Views, mapper.Map<ViewPresets>(presets));
        }
    }
}
