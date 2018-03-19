namespace EventTraceKit.VsExtension.Settings
{
    using System;

    public interface ISettingsService
    {
        event EventHandler SettingsLayerChanged;

        ISettingsStore GetGlobalStore();
        ISettingsStore GetProjectStore(ProjectInfo project);
        ISettingsStore GetAmbientStore();

        AdvmPresetCollection GetViewPresets();
        void SaveViewPresets(AdvmPresetCollection presets);

        void SaveAmbient();
    }
}
