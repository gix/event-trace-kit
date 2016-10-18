namespace EventTraceKit.VsExtension
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Windows;
    using Collections;
    using Controls;
    using Microsoft.Internal.VisualStudio.Shell;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.PlatformUI;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Serialization;
    using Settings;
    using Windows;
    using Task = System.Threading.Tasks.Task;

    public sealed class PersistenceManager
    {
        private static PersistenceManager persistenceManager;
        private HdvViewModelPresetCollection persistencePresetCollections;

        private PersistenceManager()
        {
        }

        public static PersistenceManager Instance =>
            persistenceManager ?? (persistenceManager = new PersistenceManager());

        public void MergePersistedCollections(HdvPresetCollections presetCollections)
        {
            var presets = presetCollections?.PresetCollections;
            if (presets?.PersistedPresets != null) {
                foreach (var preset in presets.PersistedPresets)
                    CacheModifiedPreset(preset);
            }
        }

        public AsyncDataViewModelPreset TryGetCachedVersion(string presetName)
        {
            var presetCollection = persistencePresetCollections;
            if (presetCollection != null)
                return TryGetPreset(presetCollection, presetName);

            return null;
        }

        public bool HasCachedVersion(string presetName)
        {
            if (string.IsNullOrEmpty(presetName))
                presetName = "default";

            var presetCollection = persistencePresetCollections;
            return presetCollection != null && TryGetPreset(presetCollection, presetName) != null;
        }

        public void CacheModifiedPreset(AsyncDataViewModelPreset preset)
        {
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));

            if (!preset.IsModified)
                return;

            var presets = persistencePresetCollections;
            if (presets == null) {
                presets = new HdvViewModelPresetCollection();
                persistencePresetCollections = presets;
            }

            AsyncDataViewModelPreset unmodifiedPreset = TryGetCachedVersion(preset.Name);
            if (unmodifiedPreset == null) {
                unmodifiedPreset = PresetCollectionManagerView.Instance.PresetRepository.PresetCollectionsNotNull.TryGetPresetByName(preset.Name);
            }

            RemoveCachedVersion(preset.Name, false);
            presets.PersistedPresets.Add(preset);

            PresetCollectionManagerView.Instance.FindAndRefreshPersistedPreset(unmodifiedPreset, preset);
        }

        public bool RemoveCachedVersion(string presetName)
        {
            return RemoveCachedVersion(presetName, true);
        }

        private AsyncDataViewModelPreset TryGetPreset(
            HdvViewModelPresetCollection presetCollection, string presetName)
        {
            return presetCollection.PersistedPresets.FirstOrDefault(p => p.Name == presetName);
        }

        private bool RemoveCachedVersion(string presetName, bool removeFromRepo)
        {
            if (string.IsNullOrEmpty(presetName))
                presetName = "default";

            var presetCollection = persistencePresetCollections;
            if (presetCollection == null)
                return false;

            var preset = TryGetPreset(presetCollection, presetName);
            if (preset == null)
                return false;

            presetCollection.PersistedPresets.Remove(preset);
            if (removeFromRepo)
                PresetCollectionManagerView.Instance.RemovePersistedPresetByName(preset.Name);

            return true;
        }

        private void checkHdvViewModelThenExecute(
            DataTableGraphTreeItem dtGti, Action<DataTableGraphTreeItem> action)
        {
            if (dtGti.HdvViewModel == null) {
                ValueChangedEventHandler<AsyncDataViewModelPreset> handler = null;
                handler = (sender, args) => {
                    dtGti.PresetChanged -= handler;
                    action(dtGti);
                };
                dtGti.PresetChanged += handler;
            } else {
                action(dtGti);
            }
        }

        public bool TryApplyModifiedPreset(DataTableGraphTreeItem dtGti)
        {
            if (dtGti == null) {
                throw new ArgumentNullException(nameof(dtGti));
            }
            var preset = TryGetCachedVersion(dtGti.HdvViewModel.Preset.Name);
            if (preset != null) {
                dtGti.HdvViewModel.Preset = preset;
                return true;
            }

            return false;
        }

        public void Attach(DataTableGraphTreeItem dtGti)
        {
            checkHdvViewModelThenExecute(dtGti, delegate {
                TryApplyModifiedPreset(dtGti);
                WeakEventManager<AsyncDataViewModel, ValueChangedEventArgs<AsyncDataViewModelPreset>>.AddHandler(
                    dtGti.HdvViewModel,
                    nameof(AsyncDataViewModel.PresetChanged),
                    onHdvViewModelPresetChanged);
            });
        }

        private void onHdvViewModelPresetChanged(
            object sender, ValueChangedEventArgs<AsyncDataViewModelPreset> e)
        {
            if (sender == null)
                return;

            //if (!WpaProfileSerializer.IsApplyingProfile) {
            //    this.hasUnsavedChanges = true;
            //}
            var oldPreset = e.OldValue;
            var newPreset = e.NewValue;
            if (oldPreset != null &&
                newPreset != null && newPreset.IsModified &&
                newPreset.Name == oldPreset.Name) {
                CacheModifiedPreset(newPreset);
            }
        }
    }

    public sealed class PresetCollectionManagerView
    {
        private static PresetCollectionManagerView instance;

        private readonly ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly ViewPresetsService cache = new ViewPresetsService();
        private HdvPresetCollections allProfiles;

        public PresetCollectionManagerView()
        {
            InitRepo();
        }

        public event EventHandler<ExceptionFilterEventArgs> ExceptionFilter
        {
            add { cache.ExceptionFilter += value; }
            remove { cache.ExceptionFilter -= value; }
        }

        public static PresetCollectionManagerView Instance
        {
            get
            {
                if (instance == null)
                    return instance = new PresetCollectionManagerView();
                return instance;
            }
        }

        private void InitRepo()
        {
            ViewPresets profile = cache.Load();
            if (profile != null) {
                var c = DeserializePresetCollections(profile);
                allProfiles = c;
            } else {
                allProfiles = new HdvPresetCollections();
                SaveRepo();
            }
        }

        internal static HdvPresetCollections DeserializePresetCollections(ViewPresets serializedGraphSchema)
        {
            var shaper = new SerializationShaper<SettingsElement>();
            HdvPresetCollections collections = new HdvPresetCollections();
            if (serializedGraphSchema != null) {
                HdvViewModelPresetCollection presets;
                if (shaper.TryDeserialize(serializedGraphSchema, out presets) && (presets != null)) {
                    collections.AddOrUpdate(presets);
                }
            }
            return collections;
        }

        private static void MergeInPresets(HdvPresetCollections source, HdvPresetCollections dest, bool includePersistedPresets)
        {
            MergeInPresets(source.PresetCollections, dest, includePersistedPresets);
        }

        private static void MergeInPresets(HdvViewModelPresetCollection source, HdvPresetCollections dest, bool includePersistedPresets)
        {
            dest.MergeInUserPresets(source);
            if (includePersistedPresets) {
                dest.MergeInPersistedPresets(source);
            }
        }

        public void SavePresetToRepository(
            AsyncDataViewModelPreset preset, bool isPersistedPreset, string originalPresetName)
        {
            PresetRepository.PresetCollectionsNotNull.SetUserPreset(preset);

            if (isPersistedPreset)
                PersistenceManager.Instance.RemoveCachedVersion(originalPresetName);

            SaveRepo();
        }

        public void FindAndRefreshPersistedPreset(
            AsyncDataViewModelPreset unmodifiedPreset, AsyncDataViewModelPreset modifiedPreset)
        {
            if (unmodifiedPreset == null)
                return;

            var repository = PresetRepository;
            if (repository == null)
                return;

            var collection = repository.PresetCollectionsNotNull;

            bool isBuiltIn = collection.TryGetBuiltInPresetByName(unmodifiedPreset.Name) != null;
            bool isUser = collection.TryGetUserPresetByName(unmodifiedPreset.Name) != null;
            if (!isBuiltIn && !isUser) {
                repository.MergeInUserPreset(unmodifiedPreset);
                SaveRepo();
            }

            repository.MergeInPersistedPreset(modifiedPreset);
        }

        private void SaveRepo()
        {
            var repositoryProfile = ExtractProfile(PresetRepository.PresetCollections);
            if (repositoryProfile != null) {
                Task.Run(delegate {
                    if (rwLock.TryEnterWriteLock(2000)) {
                        try {
                            cache.Save(repositoryProfile);
                        } finally {
                            rwLock.ExitWriteLock();
                        }
                    }
                });
            }
        }

        private ViewPresets ExtractProfile(HdvViewModelPresetCollection graphs)
        {
            var shaper = new SerializationShaper<SettingsElement>();

            if (!graphs.UserPresets.Any() && !graphs.PersistedPresets.Any())
                return null;

            ViewPresets schema;
            if (!shaper.TrySerialize(graphs, out schema)) {
                Console.Error.WriteLine("Failed to serialize Preset Collection");
            } else {
                return schema;
            }

            return null;
        }

        internal HdvPresetCollections PresetRepository => allProfiles;

        public void RemovePersistedPresetByName(string presetName)
        {
            PresetRepository.PresetCollectionsNotNull.DeletePersistedPresetByName(presetName);
            CheckEmptyAndRemoveGraph(PresetRepository);
            SaveRepo();
        }

        private void CheckEmptyAndRemoveGraph(HdvPresetCollections collection)
        {
        }

        public void UpdateRepoFileWithPersistedPresets()
        {
            SaveRepo();
        }
    }

    public class ViewPresetsService
    {
        public ViewPresetsService()
            : this(GetRootDirectory())
        {
        }

        private static string GetRootDirectory()
        {
            var shell = (IVsShell)Package.GetGlobalService(typeof(SVsShell));
            object obj2;
            ErrorHandler.ThrowOnFailure(shell.GetProperty((int)__VSSPROPID.VSSPROPID_AppDataDir, out obj2));
            return Path.Combine((string)obj2, "EventTraceKit");
        }

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

    public class ExceptionFilterEventArgs : EventArgs
    {
        public ExceptionFilterEventArgs(Exception exception, string message)
        {
            Exception = exception;
            Message = message;
        }

        public Exception Exception { get; private set; }

        public string Message { get; private set; }
    }

    public class HdvPresetCollections
        : FreezableCustomSerializerAccessBase
    {
        public static readonly DependencyProperty PresetCollectionsProperty =
              DependencyProperty.Register(
                  nameof(PresetCollections),
                  typeof(HdvViewModelPresetCollection),
                  typeof(HdvPresetCollections),
                  PropertyMetadataUtils.DefaultNull);

        protected override Freezable CreateInstanceCore()
        {
            return new HdvPresetCollections();
        }

        public HdvViewModelPresetCollection PresetCollections
        {
            get { return (HdvViewModelPresetCollection)GetValue(PresetCollectionsProperty); }
            set { SetValue(PresetCollectionsProperty, value); }
        }

        public HdvViewModelPresetCollection PresetCollectionsNotNull
        {
            get
            {
                VerifyAccess();

                var presets = PresetCollections;
                if (presets != null)
                    return presets;

                presets = new HdvViewModelPresetCollection();
                PresetCollections = presets;
                return presets;
            }
        }

        public void MergeInBuiltInPresets(HdvViewModelPresetCollection source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var collection = PresetCollectionsNotNull;
            collection.BeginDeferChangeNotifications();
            collection.BuiltInPresets.AddRange(source.BuiltInPresets);
            collection.EndDeferChangeNotifications();
        }

        public void MergeInUserPreset(AsyncDataViewModelPreset preset)
        {
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));

            var collection = PresetCollectionsNotNull;
            collection.BeginDeferChangeNotifications();
            collection.SetUserPreset(preset);
            collection.EndDeferChangeNotifications();
        }

        public void MergeInPersistedPreset(AsyncDataViewModelPreset preset)
        {
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));

            var collection = PresetCollectionsNotNull;
            collection.BeginDeferChangeNotifications();
            collection.SetPersistedPreset(preset);
            collection.EndDeferChangeNotifications();
        }

        public void MergeInPersistedPresets(HdvViewModelPresetCollection source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var presets = PresetCollectionsNotNull;
            presets.BeginDeferChangeNotifications();
            presets.SetPersistedPresets(source.PersistedPresets);
            presets.EndDeferChangeNotifications();
        }

        public void MergeInUserPresets(HdvViewModelPresetCollection source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var presets = PresetCollectionsNotNull;
            presets.BeginDeferChangeNotifications();
            presets.SetUserPresets(source.UserPresets);
            presets.EndDeferChangeNotifications();
        }

        public void AddOrUpdate(HdvViewModelPresetCollection collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            HdvViewModelPresetCollection result = PresetCollections;
            if (result == null) {
                PresetCollections = collection;
                return;
            }

            result.BeginDeferChangeNotifications();
            result.UserPresets.Clear();
            result.BuiltInPresets.Clear();
            result.SetUserPresets(collection.UserPresets);
            result.BuiltInPresets.AddRange(collection.BuiltInPresets);
            result.EndDeferChangeNotifications();
        }
    }
}
