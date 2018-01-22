namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.PlatformUI;
    using Serialization;
    using Settings.Persistence;
    using Task = System.Threading.Tasks.Task;

    public class FileSettingsStorage
    {
        private readonly string fileName = "ViewPresets.xml";
        private readonly SettingsSerializer serializer = new SettingsSerializer();

        public FileSettingsStorage(string rootDirectory)
        {
            RootDirectory = rootDirectory;
        }

        private string RootDirectory { get; }

        public void Save(ViewPresets profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));
            if (EnsureLocalStorageDirectoryExists())
                SavePresetsToLocalStorage(profile);
        }

        public ViewPresets Load()
        {
            return LoadFromLocalStorage(fileName);
        }

        private ViewPresets LoadFromLocalStorage(string profileName)
        {
            try {
                using (Stream stream = OpenLocalStorage(profileName, FileAccess.Read))
                    return LoadProfile(stream);
            } catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex)) {
                RaiseExceptionFilter(ex, "Failed to load window configuration.");
                return null;
            }
        }

        private ViewPresets LoadProfile(Stream stream)
        {
            return serializer.Load<ViewPresets>(stream);
        }

        private void SavePresetsToLocalStorage(ViewPresets profile)
        {
            try {
                using (Stream stream = OpenLocalStorage(fileName, FileAccess.Write))
                    serializer.Save(profile, stream);
            } catch (IOException ex) {
                RaiseExceptionFilter(ex, "Failed to save window configuration.");
            } catch (UnauthorizedAccessException ex) {
                RaiseExceptionFilter(ex, "Failed to save window configuration.");
            }
        }

        private Stream OpenLocalStorage(string profileName, FileAccess fileAccess)
        {
            ThrowIfProfileNameInvalid(profileName);
            FileMode mode = fileAccess == FileAccess.Read ? FileMode.Open : FileMode.Create;
            FileShare share = fileAccess == FileAccess.Read ? FileShare.Read : FileShare.None;
            return File.Open(GetProfileFullPath(profileName), mode, fileAccess, share);
        }

        private string GetProfileFullPath(string profileName)
        {
            return Path.Combine(RootDirectory, profileName);
        }

        private void ThrowIfProfileNameInvalid(string str)
        {
            str.ThrowIfNullOrEmpty("Window profile name cannot be null or empty.");
        }

        private bool EnsureLocalStorageDirectoryExists()
        {
            try {
                if (!Directory.Exists(RootDirectory))
                    Directory.CreateDirectory(RootDirectory);
                return true;
            } catch (IOException ex) {
                RaiseExceptionFilter(ex, "Failed to create storage directory.");
                return false;
            } catch (UnauthorizedAccessException ex) {
                RaiseExceptionFilter(ex, "Failed to create storage directory.");
                return false;
            }
        }

        public event EventHandler<ExceptionFilterEventArgs> ExceptionFilter;

        private void RaiseExceptionFilter(Exception exception, string message)
        {
            ExceptionFilter?.Invoke(this, new ExceptionFilterEventArgs(exception, message));
        }
    }

    public interface ITraceSettingsService
    {
        IReadOnlyCollection<TraceSessionSettingsViewModel> Sessions { get; }
        void Save(TraceSettingsViewModel sessions);
    }

    public sealed class TraceSettingsService : ITraceSettingsService
    {
        private readonly string fileName = "TraceSettings.xml";
        private readonly SettingsSerializer serializer = new SettingsSerializer();
        private readonly List<TraceSessionSettingsViewModel> sessions =
            new List<TraceSessionSettingsViewModel>();

        public TraceSettingsService(string rootDirectory)
        {
            RootDirectory = rootDirectory;
        }

        private string RootDirectory { get; }

        public IReadOnlyCollection<TraceSessionSettingsViewModel> Sessions => sessions;

        public void Load()
        {
            if (!EnsureLocalStorageDirectoryExists())
                return;
            var settings = LoadFromLocalStorage();
            if (settings != null)
                sessions.AddRange(settings.Sessions);
        }

        public void Save(TraceSettingsViewModel settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            if (EnsureLocalStorageDirectoryExists()) {
                SaveToLocalStorage(settings);
            }
        }

        private TraceSettingsViewModel LoadFromLocalStorage()
        {
            try {
                using (Stream stream = OpenLocalStorage(FileAccess.Read))
                    return Load(stream);
            } catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex)) {
                RaiseExceptionFilter(ex, "Failed to load trace settings.");
                return null;
            }
        }

        private TraceSettingsViewModel Load(Stream stream)
        {
            return serializer.Load<TraceSettingsViewModel>(stream);
        }

        private void SaveToLocalStorage(TraceSettingsViewModel settings)
        {
            try {
                using (Stream stream = OpenLocalStorage(FileAccess.Write))
                    serializer.Save(settings, stream);
            } catch (IOException ex) {
                RaiseExceptionFilter(ex, "Failed to save trace settings.");
            } catch (UnauthorizedAccessException ex) {
                RaiseExceptionFilter(ex, "Failed to save trace settings.");
            }
        }

        private Stream OpenLocalStorage(FileAccess fileAccess)
        {
            FileMode mode = fileAccess == FileAccess.Read ? FileMode.Open : FileMode.Create;
            FileShare share = fileAccess == FileAccess.Read ? FileShare.Read : FileShare.None;
            return File.Open(GetFullPath(fileName), mode, fileAccess, share);
        }

        private string GetFullPath(string profileName)
        {
            return Path.Combine(RootDirectory, profileName);
        }

        private bool EnsureLocalStorageDirectoryExists()
        {
            try {
                if (!Directory.Exists(RootDirectory))
                    Directory.CreateDirectory(RootDirectory);
                return true;
            } catch (IOException ex) {
                RaiseExceptionFilter(ex, "Failed to create trace settings storage directory.");
                return false;
            } catch (UnauthorizedAccessException ex) {
                RaiseExceptionFilter(ex, "Failed to create trace settings storage directory.");
                return false;
            }
        }

        public event EventHandler<ExceptionFilterEventArgs> ExceptionFilter;

        private void RaiseExceptionFilter(Exception exception, string message)
        {
            ExceptionFilter?.Invoke(this, new ExceptionFilterEventArgs(exception, message));
        }
    }

    public interface IViewPresetService
    {
        AdvmPresetCollection Presets { get; }
        event EventHandler<ExceptionFilterEventArgs> ExceptionFilter;
        void SaveToStorage();
    }

    public sealed class ViewPresetService : IViewPresetService
    {
        private readonly ReaderWriterLockSlim rwLock =
            new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        private readonly FileSettingsStorage storage;

        public ViewPresetService(FileSettingsStorage storage)
        {
            this.storage = storage;
        }

        public AdvmPresetCollection Presets { get; private set; }

        public event EventHandler<ExceptionFilterEventArgs> ExceptionFilter
        {
            add => storage.ExceptionFilter += value;
            remove => storage.ExceptionFilter -= value;
        }

        public void LoadFromStorage()
        {
            ViewPresets presets = storage.Load();
            if (presets != null)
                Presets = Deserialize(presets);

            if (Presets == null) {
                Presets = new AdvmPresetCollection();
                SaveToStorage();
            }
        }

        public void SaveToStorage()
        {
            ViewPresets viewPresets = Serialize(Presets);
            if (viewPresets == null)
                return;

            Task.Run(() => {
                if (rwLock.TryEnterWriteLock(2000)) {
                    try {
                        storage.Save(viewPresets);
                    } finally {
                        rwLock.ExitWriteLock();
                    }
                }
            });
        }

        private ViewPresets Serialize(AdvmPresetCollection presetCollection)
        {
            var shaper = new SerializationShaper<SettingsElement>();

            if (!shaper.TrySerialize(presetCollection, out ViewPresets viewPresets))
                return null;

            return viewPresets;
        }

        private static AdvmPresetCollection Deserialize(ViewPresets viewPresets)
        {
            var shaper = new SerializationShaper<SettingsElement>();

            if (viewPresets != null && shaper.TryDeserialize(viewPresets, out AdvmPresetCollection presets)
                                    && presets != null)
                return presets;

            return new AdvmPresetCollection();
        }
    }
}
