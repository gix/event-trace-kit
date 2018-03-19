namespace EventTraceKit.VsExtension.Settings
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using EventTraceKit.VsExtension.Extensions;
    using EventTraceKit.VsExtension.Serialization;
    using EventTraceKit.VsExtension.Settings.Persistence;

    internal class XamlFileSettingsStorage
    {
        private readonly object mutex = new object();
        private readonly string filePath;
        private Dictionary<string, object> entries;
        private State state = State.Unloaded;
        private bool saveInProgress;

        private enum State
        {
            Unloaded,
            Unmodified,
            Dirty
        }

        public XamlFileSettingsStorage(string filePath)
        {
            this.filePath = filePath;
        }

        public T GetValue<T>(SettingsKey<T> key)
        {
            lock (mutex) {
                EnsureLoaded();
                if (entries.TryGetValue(key.KeyPath, out object obj) && obj is T value)
                    return value;
            }

            return default;
        }

        public void ClearValue<T>(SettingsKey<T> key)
        {
            lock (mutex) {
                EnsureLoaded();
                entries.Remove(key.KeyPath);
                state = State.Dirty;
            }
        }

        public void SetValue<T>(SettingsKey<T> key, T value)
        {
            lock (mutex) {
                EnsureLoaded();
                if (EqualityComparer<T>.Default.Equals(value, default))
                    entries.Remove(key.KeyPath);
                else
                    entries[key.KeyPath] = value;
                state = State.Dirty;
            }
        }

        public void Reload()
        {
            lock (mutex) {
                entries.Clear();
                Load();
                state = State.Unmodified;
            }
        }

        public void Save()
        {
            lock (mutex) {
                if (saveInProgress || state == State.Unmodified)
                    return;

                saveInProgress = true;
                try {
                    SaveOrDelete();
                } finally {
                    saveInProgress = false;
                    state = State.Unmodified;
                }
            }
        }

        private void SaveOrDelete()
        {
            if (entries.Count == 0) {
                try {
                    File.Delete(filePath);
                } catch (DirectoryNotFoundException) {
                }

                return;
            }

            EnsureDirectoryExists(filePath);
            using (var output = File.Open(filePath, FileMode.Create, FileAccess.Write))
                SaveEntries(output);
        }

        private void SaveEntries(Stream output)
        {
            var serializer = SettingsSerializer.CreateXamlSerializer();

            var settings = new Settings();
            settings.Entries.AddRange(entries);
            serializer.Save(settings, output);
        }

        private static void EnsureDirectoryExists(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            if (directory != null)
                Directory.CreateDirectory(directory);
        }

        private void EnsureLoaded()
        {
            if (state != State.Unloaded)
                return;

            Load();
            state = State.Unmodified;
        }

        private void Load()
        {
            try {
                if (!File.Exists(filePath))
                    return;

                using (var input = File.Open(filePath, FileMode.Open, FileAccess.Read))
                    Load(input);
            } catch (FileNotFoundException) {
            } finally {
                state = State.Unmodified;
                if (entries == null)
                    entries = new Dictionary<string, object>();
            }
        }

        private void Load(Stream input)
        {
            var serializer = SettingsSerializer.CreateXamlSerializer();
            try {
                var obj = serializer.Load(input);
                if (obj is Settings settings)
                    entries = settings.Entries;
            } catch (Exception) {
            } finally {
                if (entries == null)
                    entries = new Dictionary<string, object>();
                state = State.Unmodified;
            }
        }
    }
}
