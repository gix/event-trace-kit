namespace EventTraceKit.VsExtension.Settings
{
    using System;

    public interface ISettingsService
    {
        event EventHandler SettingsLayerChanged;

        ISettingsStore GetGlobalStore();
        ISettingsStore GetProjectStore(ProjectInfo project);
        ISettingsStore GetAmbientStore();

        void SaveAmbient();
    }
}
