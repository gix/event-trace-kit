namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Windows;
    using System.Xml;
    using Collections;
    using Controls;
    using Microsoft.Internal.VisualStudio.Shell;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.PlatformUI;
    using Serialization;
    using Settings;
    using Windows;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Task = System.Threading.Tasks.Task;

    public sealed class PersistenceManager
    {
        private static PersistenceManager persistenceManager;

        public HdvPresetCollections PresetCache;
        private bool hasUnsavedChanges;
        private DependencyPropertyDescriptor hdvViewModelDPD;

        private PersistenceManager()
        {
            PresetCache = new HdvPresetCollections();
            //hdvViewModelDPD = DependencyPropertyDescriptor.FromProperty(DataTableGraphTreeItem.HdvViewModelProperty, typeof(DataTableGraphTreeItem));
            hasUnsavedChanges = false;
            //WeakEventManager<WpaApplication, UISessionEventArgs>.AddHandler(WpaApplication.Current, "UISessionAdded", new EventHandler<UISessionEventArgs>(this.onUISessionAdded));
        }

        public bool HasUnsavedChages => this.hasUnsavedChanges;

        public void MergePersistedCollections(HdvPresetCollections hdvPresetCollections)
        {
            if ((hdvPresetCollections != null) && (hdvPresetCollections.PresetCollections != null)) {
                foreach (HdvViewModelPresetCollection presets in hdvPresetCollections.PresetCollections) {
                    if ((presets != null) && (presets.PersistedPresets != null)) {
                        foreach (var preset in presets.PersistedPresets) {
                            this.CacheModifiedPreset(Guid.Empty, preset);
                        }
                    }
                }
            }
        }

        public static PersistenceManager PersistenceManger
        {
            get
            {
                if (persistenceManager == null) {
                    persistenceManager = new PersistenceManager();
                }
                return persistenceManager;
            }
        }

        public AsyncDataViewModelPreset TryGetCachedVersion(Guid datasourceId, string presetName)
        {
            HdvViewModelPresetCollection presetCollection = tryGetHdvViewModelPresetCollection(datasourceId);
            if (presetCollection != null) {
                return tryGetPreset(presetCollection, presetName);
            }
            return null;
        }

        public bool HasCachedVersion(Guid datasourceId, string presetName)
        {
            if (string.IsNullOrEmpty(presetName)) {
                presetName = "default";
            }
            HdvViewModelPresetCollection presetCollection = tryGetHdvViewModelPresetCollection(datasourceId);
            return ((presetCollection != null) && (tryGetPreset(presetCollection, presetName) != null));
        }

        public void CacheModifiedPreset(Guid datasourceId, AsyncDataViewModelPreset preset)
        {
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));

            if (!preset.IsModified)
                return;

            HdvViewModelPresetCollection presets = tryGetHdvViewModelPresetCollection(datasourceId);
            if (presets == null)
                presets = new HdvViewModelPresetCollection();

            AsyncDataViewModelPreset unmodifiedPreset = TryGetCachedVersion(datasourceId, preset.Name);
            if (((unmodifiedPreset == null) && (WpaApplication.Current.PresetCollections != null)) && (WpaApplication.Current.PresetCollections[datasourceId] != null)) {
                unmodifiedPreset = WpaApplication.Current.PresetCollections[datasourceId].TryGetPresetByName(preset.Name);
            }

            RemoveCachedVersion(datasourceId, preset.Name, false);
            presets.PersistedPresets.Add(preset);
            PresetCache.PresetCollections.Add(presets);
            var view = PresetCollectionManagerView.Get();
            if (view != null) {
                view.FindAndRefreshPersistedPreset(null, unmodifiedPreset, preset);
            }
        }

        public bool RemoveCachedVersion(Guid datasourceId, string presetName)
        {
            return RemoveCachedVersion(datasourceId, presetName, true);
        }

        private AsyncDataViewModelPreset tryGetPreset(HdvViewModelPresetCollection presetCollection, string presetName)
        {
            return presetCollection.PersistedPresets.FirstOrDefault(p => (p.Name == presetName));
        }

        private HdvViewModelPresetCollection tryGetHdvViewModelPresetCollection(Guid datasourceId)
        {
            return PresetCache.PresetCollections.FirstOrDefault();
        }

        private bool RemoveCachedVersion(Guid datasourceId, string presetName, bool removeFromRepo)
        {
            if (string.IsNullOrEmpty(presetName)) {
                presetName = "default";
            }
            HdvViewModelPresetCollection presetCollection = tryGetHdvViewModelPresetCollection(datasourceId);
            if (presetCollection != null) {
                AsyncDataViewModelPreset preset = tryGetPreset(presetCollection, presetName);
                if (preset != null) {
                    presetCollection.PersistedPresets.Remove(preset);
                    var view = PresetCollectionManagerView.Get();
                    if ((view != null) && removeFromRepo) {
                        HdvPresetCollections collection = view.FindCollectionForGraphAndPreset(null, preset);
                        view.RemovePersistedPresetByName(collection, null, preset.Name);
                    }
                    return true;
                }
            }
            return false;
        }

        private void checkHdvViewModelThenExecute(
            DataTableGraphTreeItem dtGti, Action<DataTableGraphTreeItem> action)
        {
            if (dtGti.HdvViewModel == null) {
                ValueChangedEventHandler<AsyncDataViewModelPreset> handler = null;
                handler = (sender, args) => {
                    dtGti.PresetChanged -= handler;
                    //hdvViewModelDPD.RemoveValueChanged(dtGti, handler);
                    action(dtGti);
                };
                dtGti.PresetChanged += handler;
                //hdvViewModelDPD.AddValueChanged(dtGti, handler);
            } else {
                action(dtGti);
            }
        }
        public bool TryApplyModifiedPreset(DataTableGraphTreeItem dtGti)
        {
            if (dtGti == null) {
                throw new ArgumentNullException(nameof(dtGti));
            }
            var preset = TryGetCachedVersion(dtGti.DataSourceID, dtGti.HdvViewModel.Preset.Name);
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
                //setDataTableGraphTreeItem(dtGti.HdvViewModel, dtGti);
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
            DataTableGraphTreeItem dtGti = null; //getDataTableGraphTreeItem((AsyncDataViewModel)sender);
            var oldPreset = e.OldValue;
            var newPreset = e.NewValue;
            if (//dtGti != null &&
                oldPreset != null &&
                newPreset != null && newPreset.IsModified &&
                newPreset.Name == oldPreset.Name) {
                CacheModifiedPreset(Guid.Empty, newPreset);
            }
        }
    }

    public sealed class PresetCollectionManagerView
    {
        private readonly ProfileCache cache = new ProfileCache();
        private static PresetCollectionManagerView instance;

        public PresetCollectionManagerView()
        {
            InitRepo();
        }

        public event EventHandler<ExceptionFilterEventArgs> ExceptionFilter
        {
            add { cache.ExceptionFilter += value; }
            remove { cache.ExceptionFilter -= value; }
        }

        public static PresetCollectionManagerView Get()
        {
            if (instance == null)
                return instance = new PresetCollectionManagerView();
            return instance;
        }

        private void InitRepo()
        {
            ViewPresets profile = cache.Load();
            if (profile != null) {
                allProfiles.Add(new HdvPresetCollectionsWrapper(
                    "My Presets",
                    WpaProfileSerializer.DeserializePresetCollections(profile)));
            } else {
                allProfiles.Add(
                    new HdvPresetCollectionsWrapper("My Presets", new HdvPresetCollections()));
                allProfiles.Add(new HdvPresetCollectionsWrapper("My Presets", new HdvPresetCollections()));
                SaveRepo();
            }
        }

        public void UpdateRepo(WorkManager workManager)
        {
            if (WpaApplication.Current == null)
                return;

            Task.Run(delegate {
                if (!rwLock.TryEnterReadLock(1000))
                    return;

                HdvPresetCollections newRepoGraphs = null;
                try {
                    ViewPresets profile = cache.Load();
                    if (profile != null) {
                        newRepoGraphs = WpaProfileSerializer.DeserializePresetCollections(profile);
                        newRepoGraphs.Freeze();
                    }
                } catch (Exception ex) {
                    return;
                } finally {
                    rwLock.ExitReadLock();
                }

                if (newRepoGraphs != null) {
                    workManager.UIThread.Post(delegate {
                        MergeInPresets(newRepoGraphs, this.PresetRepository, true);
                        if ((WpaApplication.Current != null) && (WpaApplication.Current.PresetCollections != null)) {
                            MergeInPresets(newRepoGraphs, WpaApplication.Current.PresetCollections, true);
                        }
                    });
                }
            });
        }

        private static void MergeInPresets(HdvPresetCollections source, HdvPresetCollections dest, bool includePersistedPresets)
        {
            MergeInPresets(source.PresetCollections, dest, includePersistedPresets);
        }

        private static void MergeInPresets(IEnumerable<HdvViewModelPresetCollection> source, HdvPresetCollections dest, bool includePersistedPresets)
        {
            dest.MergeInUserPresets(source);
            if (includePersistedPresets) {
                dest.MergeInPersistedPresets(source);
            }
        }

        public void SavePresetToRepository(
            AsyncDataViewModelPreset preset, bool isPersistedPreset, string originalPresetName)
        {
            string graphId = string.Empty;
            HdvPresetCollections collection = FindCollectionForGraphAndPreset(graphId, preset);
            PresetRepository[graphId].SetUserPreset(preset);
            if (collection != PresetRepository)
                RemoveUserPresetByName(collection, graphId, originalPresetName);

            if (isPersistedPreset) {
                PersistenceManager.PersistenceManger.RemoveCachedVersion(Guid.Empty, originalPresetName);
            }
            SaveRepo();
        }

        public void RemoveUserPresetByName(HdvPresetCollections collection, string graphId, string presetName)
        {
            if ((collection != null) && (collection[graphId] != null)) {
                collection[graphId].DeleteUserPresetByName(presetName);
                CheckEmptyAndRemoveGraph(collection, graphId);
                if (collection == PresetRepository) {
                    SaveRepo();
                }
            }
        }

        public void FindAndRefreshPersistedPreset(
            string graphId, AsyncDataViewModelPreset unmodifiedPreset, AsyncDataViewModelPreset modifiedPreset)
        {
            if (unmodifiedPreset == null)
                return;

            HdvPresetCollections collections = FindCollectionForGraphAndPreset(graphId, unmodifiedPreset);
            if (collections == null)
                return;

            if (((WpaApplication.Current != null) && (WpaApplication.Current.PresetCollections != null)) && (WpaApplication.Current.PresetCollections[graphId] != null)) {
                bool flag = WpaApplication.Current.PresetCollections[graphId].TryGetUserPresetByName(unmodifiedPreset.Name) != null;
                var b1 = WpaApplication.Current.PresetCollections[graphId].TryGetBuiltInPresetByName(unmodifiedPreset.Name) != null;
                bool flag2 = collections[graphId].TryGetUserPresetByName(unmodifiedPreset.Name) != null;
                if (!b1) {
                    if (!flag) {
                        WpaApplication.Current.PresetCollections.MergeInUserPreset(graphId, unmodifiedPreset);
                    }
                    if (!flag2) {
                        collections.MergeInUserPreset(graphId, unmodifiedPreset);
                        SaveRepo();
                    }
                }
            }

            collections.MergeInPersistedPreset(graphId, modifiedPreset);
        }

        private void SaveRepo()
        {
            SaveRepo(false);
        }

        private ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private ObservableCollection<HdvPresetCollectionsWrapper> allProfiles =
            new ObservableCollection<HdvPresetCollectionsWrapper>();

        private void SaveRepo(bool includePersistedPresets)
        {
            includePersistedPresets = true;
            ViewPresets repositoryProfile;
            //string repoPath = TryGetWpaPresetRepoPath();
            //if (repoPath != null) {
            repositoryProfile = this.ExtractProfile(PresetRepository.PresetCollections, includePersistedPresets);
            if (repositoryProfile != null) {
                Task.Run(delegate {
                    if (rwLock.TryEnterWriteLock(2000)) {
                        try {
                            cache.Save(repositoryProfile);
                            //WpaProfileSerializer.SerializeProfile(repositoryProfile, repoPath, true, false);
                        } finally {
                            rwLock.ExitWriteLock();
                        }
                    }
                });
            }
            //}
        }

        private ViewPresets ExtractProfile(IEnumerable<HdvViewModelPresetCollection> graphs, bool includePersistedPresets)
        {
            return ExtractGraphsForSerialization(graphs, includePersistedPresets).FirstOrDefault();
        }

        private IEnumerable<ViewPresets> ExtractGraphsForSerialization(IEnumerable<HdvViewModelPresetCollection> graphs, bool includePersistedPresets)
        {
            var shaper = new SerializationShaper<SettingsElement>();
            List<ViewPresets> list = new List<ViewPresets>();
            using (IEnumerator<HdvViewModelPresetCollection> enumerator = (from hdvmpc in graphs
                                                                           where hdvmpc.UserPresets.Any() || (includePersistedPresets && hdvmpc.PersistedPresets.Any())
                                                                           select hdvmpc).GetEnumerator()) {
                while (enumerator.MoveNext()) {
                    ViewPresets schema;
                    if (!shaper.TrySerialize(enumerator.Current, out schema)) {
                        Console.Error.WriteLine("Failed to serialize Preset Collection");
                    } else {
                        if (!includePersistedPresets) {
                            schema.PersistedPresets.Clear();
                        }
                        list.Add(schema);
                    }
                }
            }
            return list;
        }

        public HdvPresetCollections FindCollectionForGraphAndPreset(
            string graphId, AsyncDataViewModelPreset preset)
        {
            if (preset == null)
                return null;

            using (IEnumerator<HdvPresetCollectionsWrapper> enumerator = allProfiles.GetEnumerator()) {
                while (enumerator.MoveNext()) {
                    HdvViewModelPresetCollection presets;
                    HdvPresetCollections collections = enumerator.Current.Collections;
                    if (collections.TryGetValue(graphId, out presets) && (presets.ContainsPreset(preset) || (presets.TryGetPersistedPresetByName(preset.Name) != null))) {
                        return collections;
                    }
                }
            }

            return PresetRepository;
        }

        internal HdvPresetCollections PresetRepository
        {
            get
            {
                HdvPresetCollectionsWrapper wrapper = allProfiles.FirstOrDefault();
                return wrapper?.Collections;
            }
        }


        public void RemovePersistedPresetByName(HdvPresetCollections collection, string graphId, string presetName)
        {
            if (collection != null && collection[graphId] != null) {
                collection[graphId].DeletePersistedPresetByName(presetName);
                CheckEmptyAndRemoveGraph(collection, graphId);
                if (collection == PresetRepository) {
                    SaveRepo();
                }
            }
        }

        private void CheckEmptyAndRemoveGraph(HdvPresetCollections collection, string graphId)
        {
            if ((collection[graphId].UserPresets.Count == 0) && (collection[graphId].PersistedPresets.Count == 0)) {
                collection.PresetCollections.Remove(collection[graphId]);
            }
            if ((collection != PresetRepository) && (collection.PresetCollections.Count == 0)) {
                allProfiles.RemoveWhere(pc => pc.Collections == collection);
            }
        }

        public void UpdateRepoFileWithPersistedPresets()
        {
            this.SaveRepo(true);
        }
    }

    internal class WpaProfileSerializer
    {
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
    }

    public class ProfileCache
    {
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

        private string ProfileRootDirectory
        {
            get
            {
                var shell = (IVsShell)Package.GetGlobalService(typeof(SVsShell));
                object obj2;
                ErrorHandler.ThrowOnFailure(shell.GetProperty((int)__VSSPROPID.VSSPROPID_AppDataDir, out obj2));
                return Path.Combine((string)obj2, "EventTraceKit");
            }
        }

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

    public class HdvPresetCollectionsWrapper
    {
        public HdvPresetCollectionsWrapper(
            string name, HdvPresetCollections collections)
        {
            Name = name;
            Collections = collections;
        }


        public HdvPresetCollections Collections { get; private set; }
        public string Name { get; private set; }
    }

    public class HdvPresetCollections
        : FreezableCustomSerializerAccessBase //, IComparable<HdvPresetCollections>
    {
        public static readonly DependencyProperty PresetCollectionsProperty =
              DependencyProperty.Register(
                  nameof(PresetCollections),
                  typeof(FreezableCollection<HdvViewModelPresetCollection>),
                  typeof(HdvPresetCollections),
                  PropertyMetadataUtils.DefaultNull);

        protected override Freezable CreateInstanceCore()
        {
            return new HdvPresetCollections();
        }

        public HdvPresetCollections()
        {
            PresetCollections = new FreezableCollection<HdvViewModelPresetCollection>();
        }

        public FreezableCollection<HdvViewModelPresetCollection> PresetCollections
        {
            get
            {
                return (FreezableCollection<HdvViewModelPresetCollection>)GetValue(PresetCollectionsProperty);
            }
            set
            {
                SetValue(PresetCollectionsProperty, value);
            }
        }
        public HdvViewModelPresetCollection this[Guid guid]
        {
            get
            {
                VerifyAccess();
                return this[guid == Guid.Empty ? null : guid.ToString()];
            }
        }
        public HdvViewModelPresetCollection this[string name]
        {
            get
            {
                HdvViewModelPresetCollection presets;
                VerifyAccess();
                if (!TryGetValue(name, out presets)) {
                    presets = new HdvViewModelPresetCollection();
                    PresetCollections.Add(presets);
                }
                return presets;
            }
        }
        public void MergeInPersistedPresets(IEnumerable<HdvViewModelPresetCollection> viewModelPresetCollection)
        {
            if (viewModelPresetCollection == null) {
                throw new ArgumentNullException("viewModelPresetCollection");
            }
            foreach (HdvViewModelPresetCollection presets in viewModelPresetCollection) {
                HdvViewModelPresetCollection collection1 = this[null];
                collection1.BeginDeferChangeNotifications();
                collection1.SetPersistedPresets(presets.PersistedPresets);
                collection1.EndDeferChangeNotifications();
            }
        }

        public void MergeInUserPresets(IEnumerable<HdvViewModelPresetCollection> viewModelPresetCollection)
        {
            if (viewModelPresetCollection == null) {
                throw new ArgumentNullException("viewModelPresetCollection");
            }
            foreach (HdvViewModelPresetCollection presets in viewModelPresetCollection) {
                HdvViewModelPresetCollection presets2 = this[null];
                presets2.BeginDeferChangeNotifications();
                presets2.SetUserPresets(presets.UserPresets);
                presets2.EndDeferChangeNotifications();
            }
        }

        public void AddOrUpdate(HdvViewModelPresetCollection collection)
        {
            if (collection == null) {
                throw new ArgumentNullException("collection");
            }
            HdvViewModelPresetCollection result = null;
            if (this.TryGetValue(null, out result)) {
                result.BeginDeferChangeNotifications();
                result.UserPresets.Clear();
                result.BuiltInPresets.Clear();
                result.SetUserPresets(collection.UserPresets);
                result.BuiltInPresets.AddRange(collection.BuiltInPresets);
                result.EndDeferChangeNotifications();
            } else {
                this.PresetCollections.Add(collection);
            }
        }

        public void MergeInBuiltInPresets(IEnumerable<HdvViewModelPresetCollection> viewModelPresetCollection)
        {
            if (viewModelPresetCollection == null)
                throw new ArgumentNullException(nameof(viewModelPresetCollection));
            foreach (HdvViewModelPresetCollection presets in viewModelPresetCollection) {
                var collection = this[null];
                collection.BeginDeferChangeNotifications();
                collection.BuiltInPresets.AddRange(presets.BuiltInPresets);
                collection.EndDeferChangeNotifications();
            }
        }

        public void MergeInUserPreset(string graphId, AsyncDataViewModelPreset preset)
        {
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));
            HdvViewModelPresetCollection collection = this[graphId];
            collection.BeginDeferChangeNotifications();
            collection.SetUserPreset(preset);
            collection.EndDeferChangeNotifications();
        }

        public void MergeInPersistedPreset(string graphId, AsyncDataViewModelPreset preset)
        {
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));
            HdvViewModelPresetCollection collection = this[graphId];
            collection.BeginDeferChangeNotifications();
            collection.SetPersistedPreset(preset);
            collection.EndDeferChangeNotifications();
        }

        public bool TryGetValue(string name, out HdvViewModelPresetCollection result)
        {
            VerifyAccess();
            foreach (HdvViewModelPresetCollection presets in PresetCollections) {
                if (string.IsNullOrEmpty(name)) {
                    result = presets;
                    return true;
                }
            }
            result = null;
            return false;
        }
    }
}
