namespace EventTraceKit.VsExtension.Settings
{
    using System;

    internal abstract class SettingsStore : ISettingsStore
    {
        private readonly XamlFileSettingsStorage storage;

        protected SettingsStore(string path, string layerId, string name, string origin = null)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (layerId == null)
                throw new ArgumentNullException(nameof(layerId));
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            storage = new XamlFileSettingsStorage(path);
            LayerId = layerId;
            Name = name;
            Origin = origin ?? $"File '{path}'";
        }

        public string LayerId { get; }
        public string Name { get; }
        public string Origin { get; }

        public T GetValue<T>(SettingsKey<T> key)
        {
            return storage.GetValue(key);
        }

        public void ClearValue<T>(SettingsKey<T> key)
        {
            storage.ClearValue(key);
        }

        public void SetValue<T>(SettingsKey<T> key, T value)
        {
            storage.SetValue(key, value);
        }

        public void Reload()
        {
            storage.Reload();
        }

        public void Save()
        {
            storage.Save();
        }
    }
}
