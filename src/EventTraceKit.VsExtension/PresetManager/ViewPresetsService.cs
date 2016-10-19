namespace EventTraceKit.VsExtension
{
    using System;
    using System.IO;
    using System.Threading;
    using Microsoft.Internal.VisualStudio.Shell;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.PlatformUI;
    using Serialization;
    using Settings;
    using Task = System.Threading.Tasks.Task;

    public class ViewPresetsService
    {
        public ViewPresetsService(string rootDirectory)
        {
            ProfileRootDirectory = rootDirectory;
        }

        public void Save(ViewPresets profile)
        {
            Validate.IsNotNull(profile, "profile");
            if (EnsureLocalStorageDirectoryExists())
                SaveProfileToLocalStorage(profile);
        }

        public ViewPresets Load()
        {
            return LoadProfileFromLocalStorage("ViewPresets.xml");
        }

        private string ProfileRootDirectory { get; }

        private ViewPresets LoadProfileFromLocalStorage(string profileName)
        {
            try {
                using (Stream stream = OpenProfileLocalStorage(profileName, FileAccess.Read))
                    return LoadProfile(stream);
            } catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex)) {
                RaiseExceptionFilter(ex, "Failed to load window configuration.");
                return null;
            }
        }

        private ViewPresets LoadProfile(Stream stream)
        {
            var serializer = new SettingsSerializer();
            return serializer.Load<ViewPresets>(stream);
        }

        private void SaveProfileToLocalStorage(ViewPresets profile)
        {
            try {
                using (Stream stream = OpenProfileLocalStorage("ViewPresets.xml", FileAccess.Write)) {
                    var serializer = new SettingsSerializer();
                    serializer.Save(profile, stream);
                }
            } catch (IOException ex) {
                RaiseExceptionFilter(ex, "Failed to save window configuration.");
            } catch (UnauthorizedAccessException ex) {
                RaiseExceptionFilter(ex, "Failed to save window configuration.");
            }
        }

        public Stream OpenProfileLocalStorage(string profileName, FileAccess fileAccess)
        {
            ThrowIfProfileNameInvalid(profileName);
            FileMode mode = fileAccess == FileAccess.Read ? FileMode.Open : FileMode.Create;
            FileShare share = fileAccess == FileAccess.Read ? FileShare.Read : FileShare.None;
            return File.Open(GetProfileFullPath(profileName), mode, fileAccess, share);
        }

        private string GenerateProfileFileName(string profileName)
        {
            return Path.Combine(ProfileRootDirectory, profileName);
        }

        private string GetProfileFullPath(string profileName)
        {
            return GenerateProfileFileName(profileName);
        }

        private void ThrowIfProfileNameInvalid(string str)
        {
            str.ThrowIfNullOrEmpty("Window profile name cannot be null or empty.");
        }

        private bool EnsureLocalStorageDirectoryExists()
        {
            try {
                if (!Directory.Exists(ProfileRootDirectory))
                    Directory.CreateDirectory(ProfileRootDirectory);
                return true;
            } catch (IOException ex) {
                RaiseExceptionFilter(ex, "Failed to create window configuration storage directory.");
                return false;
            } catch (UnauthorizedAccessException ex) {
                RaiseExceptionFilter(ex, "Failed to create window configuration storage directory.");
                return false;
            }
        }

        public event EventHandler<ExceptionFilterEventArgs> ExceptionFilter;

        private void RaiseExceptionFilter(Exception exception, string message)
        {
            ExceptionFilter?.Invoke(this, new ExceptionFilterEventArgs(exception, message));
        }
    }

    public sealed class PresetCollectionManagerView
    {
        private readonly ReaderWriterLockSlim rwLock =
            new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        private readonly ViewPresetsService cache;

        public PresetCollectionManagerView(ViewPresetsService cache)
        {
            this.cache = cache;
            InitRepo();
        }

        private void InitRepo()
        {
            ViewPresets profile = cache.Load();
            if (profile != null)
                Presets = DeserializePresetCollections(profile);

            if (Presets == null) {
                Presets = new AdvViewModelPresetCollection();
                SaveRepo();
            }
        }

        public AdvViewModelPresetCollection Presets { get; private set; }

        public event EventHandler<ExceptionFilterEventArgs> ExceptionFilter
        {
            add { cache.ExceptionFilter += value; }
            remove { cache.ExceptionFilter -= value; }
        }

        private static AdvViewModelPresetCollection DeserializePresetCollections(ViewPresets viewPresets)
        {
            var shaper = new SerializationShaper<SettingsElement>();

            AdvViewModelPresetCollection presets;
            if (viewPresets != null && shaper.TryDeserialize(viewPresets, out presets) && presets != null)
                return presets;

            return new AdvViewModelPresetCollection();
        }

        public void SaveRepo()
        {
            var viewPresets = Convert(Presets);
            if (viewPresets == null)
                return;

            Task.Run(() => {
                if (rwLock.TryEnterWriteLock(2000)) {
                    try {
                        cache.Save(viewPresets);
                    } finally {
                        rwLock.ExitWriteLock();
                    }
                }
            });
        }

        private ViewPresets Convert(AdvViewModelPresetCollection presetCollection)
        {
            var shaper = new SerializationShaper<SettingsElement>();

            ViewPresets viewPresets;
            if (!shaper.TrySerialize(presetCollection, out viewPresets)) {
                Console.Error.WriteLine("Failed to serialize preset collection.");
                return null;
            }

            return viewPresets;
        }
    }
}
