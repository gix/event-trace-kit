namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Windows;
    using Collections;
    using Controls;
    using Windows;

    public class AsyncDataViewModel : DependencyObject
    {
        private readonly WorkManager workManager;
        private readonly IDataView dataView;
        private readonly AsyncDataGridColumnsViewModel columnsViewModel;

        private bool isInitializedWithFirstPreset;
        private bool shouldApplyPreset;
        private AsyncDataViewModelPreset presetBeingApplied;
        private AsyncDataViewModelPreset presetToApplyOnReady;
        private bool refreshViewModelFromModelOnReady;
        private bool refreshViewModelOnUpdateRequest;

        internal CancellationTokenSource readCancellationTokenSource;
        private readonly ManualResetEvent asyncReadQueueComplete;
        private readonly object asyncReadWorkQueueLock;
        private bool allowBackgroundThreads;

        public AsyncDataViewModel(
            IDataView dataView,
            AsyncDataViewModelPreset templatePreset,
            AsyncDataViewModelPreset defaultPreset,
            AdvViewModelPresetCollection presetCollection)
        {
            if (dataView == null)
                throw new ArgumentNullException(nameof(dataView));
            if (templatePreset == null)
                throw new ArgumentNullException(nameof(templatePreset));

            this.dataView = dataView;
            PresetCollection = presetCollection;
            TemplatePreset = templatePreset.Clone().EnsureFrozen();

            workManager = new WorkManager(Dispatcher);

            asyncReadQueueComplete = new ManualResetEvent(true);
            asyncReadWorkQueueLock = new object();
            allowBackgroundThreads = true;

            columnsViewModel = new AsyncDataGridColumnsViewModel(this);
            GridViewModel = new AsyncDataGridViewModel(this, columnsViewModel);

            IsReady = false;

            dataView.RowCountChanged += OnRowCountChanged;

            Preset = defaultPreset;
        }

        public AsyncDataViewModelPreset TemplatePreset { get; }

        public event ValueChangedEventHandler<AsyncDataViewModelPreset> PresetChanged;

        private readonly ActionThrottler rowCountChangedThrottler =
            new ActionThrottler(TimeSpan.FromMilliseconds(100));

        private void OnRowCountChanged(object sender, EventArgs eventArgs)
        {
            rowCountChangedThrottler.Run(
                () => workManager.UIThreadTaskFactory.StartNew(() => RaiseUpdate(false)));
        }

        public AsyncDataGridViewModel GridViewModel { get; }

        #region public AsyncDataViewModelPreset Preset

        public static readonly DependencyProperty PresetProperty =
            DependencyProperty.Register(
                nameof(Preset),
                typeof(AsyncDataViewModelPreset),
                typeof(AsyncDataViewModel),
                new PropertyMetadata(
                    null,
                    (d, e) => ((AsyncDataViewModel)d).OnPresetChanged(e),
                    (d, v) => ((AsyncDataViewModel)d).CoercePreset(v)));

        public AsyncDataViewModelPreset Preset
        {
            get { return (AsyncDataViewModelPreset)GetValue(PresetProperty); }
            set { SetValue(PresetProperty, value); }
        }

        private void OnPresetChanged(DependencyPropertyChangedEventArgs e)
        {
            var oldPreset = (AsyncDataViewModelPreset)e.OldValue;
            var newPreset = (AsyncDataViewModelPreset)e.NewValue;

            if (newPreset != null && (!isInitializedWithFirstPreset || shouldApplyPreset)) {
                if (!newPreset.IsFrozen)
                    throw new ArgumentException("Preset must be frozen before being applied");

                if (!IsReady && !isInitializedWithFirstPreset) {
                    IsReady = true;
                    isInitializedWithFirstPreset = true;
                }
                if (!IsReady) {
                    var b = presetToApplyOnReady == null &&
                            presetBeingApplied != null &&
                            !presetBeingApplied.Equals(newPreset);
                    var b1 = presetToApplyOnReady != null &&
                             !presetToApplyOnReady.Equals(newPreset);
                    if (b || b1) {
                        presetToApplyOnReady = newPreset;
                    }
                    return;
                }
                //this.UpdateHelpTextFromPreset(preset.HelpText);
                //ColumnChangingContext columnChangingContext = AsyncDataViewModel.columnChangingContext;
                //if (columnChangingContext != null) {
                //    this.columnMetadataCollection.PresetMetadataEntries = preset.ColumnMetadataEntries;
                //    if (this.HasAnyVisibleColumnAffectedByChange(preset, columnChangingContext.ColumnChangingPredicate)) {
                //        this.DisableTableForAsyncOperation();
                //        this.presetToApplyOnReady = preset;
                //        AddChangingAdvModel(this);
                //        return;
                //    }
                //}
                presetBeingApplied = newPreset;
                //if (this.presenterRowSelectionToUseOnReady == null) {
                //    this.SaveTableRowSelectionAndFocus();
                //}

                //this.DataView.BeginDataUpdate();
                ApplyPresetToGridModel(
                    newPreset,
                    () => ContinuePresetAfterGridModelInSync(newPreset));
            }

            if (oldPreset != null && newPreset != null &&
                newPreset.IsModified && newPreset.Name == oldPreset.Name)
                PresetCollection.CachePreset(newPreset);

            PresetChanged.Raise(this, e);
            //this.ResetIsPresetError();
        }

        private object CoercePreset(object baseValue)
        {
            var preset = (AsyncDataViewModelPreset)baseValue;
            if (preset == null)
                return null;

            //bool includeDynamicColumns = this.IsUnmodifiedBuiltInPreset(preset);
            var compatiblePreset = preset.CreateCompatiblePreset(TemplatePreset);

            shouldApplyPreset = !compatiblePreset.IsUIModified;
            compatiblePreset.IsUIModified = false;
            compatiblePreset.Freeze();
            //this.cachedPreset = compatiblePreset;
            return compatiblePreset;
        }

        #endregion

        #region public bool IsReady { get; private set; }

        private static readonly DependencyPropertyKey IsReadyPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsReady),
                typeof(bool),
                typeof(AsyncDataViewModel),
                new PropertyMetadata(Boxed.True, OnIsReadyChanged));

        public static readonly DependencyProperty IsReadyProperty = IsReadyPropertyKey.DependencyProperty;

        public bool IsReady
        {
            get { return (bool)GetValue(IsReadyProperty); }
            private set { SetValue(IsReadyPropertyKey, Boxed.Bool(value)); }
        }

        private static void OnIsReadyChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AsyncDataViewModel)d).OnIsReadyChanged(e.NewValue);
        }

        private void OnIsReadyChanged(object newValue)
        {
        }

        #endregion

        public bool RequestUpdate(bool refreshViewModelFromModel = true)
        {
            if (!IsReady) {
                refreshViewModelOnUpdateRequest |= refreshViewModelFromModel;
                return false;
            }

            RaiseUpdate(refreshViewModelFromModel);
            return true;
        }

        public event ItemEventHandler<bool> Updated;

        public void RaiseUpdate(bool refreshViewModelFromModel = true)
        {
            VerifyAccess();

            Updated?.Invoke(this, new ItemEventArgs<bool>(refreshViewModelFromModel));
            GridViewModel?.RaiseUpdated(refreshViewModelFromModel);
        }

        internal void EnableTableAfterAsyncOperation()
        {
            IsReady = true;
            allowBackgroundThreads = true;
        }

        internal void DisableTableForAsyncOperation()
        {
            IsReady = false;
        }

        private void ApplyPresetToGridModel(
            AsyncDataViewModelPreset preset, Action callbackOnComplete)
        {
            if (!IsReady)
                ExceptionUtils.ThrowInvalidOperationException(
                    "Must be ready before applying preset to grid model.");

            DisableTableForAsyncOperation();

            DataColumnViewInfo[] columnViewInfos = GetDataColumnViewInfosFromPreset(preset).ToArray();
            WaitForReadOperationToComplete();

            WorkManager.BackgroundTaskFactory.StartNew(delegate {
                dataView.BeginDataUpdate();
                dataView.ApplyColumnView(columnViewInfos);
                //this.columnViewReady = true;
                dataView.EndDataUpdate();
            }).ContinueWith(t => callbackOnComplete(), WorkManager.UIThreadTaskScheduler);
        }

        private IEnumerable<DataColumnViewInfo> GetDataColumnViewInfosFromPreset(
            AsyncDataViewModelPreset preset)
        {
            foreach (ColumnViewModelPreset columnPreset in preset.ConfigurableColumns)
                yield return GetDataColumnViewInfoFromPreset(columnPreset);
        }

        private DataColumnViewInfo GetDataColumnViewInfoFromPreset(
            ColumnViewModelPreset columnPreset)
        {
            var info = new DataColumnViewInfo {
                ColumnId = columnPreset.Id,
                Name = columnPreset.Name,
                HelpText = columnPreset.HelpText,
                IsVisible = columnPreset.IsVisible,
                Format = columnPreset.CellFormat,
            };
            return info;
        }

        internal void WaitForReadOperationToComplete()
        {
            WaitForReadOperationToComplete(-1, null);
        }

        internal void WaitForReadOperationToComplete(
            int spinIntervalMilliseconds, Action stillWaitingCallback)
        {
            allowBackgroundThreads = false;
            readCancellationTokenSource?.Cancel();

            while (!asyncReadQueueComplete.WaitOne(spinIntervalMilliseconds))
                stillWaitingCallback?.Invoke();
        }

        private void ContinuePresetAfterGridModelInSync(AsyncDataViewModelPreset preset)
        {
            bool ignoreInitialSelection = false;
            columnsViewModel.ApplyPresetAssumeGridModelInSync(preset);
            ContinueAsyncOperation(
                () => CompleteApplyPreset(ignoreInitialSelection), true);
        }

        private void CompleteApplyPreset(bool ignoreInitialSelection)
        {

            //if (!this.DataView.EndDataUpdate()) {
            //    ExceptionUtils.ThrowInternalErrorException("Expected data update nesting depth to be 0");
            //}

            if (!ignoreInitialSelection) {
                //this.ReapplyInitialSelection();
            }
            CompleteAsyncOrSyncUpdate();
        }

        internal WorkManager WorkManager => workManager;

        public IDataView DataView => dataView;

        private void ContinueAsyncOperation(Action callback, bool refreshViewModelFromModelOnReady = true)
        {
            if (IsReady)
                ExceptionUtils.ThrowInvalidOperationException(
                    "The system is ready, you aren't continuing an async operation?");
            this.refreshViewModelFromModelOnReady |= refreshViewModelFromModelOnReady;
            WorkManager.BackgroundTaskFactory.StartNew(callback);
        }

        private void CompleteAsyncOrSyncUpdate()
        {
            WorkManager.UIThread.Send(() => CompleteAsyncUpdate());
        }

        private bool CompleteAsyncUpdate()
        {
            bool flag = false;
            //this.IsTableRefreshing = true;
            EnableTableAfterAsyncOperation();
            //if (this.checkForColumnChangingOnReady) {
            //    this.checkForColumnChangingOnReady = false;
            //    ColumnChangingContext columnChangingContext = AsyncDataViewModel.columnChangingContext;
            //    AsyncDataViewModelPreset preset = presetBeingApplied ?? presetToApplyOnReady;
            //    if (this.HasAnyVisibleColumnAffectedByChange(preset, columnChangingContext.ColumnChangingPredicate)) {
            //        this.DisableTableForAsyncOperation();
            //        AddChangingAdvModel(this);
            //        flag = true;
            //    }
            //    countBusyAdvModels--;
            //    CompleteUpdateWhenAllReady();
            //}
            if (!flag && (presetToApplyOnReady != null)) {
                Preset = presetToApplyOnReady;
                presetToApplyOnReady = null;
                flag = true;
            }

            //if (!flag && (this.advModelToCopyStateOnReady != null)) {
            //    this.TryCopyAllStateFrom(this.advModelToCopyStateOnReady);
            //    this.advModelToCopyStateOnReady = null;
            //    flag = true;
            //}

            //if (flag) {
            //    if (this.selectionBookmarkToApplyOnReady != null) {
            //        this.selectionBookmarkToApplyOnReady.ConvertRowsToRowIds();
            //    }
            //    if (this.focusBookmarkToApplyOnReady != null) {
            //        this.focusBookmarkToApplyOnReady.ConvertRowsToRowIds();
            //    }
            //} else {
            //    flag = this.RestoresSelectionAndFocusFromBookmarksAsync();
            //}

            if (!flag) {
                //this.UpdateDynamicHeader();
                bool refreshViewModelFromModel = refreshViewModelFromModelOnReady && IsReady;
                if (refreshViewModelFromModel)
                    refreshViewModelFromModelOnReady = false;

                bool flag3 = RequestUpdate(refreshViewModelFromModel);
                if (refreshViewModelFromModel && !flag3) {
                    ExceptionUtils.ThrowInternalErrorException("We should have sent an update for the advmodel, but didn't");
                }
                //this.IsTableRefreshing = false;
            }
            return !flag;
        }

        internal CellValue GetCellValue(int rowIndex, int visibleColumnIndex)
        {
            if (workManager.UIThread.CheckAccess()) {
                return workManager.BackgroundTaskFactory.StartNew(() => {
                    var value = dataView.GetCellValue(rowIndex, visibleColumnIndex);
                    value.PrecomputeString();
                    return value;
                }).Result;
            } else {
                var value = dataView.GetCellValue(rowIndex, visibleColumnIndex);
                value.PrecomputeString();
                return value;
            }
        }

        internal AsyncDataViewModelPreset CreatePresetFromModifiedUI()
        {
            VerifyIsReady();

            var newPreset = new AsyncDataViewModelPreset();
            newPreset.Name = Preset.Name;
            newPreset.IsModified = true;
            newPreset.ConfigurableColumns.Clear();
            newPreset.ConfigurableColumns.AddRange(
                from column in columnsViewModel.WritableColumns
                where column.IsConnected
                select column.ToPreset());
            newPreset.LeftFrozenColumnCount = columnsViewModel.LeftFrozenColumnCount;
            newPreset.RightFrozenColumnCount = columnsViewModel.RightFrozenColumnCount;

            return newPreset;
        }

        public void VerifyIsReady()
        {
            if (!IsReady)
                ExceptionUtils.ThrowInvalidOperationException(
                    "AsyncDataViewModel needs to be ready for this operation");
        }

        internal void OnUIPropertyChanged(
            AsyncDataGridColumn column, DependencyPropertyChangedEventArgs args)
        {
            columnsViewModel?.OnUIPropertyChanged(column, args);
        }

        internal void PerformAsyncReadOperation(Action<CancellationToken> callback)
        {
            if (TryBeginReadOperation()) {
                WorkManager.BackgroundTaskFactory.StartNew(() => {
                    callback(readCancellationTokenSource.Token);
                    CompleteAsyncRead();
                });
            } else {
                var cancelledToken = new CancellationTokenSource();
                cancelledToken.Cancel();
                WorkManager.BackgroundTaskFactory.StartNew(() => callback(cancelledToken.Token));
            }
        }

        internal bool TryBeginReadOperation()
        {
            VerifyAccess();
            if (!allowBackgroundThreads)
                ExceptionUtils.ThrowInvalidOperationException("Cannot perform background operation in this state.");

            lock (asyncReadWorkQueueLock) {
                if (countReadOperationInProgress == 0) {
                    if (readCancellationTokenSource != null)
                        ExceptionUtils.ThrowInvalidOperationException("The async read count is out of sync with the token source");

                    readCancellationTokenSource = new CancellationTokenSource();
                    asyncReadQueueComplete.Reset();
                }

                ++countReadOperationInProgress;
            }

            return true;
        }

        private int countReadOperationInProgress;

        private void CompleteAsyncRead()
        {
            if (WorkManager.UIThread.CheckAccess())
                ExceptionUtils.ThrowInternalErrorException("Must not invoke this method from the UI thread");

            lock (asyncReadWorkQueueLock) {
                if (countReadOperationInProgress == 0)
                    ExceptionUtils.ThrowInternalErrorException("There was no read operation to complete.");

                --countReadOperationInProgress;
                if (countReadOperationInProgress == 0) {
                    readCancellationTokenSource.Dispose();
                    readCancellationTokenSource = null;
                    asyncReadQueueComplete.Set();
                }
            }
        }

        internal object DataValidityToken => DataView.DataValidityToken;

        private static readonly DependencyPropertyKey PresetCollectionPropertyKey =
                 DependencyProperty.RegisterReadOnly(
                     "Presets",
                     typeof(AdvViewModelPresetCollection),
                     typeof(AsyncDataViewModel),
                     PropertyMetadataUtils.DefaultNull);

        public static readonly DependencyProperty PresetCollectionProperty =
            PresetCollectionPropertyKey.DependencyProperty;

        public AdvViewModelPresetCollection PresetCollection
        {
            get { return (AdvViewModelPresetCollection)GetValue(PresetCollectionProperty); }
            private set { SetValue(PresetCollectionPropertyKey, value); }
        }

        public bool IsValidDataValidityToken(object dataValidityToken)
        {
            return DataView.IsValidDataValidityToken(dataValidityToken);
        }

        public DataColumnView GetPrototypeViewForColumnPreset(ColumnViewModelPreset columnPreset)
        {
            DataColumnViewInfo columnViewInfo = GetDataColumnViewInfoFromPreset(columnPreset);
            return dataView.CreateDataColumnViewFromInfo(columnViewInfo);
        }
    }
}
