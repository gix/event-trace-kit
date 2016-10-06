namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Windows;
    using EventTraceKit.VsExtension.Collections;
    using EventTraceKit.VsExtension.Controls;
    using Serialization;

    [SerializedShape(typeof(Settings.ProfilePreset))]
    public class DataViewViewModel : DependencyObject
    {
        private readonly WorkManager workManager;
        private readonly IDataView dataView;
        private readonly AsyncDataGridColumnsViewModel columnsViewModel;

        private bool isInitializedWithFirstPreset;
        private bool shouldApplyPreset;
        private HdvViewModelPreset presetBeingApplied;
        private HdvViewModelPreset presetToApplyOnReady;
        private bool refreshViewModelFromModelOnReady;
        private bool refreshViewModelOnUpdateRequest;

        internal CancellationTokenSource readCancellationTokenSource;
        private readonly ManualResetEvent asyncReadQueueComplete;
        private readonly object asyncReadWorkQueueLock;
        private bool allowBackgroundThreads;

        public DataViewViewModel(IDataView dataView)
        {
            if (dataView == null)
                throw new ArgumentNullException(nameof(dataView));

            this.dataView = dataView;
            workManager = new WorkManager(Dispatcher);

            asyncReadQueueComplete = new ManualResetEvent(true);
            asyncReadWorkQueueLock = new object();
            allowBackgroundThreads = true;

            columnsViewModel = new AsyncDataGridColumnsViewModel(this);
            GridViewModel = new AsyncDataGridViewModel(
                this, columnsViewModel);

            IsReady = false;

            dataView.RowCountChanged += OnRowCountChanged;
        }

        private readonly ActionThrottler rowCountChangedThrottler =
            new ActionThrottler(TimeSpan.FromMilliseconds(100));

        private void OnRowCountChanged(object sender, EventArgs eventArgs)
        {
            rowCountChangedThrottler.Run(
                () => workManager.UIThreadTaskFactory.StartNew(() => RaiseUpdate(false)));
        }

        public AsyncDataGridViewModel GridViewModel { get; }

        #region public HdvViewModelPreset HdvViewModelPreset

        public static readonly DependencyProperty HdvViewModelPresetProperty =
            DependencyProperty.Register(
                nameof(HdvViewModelPreset),
                typeof(HdvViewModelPreset),
                typeof(DataViewViewModel),
                new PropertyMetadata(
                    null,
                    (s, e) => ((DataViewViewModel)s).HdvViewModelPresetPropertyChanged(e),
                    (d, e) => ((DataViewViewModel)d).CoerceHdvViewModelPresetProperty(e)));

        public HdvViewModelPreset HdvViewModelPreset
        {
            get { return (HdvViewModelPreset)GetValue(HdvViewModelPresetProperty); }
            set { SetValue(HdvViewModelPresetProperty, value); }
        }

        private void HdvViewModelPresetPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            var preset = (HdvViewModelPreset)e.NewValue;
            if (preset != null && (!isInitializedWithFirstPreset || shouldApplyPreset)) {
                if (!preset.IsFrozen)
                    throw new ArgumentException("Preset must be frozen before being applied");

                if (!IsReady && !isInitializedWithFirstPreset) {
                    IsReady = true;
                    isInitializedWithFirstPreset = true;
                }
                if (!IsReady) {
                    var b = presetToApplyOnReady == null &&
                            presetBeingApplied != null &&
                            !presetBeingApplied.Equals(preset);
                    var b1 = presetToApplyOnReady != null &&
                             !presetToApplyOnReady.Equals(preset);
                    if (b || b1) {
                        presetToApplyOnReady = preset;
                    }
                    return;
                }
                //this.UpdateHelpTextFromPreset(preset.HelpText);
                //ColumnChangingContext columnChangingContext = DataViewViewModel.columnChangingContext;
                //if (columnChangingContext != null) {
                //    this.columnMetadataCollection.PresetMetadataEntries = preset.ColumnMetadataEntries;
                //    if (this.HasAnyVisibleColumnAffectedByChange(preset, columnChangingContext.ColumnChangingPredicate)) {
                //        this.DisableTableForAsyncOperation();
                //        this.presetToApplyOnReady = preset;
                //        AddChangingHdvViewModel(this);
                //        return;
                //    }
                //}
                presetBeingApplied = preset;
                //if (this.presenterRowSelectionToUseOnReady == null) {
                //    this.SaveTableRowSelectionAndFocus();
                //}

                //this.DataView.BeginDataUpdate();
                ApplyPresetToGridModel(
                    preset,
                    () => ContinuePresetAfterGridModelInSync(preset));
            }
            //this.HdvViewModelPresetChanged.Raise<HdvViewModelPreset>(this, e);
            //this.ResetIsPresetError();
        }

        private object CoerceHdvViewModelPresetProperty(object baseValue)
        {
            var preset = (HdvViewModelPreset)baseValue;
            if (preset == null)
                return null;

            //bool includeDynamicColumns = this.IsUnmodifiedBuiltInPreset(preset);
            //HdvViewModelPreset compatiblePreset = preset.CreateCompatiblePreset(
            //    this.templatePreset, this.viewCreationInfoCollection, includeDynamicColumns);
            HdvViewModelPreset compatiblePreset = preset;

            shouldApplyPreset = !compatiblePreset.IsUIModified;
            compatiblePreset.IsUIModified = false;
            compatiblePreset.Freeze();
            //this.cachedHdvViewModelPreset = compatiblePreset;
            return compatiblePreset;
        }

        #endregion

        #region public bool IsReady { get; private set; }

        private static readonly DependencyPropertyKey IsReadyPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsReady),
                typeof(bool),
                typeof(DataViewViewModel),
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
            ((DataViewViewModel)d).OnIsReadyChanged(e.NewValue);
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
            HdvViewModelPreset preset, Action callbackOnComplete)
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
            HdvViewModelPreset preset)
        {
            foreach (HdvColumnViewModelPreset columnPreset in preset.ConfigurableColumns) {
                var info = new DataColumnViewInfo {
                    ColumnId = columnPreset.Id,
                    Name = columnPreset.Name,
                    HelpText = columnPreset.HelpText,
                    IsVisible = columnPreset.IsVisible,
                    Format = columnPreset.CellFormat,
                    FormatProvider = null,
                };
                yield return info;
            }
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

        private void ContinuePresetAfterGridModelInSync(HdvViewModelPreset preset)
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
            //    ColumnChangingContext columnChangingContext = DataViewViewModel.columnChangingContext;
            //    HdvViewModelPreset preset = presetBeingApplied ?? presetToApplyOnReady;
            //    if (this.HasAnyVisibleColumnAffectedByChange(preset, columnChangingContext.ColumnChangingPredicate)) {
            //        this.DisableTableForAsyncOperation();
            //        AddChangingHdvViewModel(this);
            //        flag = true;
            //    }
            //    countBusyHdvViewModels--;
            //    CompleteUpdateWhenAllReady();
            //}
            if (!flag && (presetToApplyOnReady != null)) {
                HdvViewModelPreset = presetToApplyOnReady;
                presetToApplyOnReady = null;
                flag = true;
            }

            //if (!flag && (this.hdvViewModelToCopyStateOnReady != null)) {
            //    this.TryCopyAllStateFrom(this.hdvViewModelToCopyStateOnReady);
            //    this.hdvViewModelToCopyStateOnReady = null;
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
                    ExceptionUtils.ThrowInternalErrorException("We should have sent an update for the hdvviewmodel, but didn't");
                }
                //this.IsTableRefreshing = false;
            }
            return !flag;
        }

        internal CellValue GetCellValue(int rowIndex, int visibleColumnIndex)
        {
            return workManager.BackgroundTaskFactory.StartNew(() => {
                var value = dataView.GetCellValue(rowIndex, visibleColumnIndex);
                value.PrecomputeString();
                return value;
            }).Result;
        }

        internal HdvViewModelPreset CreatePresetFromModifiedUI()
        {
            VerifyIsReady();

            var newPreset = new HdvViewModelPreset();
            HdvViewModelPreset currentPreset = HdvViewModelPreset;
            string name = currentPreset.Name;
            if (name != null)
                newPreset.Name = name;

            newPreset.IsModified = true;

            newPreset.ConfigurableColumns.Clear();
            newPreset.ConfigurableColumns.AddRange(
                from column in columnsViewModel.WritableColumns
                where column.IsConnected
                select column.ToPreset());

            return newPreset;
        }

        public void VerifyIsReady()
        {
            if (!IsReady)
                ExceptionUtils.ThrowInvalidOperationException(
                    "DataViewViewModel needs to be ready for this operation");
        }

        internal void OnUIPropertyChanged(AsyncDataGridColumn column)
        {
            columnsViewModel?.OnUIPropertyChanged(column);
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

        public bool IsValidDataValidityToken(object dataValidityToken)
        {
            return DataView.IsValidDataValidityToken(dataValidityToken);
        }
    }
}
