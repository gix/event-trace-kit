namespace EventTraceKit.VsExtension
{
    using System;
    using System.ComponentModel.Composition;
    using Extensions;
    using Microsoft.VisualStudio.Settings;
    using Microsoft.VisualStudio.Shell;
    using Serialization;

    [Export(typeof(IGlobalSettings))]
    internal sealed class SettingsStoreGlobalSettings : IGlobalSettings
    {
        private const string ErrorGetFormat = "Cannot get setting {0}";
        private const string ErrorSetFormat = "Cannot set setting {0}";

        private const string CollectionPath = "EventTraceKit";
        private const string ActiveViewPresetName = "ActiveViewPreset";
        private const string AutoLogName = "AutoLog";
        private const string ShowColumnHeadersName = "ShowColumnHeaders";
        private const string ShowStatusBarName = "ShowStatusBar";

        private readonly WritableSettingsStore settingsStore;

        [ImportingConstructor]
        internal SettingsStoreGlobalSettings(SVsServiceProvider vsServiceProvider)
            : this(vsServiceProvider.GetWritableSettingsStore())
        {
        }

        internal SettingsStoreGlobalSettings(WritableSettingsStore settingsStore)
        {
            this.settingsStore = settingsStore;
        }

        public string ActiveViewPreset
        {
            get => GetString(ActiveViewPresetName, null);
            set => SetString(ActiveViewPresetName, value, null);
        }

        public bool AutoLog
        {
            get => GetBool(AutoLogName, false);
            set => SetBool(AutoLogName, value, false);
        }

        public bool ShowColumnHeaders
        {
            get => GetBool(ShowColumnHeadersName, true);
            set => SetBool(ShowColumnHeadersName, value, true);
        }

        public bool ShowStatusBar
        {
            get => GetBool(ShowStatusBarName, true);
            set => SetBool(ShowStatusBarName, value, true);
        }

        private void Report(string format, Exception exception)
        {
            // FIXME
        }

        private void EnsureCollectionExists()
        {
            if (!settingsStore.CollectionExists(CollectionPath))
                settingsStore.CreateCollection(CollectionPath);
        }

        private bool GetBool(string propertyName, bool defaultValue)
        {
            EnsureCollectionExists();
            try {
                if (!settingsStore.PropertyExists(CollectionPath, propertyName))
                    return defaultValue;
                return settingsStore.GetBoolean(CollectionPath, propertyName);
            } catch (Exception ex) {
                Report(string.Format(ErrorGetFormat, propertyName), ex);
                return defaultValue;
            }
        }

        private void SetBool(string propertyName, bool value, bool defaultValue)
        {
            EnsureCollectionExists();
            try {
                if (value == defaultValue)
                    settingsStore.DeleteProperty(CollectionPath, propertyName);
                else
                    settingsStore.SetBoolean(CollectionPath, propertyName, value);
            } catch (Exception ex) {
                Report(string.Format(ErrorSetFormat, propertyName), ex);
            }
        }

        private string GetString(string propertyName, string defaultValue)
        {
            EnsureCollectionExists();
            try {
                if (!settingsStore.PropertyExists(CollectionPath, propertyName))
                    return defaultValue;
                return settingsStore.GetString(CollectionPath, propertyName);
            } catch (Exception ex) {
                Report(string.Format(ErrorGetFormat, propertyName), ex);
                return defaultValue;
            }
        }

        private void SetString(string propertyName, string value, string defaultValue)
        {
            EnsureCollectionExists();
            try {
                if (value == defaultValue)
                    settingsStore.DeleteProperty(CollectionPath, propertyName);
                else
                    settingsStore.SetString(CollectionPath, propertyName, value);
            } catch (Exception ex) {
                Report(string.Format(ErrorSetFormat, propertyName), ex);
            }
        }

        private T GetObject<T>(string propertyName, Func<T> defaultValue)
        {
            EnsureCollectionExists();
            try {
                if (!settingsStore.PropertyExists(CollectionPath, propertyName))
                    return defaultValue();
                var serializer = new SettingsSerializer();
                var stream = settingsStore.GetMemoryStream(CollectionPath, propertyName);
                using (stream)
                    return serializer.Load<T>(stream);
            } catch (Exception ex) {
                Report(string.Format(ErrorGetFormat, propertyName), ex);
                return defaultValue();
            }
        }

        private void SetObject<T>(string propertyName, T value)
        {
            EnsureCollectionExists();
            try {
                var serializer = new SettingsSerializer();
                var stream = serializer.SaveToStream(value);
                settingsStore.SetMemoryStream(CollectionPath, propertyName, stream);
            } catch (Exception ex) {
                Report(string.Format(ErrorSetFormat, propertyName), ex);
            }
        }
    }
}
