namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading;
    using System.Windows;
    using Windows;
    using Controls;

    public sealed class PersistenceManager
    {
        private static PersistenceManager persistenceManager;

        public HdvPresetCollections PresetCache;
        private bool hasUnsavedChanges;
        private DependencyPropertyDescriptor hdvViewModelDPD;

        private PersistenceManager()
        {
            this.PresetCache = new HdvPresetCollections();
            this.hdvViewModelDPD = DependencyPropertyDescriptor.FromProperty(DataTableGraphTreeItem.HdvViewModelProperty, typeof(DataTableGraphTreeItem));
            this.hasUnsavedChanges = false;
            //WeakEventManager<WpaApplication, UISessionEventArgs>.AddHandler(WpaApplication.Current, "UISessionAdded", new EventHandler<UISessionEventArgs>(this.onUISessionAdded));
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
            HdvViewModelPresetCollection presetCollection = this.tryGetHdvViewModelPresetCollection(datasourceId);
            if (presetCollection != null) {
                return this.tryGetPreset(presetCollection, presetName);
            }
            return null;
        }

        public bool HasCachedVersion(Guid datasourceId, string presetName)
        {
            if (string.IsNullOrEmpty(presetName)) {
                presetName = "default";
            }
            HdvViewModelPresetCollection presetCollection = this.tryGetHdvViewModelPresetCollection(datasourceId);
            return ((presetCollection != null) && (this.tryGetPreset(presetCollection, presetName) != null));
        }

        public void CacheModifiedPreset(Guid datasourceId, AsyncDataViewModelPreset preset)
        {
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));

            if (!preset.IsModified)
                return;

            HdvViewModelPresetCollection presets = tryGetHdvViewModelPresetCollection(datasourceId);
            if (presets == null)
                presets = new HdvViewModelPresetCollection(datasourceId.ToString());

            AsyncDataViewModelPreset unmodifiedPreset = this.TryGetCachedVersion(datasourceId, preset.Name);
            //if (((unmodifiedPreset == null) && (WpaApplication.Current.HdvPresetCollections != null)) && (WpaApplication.Current.HdvPresetCollections[datasourceId] != null)) {
            //    unmodifiedPreset = WpaApplication.Current.HdvPresetCollections[datasourceId].TryGetPresetByName(preset.Name);
            //}

            this.RemoveCachedVersion(datasourceId, preset.Name, false);
            presets.PersistedPresets.Add(preset);
            this.PresetCache.PresetCollections.Add(presets);
            var view = PresetCollectionManagerView.Get();
            if (view != null) {
                view.FindAndRefreshPersistedPreset(datasourceId.ToString(), unmodifiedPreset, preset);
            }
        }

        public bool RemoveCachedVersion(Guid datasourceId, string presetName)
        {
            return this.RemoveCachedVersion(datasourceId, presetName, true);
        }

        private AsyncDataViewModelPreset tryGetPreset(HdvViewModelPresetCollection presetCollection, string presetName)
        {
            return presetCollection.PersistedPresets.FirstOrDefault(p => (p.Name == presetName));
        }

        private HdvViewModelPresetCollection tryGetHdvViewModelPresetCollection(Guid datasourceId)
        {
            return
                this.PresetCache.PresetCollections.FirstOrDefault(
                    pc => (new Guid(pc.Name) == datasourceId));
        }

        private bool RemoveCachedVersion(Guid datasourceId, string presetName, bool removeFromRepo)
        {
            if (string.IsNullOrEmpty(presetName)) {
                presetName = "default";
            }
            HdvViewModelPresetCollection presetCollection = this.tryGetHdvViewModelPresetCollection(datasourceId);
            if (presetCollection != null) {
                AsyncDataViewModelPreset preset = this.tryGetPreset(presetCollection, presetName);
                if (preset != null) {
                    presetCollection.PersistedPresets.Remove(preset);
                    var view = PresetCollectionManagerView.Get();
                    if ((view != null) && removeFromRepo) {
                        HdvPresetCollections collection = view.FindCollectionForGraphAndPreset(presetCollection.Name, preset);
                        view.RemovePersistedPresetByName(collection, presetCollection.Name, preset.Name);
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
                EventHandler handler = null;
                handler = (sender, args) => {
                    hdvViewModelDPD.RemoveValueChanged(dtGti, handler);
                    action(dtGti);
                };
                hdvViewModelDPD.AddValueChanged(dtGti, handler);
            } else {
                action(dtGti);
            }
        }
        public bool TryApplyModifiedPreset(DataTableGraphTreeItem dtGti)
        {
            if (dtGti == null) {
                throw new ArgumentNullException("dtGti");
            }
            var preset = this.TryGetCachedVersion(dtGti.DataSourceID, dtGti.HdvViewModel.Preset.Name);
            if (preset != null) {
                dtGti.HdvViewModel.Preset = preset;
                return true;
            }
            return false;
        }

        public void Attach(DataTableGraphTreeItem dtGti)
        {
            this.checkHdvViewModelThenExecute(dtGti, delegate {
                this.TryApplyModifiedPreset(dtGti);
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
                this.CacheModifiedPreset(Guid.Empty, newPreset);
            }
        }
    }

    public sealed class PresetCollectionManagerView
    {
        private static PresetCollectionManagerView instance;

        public PresetCollectionManagerView()
        {
            this.allProfiles.Add(
                new HdvPresetCollectionsWrapper("My Presets", new HdvPresetCollections()));
        }

        public static PresetCollectionManagerView Get()
        {
            if (instance == null)
                return instance = new PresetCollectionManagerView();
            return instance;
        }

        public void SavePresetToRepository(
            string graphId, AsyncDataViewModelPreset preset, bool isPersistedPreset, string originalPresetName)
        {
            Guid guid;
            HdvPresetCollections collection = this.FindCollectionForGraphAndPreset(graphId, preset);
            this.PresetRepository[graphId].SetUserPreset(preset);
            if (collection != this.PresetRepository) {
                this.RemoveUserPresetByName(collection, graphId, originalPresetName);
            }
            if (isPersistedPreset && Guid.TryParse(graphId, out guid)) {
                PersistenceManager.PersistenceManger.RemoveCachedVersion(guid, originalPresetName);
            }
            this.SaveRepo();
        }

        public void RemoveUserPresetByName(HdvPresetCollections collection, string graphId, string presetName)
        {
            if ((collection != null) && (collection[graphId] != null)) {
                collection[graphId].DeleteUserPresetByName(presetName);
                this.CheckEmptyAndRemoveGraph(collection, graphId);
                if (collection == this.PresetRepository) {
                    this.SaveRepo();
                }
            }
        }

        public void FindAndRefreshPersistedPreset(
            string graphId, AsyncDataViewModelPreset unmodifiedPreset, AsyncDataViewModelPreset modifiedPreset)
        {
            if (unmodifiedPreset == null)
                return;

            HdvPresetCollections collections = this.FindCollectionForGraphAndPreset(graphId, unmodifiedPreset);
            if (collections == null)
                return;

            //if (((WpaApplication.Current != null) && (WpaApplication.Current.HdvPresetCollections != null)) && (WpaApplication.Current.HdvPresetCollections[graphId] != null)) {
            //    bool flag = WpaApplication.Current.HdvPresetCollections[graphId].TryGetUserPresetByName(unmodifiedPreset.Name) > null;
            //    bool flag2 = collections[graphId].TryGetUserPresetByName(unmodifiedPreset.Name) > null;
            //    if (WpaApplication.Current.HdvPresetCollections[graphId].TryGetBuiltInPresetByName(unmodifiedPreset.Name) <= null) {
            //        if (!flag) {
            //            WpaApplication.Current.HdvPresetCollections.MergeInUserPreset(graphId, unmodifiedPreset);
            //        }
            //        if (!flag2) {
            //            collections.MergeInUserPreset(graphId, unmodifiedPreset);
            //            this.SaveRepo();
            //        }
            //    }
            //}

            //collections.MergeInPersistedPreset(graphId, modifiedPreset);
        }

        private void SaveRepo()
        {
            this.SaveRepo(false);
        }

        private ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private ObservableCollection<HdvPresetCollectionsWrapper> allProfiles = new ObservableCollection<HdvPresetCollectionsWrapper>();

        private void SaveRepo(bool includePersistedPresets)
        {
            //WpaProfile2 repositoryProfile;
            //string repoPath = TryGetWpaPresetRepoPath();
            //if (repoPath != null) {
            //    repositoryProfile = this.ExtractProfile(this.PresetRepository.PresetCollections, includePersistedPresets);
            //    if (repositoryProfile != null) {
            //        WpaApplication.Current.WorkManager.BackgroundThread.Post(delegate {
            //            if (this.rwLock.TryEnterWriteLock(2000)) {
            //                try {
            //                    WpaProfileSerializer.SerializeProfile(repositoryProfile, repoPath, true, false);
            //                } finally {
            //                    this.rwLock.ExitWriteLock();
            //                }
            //            }
            //        });
            //    }
            //}
        }

        public HdvPresetCollections FindCollectionForGraphAndPreset(
            string graphId, AsyncDataViewModelPreset preset)
        {
            if (preset == null) {
                return null;
            }
            using (IEnumerator<HdvPresetCollectionsWrapper> enumerator = this.allProfiles.GetEnumerator()) {
                while (enumerator.MoveNext()) {
                    HdvViewModelPresetCollection presets;
                    HdvPresetCollections collections = enumerator.Current.Collections;
                    if (collections.TryGetValue(graphId, out presets) && (presets.ContainsPreset(preset) || (presets.TryGetPersistedPresetByName(preset.Name) != null))) {
                        return collections;
                    }
                }
            }
            return this.PresetRepository;
        }

        internal HdvPresetCollections PresetRepository
        {
            get
            {
                HdvPresetCollectionsWrapper wrapper = this.allProfiles.FirstOrDefault();
                if (wrapper == null) {
                    return null;
                }
                return wrapper.Collections;
            }
        }


        public void RemovePersistedPresetByName(HdvPresetCollections collection, string graphId, string presetName)
        {
            if (collection != null && collection[graphId] != null) {
                collection[graphId].DeletePersistedPresetByName(presetName);
                this.CheckEmptyAndRemoveGraph(collection, graphId);
                if (collection == this.PresetRepository) {
                    this.SaveRepo();
                }
            }
        }

        private void CheckEmptyAndRemoveGraph(HdvPresetCollections collection, string graphId)
        {
            if ((collection[graphId].UserPresets.Count == 0) && (collection[graphId].PersistedPresets.Count == 0)) {
                collection.PresetCollections.Remove(collection[graphId]);
            }
            if ((collection != this.PresetRepository) && (collection.PresetCollections.Count == 0)) {
                this.allProfiles.RemoveWhere(pc => pc.Collections == collection);
            }
        }
    }

    public class HdvPresetCollectionsWrapper
    {
        public HdvPresetCollectionsWrapper(
            string name, HdvPresetCollections collections)
        {
            this.Name = name;
            this.Collections = collections;
        }


        public HdvPresetCollections Collections { get; private set; }
        public string Name { get; private set; }
    }

    public class HdvPresetCollections : DependencyObject
    {
        public static readonly DependencyProperty PresetCollectionsProperty =
              DependencyProperty.Register(
                  nameof(PresetCollections),
                  typeof(FreezableCollection<HdvViewModelPresetCollection>),
                  typeof(HdvPresetCollections),
                  PropertyMetadataUtils.DefaultNull);

        public HdvPresetCollections()
        {
            PresetCollections = new FreezableCollection<HdvViewModelPresetCollection>();
        }

        public FreezableCollection<HdvViewModelPresetCollection> PresetCollections
        {
            get
            {
                return (FreezableCollection<HdvViewModelPresetCollection>)base.GetValue(PresetCollectionsProperty);
            }
            set
            {
                base.SetValue(PresetCollectionsProperty, value);
            }
        }
        public HdvViewModelPresetCollection this[Guid guid]
        {
            get
            {
                base.VerifyAccess();
                return this[guid.ToString()];
            }
        }
        public HdvViewModelPresetCollection this[string name]
        {
            get
            {
                HdvViewModelPresetCollection presets;
                base.VerifyAccess();
                if (!this.TryGetValue(name, out presets)) {
                    presets = new HdvViewModelPresetCollection(name);
                    this.PresetCollections.Add(presets);
                }
                return presets;
            }
        }

        public bool TryGetValue(string name, out HdvViewModelPresetCollection result)
        {
            base.VerifyAccess();
            foreach (HdvViewModelPresetCollection presets in this.PresetCollections) {
                string str;
                Guid guid;
                if (Guid.TryParse(presets.Name, out guid)) {
                    str = guid.ToString();
                } else {
                    str = presets.Name;
                }
                if (str.Equals(name, StringComparison.InvariantCultureIgnoreCase)) {
                    result = presets;
                    return true;
                }
            }
            result = null;
            return false;
        }
    }
}
