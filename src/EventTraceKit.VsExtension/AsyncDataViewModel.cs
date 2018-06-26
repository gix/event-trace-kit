namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using Controls;
    using Windows;

    public class AsyncDataViewModel : DependencyObject
    {
        private readonly TaskFactory uiTaskFactory;

        private bool isInitializedWithFirstPreset;
        private bool shouldApplyPreset;
        private AsyncDataViewModelPreset presetBeingApplied;
        private AsyncDataViewModelPreset presetToApplyOnReady;

        public AsyncDataViewModel(
            IDataView dataView,
            AsyncDataViewModelPreset templatePreset,
            AsyncDataViewModelPreset defaultPreset,
            AdvmPresetCollection presetCollection)
        {
            if (dataView == null)
                throw new ArgumentNullException(nameof(dataView));
            if (templatePreset == null)
                throw new ArgumentNullException(nameof(templatePreset));

            uiTaskFactory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());
            DataView = dataView;
            PresetCollection = presetCollection;

            TemplatePreset = templatePreset.EnsureFrozen();
            GridViewModel = new AsyncDataGridViewModel(this);
            dataView.RowCountChanged += OnRowCountChanged;

            Preset = defaultPreset;
            throttledRaiseUpdate = new ThrottledAction(
                TimeSpan.FromMilliseconds(100),
                () => uiTaskFactory.StartNew(RaiseUpdate));
        }

        public AsyncDataViewModelPreset TemplatePreset { get; }
        public AsyncDataGridViewModel GridViewModel { get; }

        #region public AdvmPresetCollection PresetCollection

        private static readonly DependencyPropertyKey PresetCollectionPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(PresetCollection),
                typeof(AdvmPresetCollection),
                typeof(AsyncDataViewModel),
                PropertyMetadataUtils.DefaultNull);

        public static readonly DependencyProperty PresetCollectionProperty =
            PresetCollectionPropertyKey.DependencyProperty;

        public AdvmPresetCollection PresetCollection
        {
            get => (AdvmPresetCollection)GetValue(PresetCollectionProperty);
            private set => SetValue(PresetCollectionPropertyKey, value);
        }

        #endregion

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
            get => (AsyncDataViewModelPreset)GetValue(PresetProperty);
            set => SetValue(PresetProperty, value);
        }

        public event ValueChangedEventHandler<AsyncDataViewModelPreset> PresetChanged;

        private object CoercePreset(object baseValue)
        {
            var preset = (AsyncDataViewModelPreset)baseValue;
            if (preset == null)
                return null;

            shouldApplyPreset = !preset.IsUIModified;

            preset = preset.CreateCompatiblePreset(TemplatePreset);
            preset.IsUIModified = false;
            preset.Freeze();

            return preset;
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
                    if (presetToApplyOnReady == null) {
                        if (presetBeingApplied != null && !presetBeingApplied.Equals(newPreset))
                            presetToApplyOnReady = newPreset;
                    } else {
                        if (!presetToApplyOnReady.Equals(newPreset))
                            presetToApplyOnReady = newPreset;
                    }

                    return;
                }

                presetBeingApplied = newPreset;
                ApplyPresetToGridModel(newPreset);
            }

            if (oldPreset != null && newPreset != null &&
                newPreset.IsModified && newPreset.Name == oldPreset.Name)
                PresetCollection.CachePreset(newPreset);

            PresetChanged.Raise(this, e);
        }

        #endregion

        #region public bool IsReady { get; private set; }

        private static readonly DependencyPropertyKey IsReadyPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsReady),
                typeof(bool),
                typeof(AsyncDataViewModel),
                new PropertyMetadata(Boxed.False, OnIsReadyChanged));

        public static readonly DependencyProperty IsReadyProperty =
            IsReadyPropertyKey.DependencyProperty;

        public bool IsReady
        {
            get => (bool)GetValue(IsReadyProperty);
            private set => SetValue(IsReadyPropertyKey, Boxed.Bool(value));
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

        public bool RequestUpdate()
        {
            if (!IsReady)
                return false;
            RaiseUpdate();
            return true;
        }

        public IDataView DataView { get; }
        internal object DataValidityToken => DataView.DataValidityToken;

        public event EventHandler DataInvalidated;

        private readonly ThrottledAction throttledRaiseUpdate;

        private void OnRowCountChanged(object sender, EventArgs eventArgs)
        {
            if (DataView.RowCount == 0)
                uiTaskFactory.StartNew(
                    () => DataInvalidated?.Invoke(this, EventArgs.Empty));

            throttledRaiseUpdate.Invoke();
        }

        public void RaiseUpdate()
        {
            VerifyAccess();
            GridViewModel?.RaiseUpdated();
        }

        private void ApplyPresetToGridModel(AsyncDataViewModelPreset preset)
        {
            if (!IsReady)
                throw new InvalidOperationException("Model must be ready to apply preset.");

            IsReady = false;

            var columnViewInfos = GetDataColumnViewInfosFromPreset(preset).ToArray();

            DataView.BeginDataUpdate();
            DataView.ApplyColumnView(columnViewInfos);
            DataView.EndDataUpdate();

            GridViewModel.ColumnsModel.ApplyPreset(preset);

            IsReady = true;
            if (presetToApplyOnReady != null) {
                Preset = presetToApplyOnReady;
                presetToApplyOnReady = null;
                return;
            }

            RequestUpdate();
        }

        private IEnumerable<DataColumnViewInfo> GetDataColumnViewInfosFromPreset(
            AsyncDataViewModelPreset preset)
        {
            foreach (var columnPreset in preset.ConfigurableColumns)
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

        internal CellValue GetCellValue(int rowIndex, int visibleColumnIndex)
        {
            var value = DataView.GetCellValue(rowIndex, visibleColumnIndex);
            value.PrecomputeString();
            return value;
        }

        public void VerifyIsReady()
        {
            if (!IsReady) {
                throw new InvalidOperationException("AsyncDataViewModel needs to be ready for this operation");
            }
        }

        internal void OnUIPropertyChanged(
            AsyncDataGridColumn column, DependencyPropertyChangedEventArgs args)
        {
            GridViewModel?.ColumnsModel?.OnUIPropertyChanged(column, args);
        }

        public bool IsValidDataValidityToken(object dataValidityToken)
        {
            return DataView.IsValidDataValidityToken(dataValidityToken);
        }

        public DataColumnView GetPrototypeViewForColumnPreset(ColumnViewModelPreset columnPreset)
        {
            DataColumnViewInfo columnViewInfo = GetDataColumnViewInfoFromPreset(columnPreset);
            return DataView.CreateDataColumnViewFromInfo(columnViewInfo);
        }
    }
}
