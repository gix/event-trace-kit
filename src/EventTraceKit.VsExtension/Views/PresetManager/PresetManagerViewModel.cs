namespace EventTraceKit.VsExtension.Views.PresetManager
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Input;
    using Controls;
    using EventTraceKit.VsExtension.Windows;
    using Extensions;

    public class PresetManagerViewModel : DependencyObject
    {
        private readonly ObservableCollection<ColumnViewModelPreset> templateColumns;
        private readonly ObservableCollection<PresetManagerColumnViewModel> presetColumns;
        private readonly PresetManagerColumnViewModel leftFreezableAreaSeparatorColumn;
        private readonly PresetManagerColumnViewModel rightFreezableAreaSeparatorColumn;

        private AsyncDelegateCommand savePresetCommand;
        private AsyncDelegateCommand savePresetAsCommand;
        private AsyncDelegateCommand resetPresetCommand;
        private AsyncDelegateCommand deletePresetCommand;

        private AsyncDataViewModelPreset templatePreset;
        private AsyncDataViewModelPreset currentPreset;

        private bool refreshingFromPreset;
        private bool isApplyingChanges;

        public PresetManagerViewModel(AsyncDataViewModel advModel)
        {
            TemplateColumns = CollectionUtils.InitializeReadOnly(out templateColumns);
            PresetColumns = CollectionUtils.InitializeReadOnly(out presetColumns);

            leftFreezableAreaSeparatorColumn = new PresetManagerColumnViewModel(
                this, PresetManagerColumnType.LeftFreezableAreaSeparator);
            rightFreezableAreaSeparatorColumn = new PresetManagerColumnViewModel(
                this, PresetManagerColumnType.RightFreezableAreaSeparator);

            PresetDropDownMenu = new HeaderDropDownMenu();
            BindingOperations.SetBinding(PresetDropDownMenu, HeaderDropDownMenu.HeaderProperty, new Binding {
                Source = this,
                Path = new PropertyPath(MangledPresetNameProperty),
                Mode = BindingMode.OneWay
            });

            HdvViewModel = advModel;
            IsDialogStateDirty = false;
            isApplyingChanges = false;
        }

        public HeaderDropDownMenu PresetDropDownMenu { get; }

        public int LastLeftFrozenIndex => presetColumns.IndexOf(leftFreezableAreaSeparatorColumn);
        public int FirstRightFrozenIndex => presetColumns.IndexOf(rightFreezableAreaSeparatorColumn);

        public ICommand SavePresetCommand =>
            savePresetCommand ?? (savePresetCommand = new AsyncDelegateCommand(SavePreset, CanSavePreset));

        public ICommand SavePresetAsCommand =>
            savePresetAsCommand ?? (savePresetAsCommand = new AsyncDelegateCommand(SavePresetAs));

        public ICommand ResetPresetCommand =>
            resetPresetCommand ?? (resetPresetCommand = new AsyncDelegateCommand(ResetPreset, CanResetPreset));

        public ICommand DeletePresetCommand =>
            deletePresetCommand ?? (deletePresetCommand = new AsyncDelegateCommand(DeletePreset, CanDeletePreset));

        #region public string DisplayName

        public static readonly DependencyProperty DisplayNameProperty =
            DependencyProperty.Register(
                nameof(DisplayName),
                typeof(string),
                typeof(PresetManagerViewModel),
                new PropertyMetadata(null));

        public string DisplayName
        {
            get => (string)GetValue(DisplayNameProperty);
            set => SetValue(DisplayNameProperty, value);
        }

        #endregion

        #region public AsyncDataViewModel HdvViewModel

        private static readonly DependencyPropertyKey HdvViewModelPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(HdvViewModel),
                typeof(AsyncDataViewModel),
                typeof(PresetManagerViewModel),
                new PropertyMetadata(
                    null,
                    (d, e) => ((PresetManagerViewModel)d).OnHdvViewModelPropertyChanged(e)));

        public static readonly DependencyProperty HdvViewModelProperty =
            HdvViewModelPropertyKey.DependencyProperty;

        public AsyncDataViewModel HdvViewModel
        {
            get => (AsyncDataViewModel)GetValue(HdvViewModelProperty);
            private set => SetValue(HdvViewModelPropertyKey, value);
        }

        #endregion

        #region public ReadOnlyObservableCollection<ColumnViewModelPreset> TemplateColumns

        private static readonly DependencyPropertyKey TemplateColumnsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(TemplateColumns),
                typeof(ReadOnlyObservableCollection<ColumnViewModelPreset>),
                typeof(PresetManagerViewModel),
                new PropertyMetadata(null));

        public static readonly DependencyProperty TemplateColumnsProperty =
            TemplateColumnsPropertyKey.DependencyProperty;

        public ReadOnlyObservableCollection<ColumnViewModelPreset> TemplateColumns
        {
            get => (ReadOnlyObservableCollection<ColumnViewModelPreset>)GetValue(TemplateColumnsProperty);
            private set => SetValue(TemplateColumnsPropertyKey, value);
        }

        #endregion

        #region public ReadOnlyObservableCollection<PresetManagerColumnViewModel> PresetColumns

        private static readonly DependencyPropertyKey PresetColumnsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(PresetColumns),
                typeof(ReadOnlyObservableCollection<PresetManagerColumnViewModel>),
                typeof(PresetManagerViewModel),
                new PropertyMetadata(null));

        public static readonly DependencyProperty PresetColumnsProperty =
            PresetColumnsPropertyKey.DependencyProperty;

        public ReadOnlyObservableCollection<PresetManagerColumnViewModel> PresetColumns
        {
            get => (ReadOnlyObservableCollection<PresetManagerColumnViewModel>)GetValue(PresetColumnsProperty);
            private set => SetValue(PresetColumnsPropertyKey, value);
        }

        #endregion

        #region public ReadOnlyObservableCollection<PresetManagerColumnViewModel> ConfigurablePresetColumns

        private static readonly DependencyPropertyKey ConfigurablePresetColumnsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ConfigurablePresetColumns),
                typeof(ReadOnlyObservableCollection<PresetManagerColumnViewModel>),
                typeof(PresetManagerViewModel),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ConfigurablePresetColumnsProperty =
            ConfigurablePresetColumnsPropertyKey.DependencyProperty;

        public ReadOnlyObservableCollection<PresetManagerColumnViewModel> ConfigurablePresetColumns
        {
            get => (ReadOnlyObservableCollection<PresetManagerColumnViewModel>)
                GetValue(ConfigurablePresetColumnsProperty);
            private set => SetValue(ConfigurablePresetColumnsPropertyKey, value);
        }

        #endregion

        #region public PresetManagerColumnViewModel SelectedColumn

        public static readonly DependencyProperty SelectedColumnProperty =
            DependencyProperty.Register(
                nameof(SelectedColumn),
                typeof(PresetManagerColumnViewModel),
                typeof(PresetManagerViewModel),
                new PropertyMetadata(default(PresetManagerColumnViewModel)));

        public PresetManagerColumnViewModel SelectedColumn
        {
            get => (PresetManagerColumnViewModel)GetValue(SelectedColumnProperty);
            set => SetValue(SelectedColumnProperty, value);
        }

        #endregion

        #region public string MangledPresetName

        private static readonly DependencyPropertyKey MangledPresetNamePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(MangledPresetName),
                typeof(string),
                typeof(PresetManagerViewModel),
                new PropertyMetadata(string.Empty, null, CoerceMangledPresetName));

        public static readonly DependencyProperty MangledPresetNameProperty =
            MangledPresetNamePropertyKey.DependencyProperty;

        public string MangledPresetName
        {
            get => (string)GetValue(MangledPresetNameProperty);
            private set => SetValue(MangledPresetNamePropertyKey, value);
        }

        private static object CoerceMangledPresetName(DependencyObject d, object newValue)
        {
            var source = (PresetManagerViewModel)d;
            if (!source.IsCurrentPresetModified)
                return source.CurrentSelectedPresetName;
            return $"{source.CurrentSelectedPresetName}*";
        }

        #endregion

        #region public string CurrentSelectedPresetName

        private static readonly DependencyPropertyKey CurrentSelectedPresetNamePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(CurrentSelectedPresetName),
                typeof(string),
                typeof(PresetManagerViewModel),
                new PropertyMetadata(
                    string.Empty,
                    OnIsCurrentSelectedPresetNameChanged));

        public static readonly DependencyProperty CurrentSelectedPresetNameProperty =
            CurrentSelectedPresetNamePropertyKey.DependencyProperty;

        public string CurrentSelectedPresetName
        {
            get => (string)GetValue(CurrentSelectedPresetNameProperty);
            set => SetValue(CurrentSelectedPresetNamePropertyKey, value);
        }

        private static void OnIsCurrentSelectedPresetNameChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(MangledPresetNameProperty);
        }

        #endregion

        #region public bool IsDialogStateDirty

        private static readonly DependencyPropertyKey IsDialogStateDirtyPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsDialogStateDirty),
                typeof(bool),
                typeof(PresetManagerViewModel),
                new PropertyMetadata(Boxed.False, OnIsDialogStateDirtyChanged, null));

        public static readonly DependencyProperty IsDialogStateDirtyProperty =
            IsDialogStateDirtyPropertyKey.DependencyProperty;

        public bool IsDialogStateDirty
        {
            get => (bool)GetValue(IsDialogStateDirtyProperty);
            set => SetValue(IsDialogStateDirtyPropertyKey, value);
        }

        private static void OnIsDialogStateDirtyChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(CanApplyProperty);
        }

        #endregion

        #region public bool CanApply

        private static readonly DependencyPropertyKey CanApplyPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(CanApply),
                typeof(bool),
                typeof(PresetManagerViewModel),
                new PropertyMetadata(Boxed.False, null, CoerceCanApply));

        public static readonly DependencyProperty CanApplyProperty =
            CanApplyPropertyKey.DependencyProperty;

        public bool CanApply
        {
            get => (bool)GetValue(CanApplyProperty);
            private set => SetValue(CanApplyPropertyKey, value);
        }

        private static object CoerceCanApply(DependencyObject d, object newValue)
        {
            var source = (PresetManagerViewModel)d;
            return Boxed.Bool(source.IsDialogStateDirty);
        }

        #endregion

        #region public bool IsCurrentPresetModified

        private static readonly DependencyPropertyKey IsCurrentPresetModifiedPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsCurrentPresetModified),
                typeof(bool),
                typeof(PresetManagerViewModel),
                new PropertyMetadata(
                    Boxed.False, OnIsCurrentPresetModifiedChanged, null));

        public static readonly DependencyProperty IsCurrentPresetModifiedProperty =
            IsCurrentPresetModifiedPropertyKey.DependencyProperty;

        public bool IsCurrentPresetModified
        {
            get => (bool)GetValue(IsCurrentPresetModifiedProperty);
            private set => SetValue(IsCurrentPresetModifiedPropertyKey, value);
        }

        private static void OnIsCurrentPresetModifiedChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(MangledPresetNameProperty);
        }

        #endregion

        private void OnHdvViewModelPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            var oldValue = (AsyncDataViewModel)e.OldValue;
            if (oldValue != null) {
                oldValue.PresetCollection.AvailablePresetsChanged -= OnAvailablePresetsChanged;
                oldValue.PresetChanged -= OnPresetChanged;
            }

            var newValue = (AsyncDataViewModel)e.NewValue;
            if (newValue != null) {
                newValue.PresetCollection.AvailablePresetsChanged += OnAvailablePresetsChanged;
                newValue.PresetChanged += OnPresetChanged;
                RefreshFromHdvViewModel();
            }
        }

        private void OnAvailablePresetsChanged(object sender, EventArgs e)
        {
            PresetDropDownMenu.Items.Clear();
            PresetDropDownMenu.Items.AddRange(
                from preset in HdvViewModel.PresetCollection.EnumerateAllPresetsByName()
                select new ApplyPresetHeaderCommand(this, preset.Name));
        }

        private bool CanSavePreset()
        {
            if (HdvViewModel == null)
                return false;
            var presetCollection = HdvViewModel.PresetCollection;
            if (presetCollection.IsBuiltInPreset(CurrentSelectedPresetName))
                return false;

            return IsCurrentPresetModified;
        }

        private Task SavePreset()
        {
            SaveCurrentPresetAs(CurrentSelectedPresetName);
            return Task.CompletedTask;
        }

        public Task SavePresetAs()
        {
            var dialog = PresetSaveAsDialog.ShowPresetSaveAsDialog(
                HdvViewModel.PresetCollection);
            if (dialog.DialogResult == true)
                SaveCurrentPresetAs(dialog.NewPresetName);
            return Task.CompletedTask;
        }

        private bool CanResetPreset()
        {
            return IsCurrentPresetModified;
        }

        private Task ResetPreset()
        {
            ResetCurrentPreset();
            return Task.CompletedTask;
        }

        private bool CanDeletePreset()
        {
            return
                HdvViewModel != null &&
                !HdvViewModel.PresetCollection.IsBuiltInPreset(CurrentSelectedPresetName);
        }

        private Task DeletePreset()
        {
            var name = CurrentSelectedPresetName;
            var result = MessageBox.Show(
                "Are you sure you want to delete \"" + name + "\"?", "Confirm Delete",
                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes);

            if (result == MessageBoxResult.Yes) {
                DeletePreset(name);
                var newPreset = HdvViewModel.PresetCollection.EnumerateAllPresets().FirstOrDefault();
                if (newPreset == null) {
                    newPreset = HdvViewModel.TemplatePreset;
                    HdvViewModel.PresetCollection.SetUserPreset(newPreset);
                }

                if (HdvViewModel.Preset.Equals(currentPreset)) {
                    isApplyingChanges = true;
                    HdvViewModel.Preset = newPreset;
                    isApplyingChanges = false;
                }

                RefreshFromPreset(newPreset);
            }

            return Task.CompletedTask;
        }

        private void DeletePreset(string name)
        {
            HdvViewModel.PresetCollection.DeleteUserPresetByName(name);
        }

        private sealed class ApplyPresetHeaderCommand : GraphTreeItemHeaderCommand
        {
            private static readonly IsEqualConverter isEqualConverter = new IsEqualConverter();
            private readonly PresetManagerViewModel presetManagerViewModel;

            public ApplyPresetHeaderCommand(PresetManagerViewModel presetManagerViewModel, string presetName)
            {
                this.presetManagerViewModel = presetManagerViewModel;
                DisplayName = presetName;
                IsCheckable = true;
                BindingOperations.SetBinding(this, IsCheckedProperty, new Binding {
                    Source = presetManagerViewModel,
                    Path = new PropertyPath(CurrentSelectedPresetNameProperty),
                    Mode = BindingMode.OneWay,
                    Converter = isEqualConverter,
                    ConverterParameter = presetName
                });
            }

            public override void OnExecute()
            {
                presetManagerViewModel.RefreshFromPreset(DisplayName);
            }
        }

        private void RefreshFromHdvViewModel()
        {
            PresetDropDownMenu.Items.Clear();
            PresetDropDownMenu.Items.AddRange(
                from preset in HdvViewModel.PresetCollection.EnumerateAllPresetsByName()
                select new ApplyPresetHeaderCommand(this, preset.Name));

            templatePreset = HdvViewModel.TemplatePreset;
            templateColumns.Clear();
            templateColumns.AddRange(
                templatePreset.ConfigurableColumns.OrderBy(x => x.Name));
            RefreshFromPreset(HdvViewModel.Preset);
        }

        private void RefreshFromPreset(string displayName)
        {
            var preset = HdvViewModel.PresetCollection.TryGetCurrentPresetByName(displayName);
            RefreshFromPreset(preset);
        }

        private void RefreshFromPreset(AsyncDataViewModelPreset preset)
        {
            refreshingFromPreset = true;
            try {
                currentPreset = preset.CreateCompatiblePreset(templatePreset);

                presetColumns.Clear();
                foreach (var columnPreset in currentPreset.ConfigurableColumns) {
                    var columnView = HdvViewModel.GetPrototypeViewForColumnPreset(columnPreset);
                    var item = new PresetManagerColumnViewModel(this, columnPreset, columnView);
                    presetColumns.Add(item);
                }

                int leftFrozenColumnIndex = currentPreset.LeftFrozenColumnCount;
                int rightFrozenColumnIndex = currentPreset.ConfigurableColumns.Count -
                                             currentPreset.RightFrozenColumnCount;
                presetColumns.Insert(rightFrozenColumnIndex, rightFreezableAreaSeparatorColumn);
                presetColumns.Insert(leftFrozenColumnIndex, leftFreezableAreaSeparatorColumn);

                foreach (var column in presetColumns)
                    column.RefreshPositionDependentProperties();

                CurrentSelectedPresetName = currentPreset.Name;
                SetDialogStateDirty(currentPreset.IsModified);
            } finally {
                refreshingFromPreset = false;
            }
        }

        private void OnPresetChanged(
            object sender, ValueChangedEventArgs<AsyncDataViewModelPreset> args)
        {
            if (!isApplyingChanges)
                RefreshFromPreset(HdvViewModel.Preset);
        }

        private void UpdateCurrentPreset()
        {
            if (refreshingFromPreset)
                return;

            currentPreset.ConfigurableColumns.Clear();
            currentPreset.ConfigurableColumns.AddRange(
                from column in PresetColumns
                where column.ColumnType == PresetManagerColumnType.Configurable
                select column.Preset);

            currentPreset.LeftFrozenColumnCount = PresetColumns.IndexOf(leftFreezableAreaSeparatorColumn);
            currentPreset.RightFrozenColumnCount = PresetColumns.Count - PresetColumns.IndexOf(rightFreezableAreaSeparatorColumn) - 1;
        }

        public void Add(
            ColumnViewModelPreset[] newColumns, PresetManagerColumnViewModel addTarget,
            bool moveAfter)
        {
            Add(newColumns, addTarget, moveAfter, false);
        }

        public void Add(
            ColumnViewModelPreset[] newColumns, PresetManagerColumnViewModel addTarget,
            bool moveAfter, bool visibleOnAdd)
        {
            if (newColumns == null)
                throw new ArgumentNullException(nameof(newColumns));

            int insertAt = addTarget != null ? presetColumns.IndexOf(addTarget) : 0;
            if (moveAfter && addTarget != null)
                ++insertAt;

            foreach (var newColumn in newColumns) {
                var columnPreset = newColumn.Clone();
                if (visibleOnAdd)
                    columnPreset.IsVisible = true;

                var viewModel = new PresetManagerColumnViewModel(
                    this, columnPreset, HdvViewModel.GetPrototypeViewForColumnPreset(newColumn));

                presetColumns.Insert(insertAt++, viewModel);
            }

            foreach (var column in presetColumns)
                column.RefreshPositionDependentProperties();

            SetDialogStateDirty(true);
        }

        public bool CanMove(
            PresetManagerColumnViewModel[] toMove,
            PresetManagerColumnViewModel moveTarget,
            bool moveAfter)
        {
            if (toMove == null)
                throw new ArgumentNullException(nameof(toMove));
            if (moveTarget == null)
                throw new ArgumentNullException(nameof(moveTarget));

            bool leftFreezableDragged = Array.IndexOf(toMove, leftFreezableAreaSeparatorColumn) != -1;
            bool rightFreezableDragged = Array.IndexOf(toMove, rightFreezableAreaSeparatorColumn) != -1;

            if (leftFreezableDragged == rightFreezableDragged)
                return true;

            int insertAt = presetColumns.IndexOf(moveTarget);
            if (insertAt == -1)
                insertAt = presetColumns.Count;
            else if (moveAfter)
                ++insertAt;

            if (leftFreezableDragged) {
                int rightFreezableIndex = presetColumns.IndexOf(rightFreezableAreaSeparatorColumn);
                return insertAt <= rightFreezableIndex;
            } else {
                int leftFreezableIndex = presetColumns.IndexOf(leftFreezableAreaSeparatorColumn);
                return insertAt > leftFreezableIndex;
            }
        }

        public void Move(
            PresetManagerColumnViewModel[] toMove,
            PresetManagerColumnViewModel moveTarget,
            bool moveAfter)
        {
            if (toMove == null)
                throw new ArgumentNullException(nameof(toMove));
            if (moveTarget == null)
                throw new ArgumentNullException(nameof(moveTarget));

            bool moved = false;
            foreach (var column in toMove) {
                if (!ReferenceEquals(moveTarget, column))
                    presetColumns.Remove(column);
            }

            int insertAt = presetColumns.IndexOf(moveTarget);
            if (insertAt == -1)
                insertAt = presetColumns.Count;
            else if (moveAfter)
                ++insertAt;

            foreach (var column in toMove) {
                if (ReferenceEquals(moveTarget, column))
                    continue;

                presetColumns.Insert(insertAt++, column);
                moved = true;
            }

            if (moved) {
                foreach (var column in presetColumns)
                    column.RefreshPositionDependentProperties();

                SetDialogStateDirty(true);
            }
        }

        public void Remove(PresetManagerColumnViewModel[] toRemove)
        {
            if (toRemove == null)
                throw new ArgumentNullException(nameof(toRemove));

            bool removed = false;
            foreach (var c in toRemove) {
                if (c.ColumnType == PresetManagerColumnType.Configurable) {
                    presetColumns.Remove(c);
                    removed = true;
                }
            }

            if (removed) {
                foreach (var column in presetColumns)
                    column.RefreshPositionDependentProperties();

                SetDialogStateDirty(true);
            }
        }

        internal void SetDialogStateDirty(bool markCurrentPresetModified = true)
        {
            IsDialogStateDirty = true;
            IsCurrentPresetModified = markCurrentPresetModified;
        }

        internal AsyncDataViewModelPreset CaptureCurrentPreset()
        {
            if (!IsCurrentPresetModified)
                return currentPreset.Clone();
            return currentPreset.CreateModifiedPreset();
        }

        public void ApplyChanges()
        {
            if (!refreshingFromPreset && !isApplyingChanges) {
                isApplyingChanges = true;
                try {
                    UpdateCurrentPreset();
                    var newPreset = CaptureCurrentPreset();
                    var hdvViewModel = HdvViewModel;
                    hdvViewModel.Preset = newPreset;
                    //hdvViewModel.ShowHideFreezeBars(this.ShowFreezeBars);
                    RefreshFromPreset(hdvViewModel.Preset);
                } finally {
                    isApplyingChanges = false;
                }
            }

            IsDialogStateDirty = false;
        }

        public AsyncDataViewModelPreset CreatePresetFromCurrentState()
        {
            UpdateCurrentPreset();
            return CaptureCurrentPreset();
        }

        public void ResetCurrentPreset()
        {
            if (IsCurrentPresetModified) {
                HdvViewModel.PresetCollection.DeletePersistedPresetByName(currentPreset.Name);
                currentPreset = HdvViewModel.PresetCollection.TryGetUnmodifiedPresetByName(currentPreset.Name);
                RefreshFromPreset(currentPreset);
            }
        }

        public void SaveCurrentPresetAs(string newPresetName)
        {
            AdvmPresetCollection presetCollection = HdvViewModel?.PresetCollection;
            if (presetCollection == null)
                return;

            string name = CurrentSelectedPresetName;
            bool isModified = IsCurrentPresetModified;
            var newPreset = CreatePresetFromCurrentState();

            if (newPreset.Name != newPresetName)
                newPreset.Name = newPresetName;
            newPreset.IsModified = false;

            var oldPreset = presetCollection.UserPresets.FirstOrDefault(p => p.Name.Equals(newPresetName));
            presetCollection.UserPresets.Add(newPreset);
            if (oldPreset != null)
                presetCollection.UserPresets.Remove(oldPreset);

            HdvViewModel.Preset = newPreset;

            presetCollection.SavePreset(newPreset, isModified, name);
        }
    }
}
