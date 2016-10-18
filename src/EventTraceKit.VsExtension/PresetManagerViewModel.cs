namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Automation;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using Collections;
    using Controls;
    using Formatting;
    using Microsoft.VisualStudio.PlatformUI;
    using Windows;

    public class PresetManagerViewModel : DependencyObject
    {
        private readonly ObservableCollection<ColumnViewModelPreset> templateColumns;
        private readonly ObservableCollection<PresetManagerColumnViewModel> presetColumns;
        private readonly PresetManagerColumnViewModel leftFreezableAreaSeparatorColumn;
        private readonly PresetManagerColumnViewModel rightFreezableAreaSeparatorColumn;
        private readonly PersistenceManager persistenceManager;

        private AsyncDelegateCommand savePresetCommand;
        private AsyncDelegateCommand savePresetAsCommand;
        private AsyncDelegateCommand resetPresetCommand;
        private AsyncDelegateCommand deletePresetCommand;

        private AsyncDataViewModelPreset templatePreset;
        private AsyncDataViewModelPreset currentPreset;

        private bool refreshingFromPreset;
        private bool isApplyingChanges;

        public PresetManagerViewModel(AsyncDataViewModel advModel, PersistenceManager persistenceManager)
        {
            this.persistenceManager = persistenceManager;

            TemplateColumns = CollectionUtils.InitializeReadOnly(out templateColumns);
            PresetColumns = CollectionUtils.InitializeReadOnly(out presetColumns);

            leftFreezableAreaSeparatorColumn = new PresetManagerColumnViewModel(this, PresetManagerColumnType.LeftFreezableAreaSeparator);
            rightFreezableAreaSeparatorColumn = new PresetManagerColumnViewModel(this, PresetManagerColumnType.RightFreezableAreaSeparator);

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
            get { return (string)GetValue(DisplayNameProperty); }
            set { SetValue(DisplayNameProperty, value); }
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
            get { return (AsyncDataViewModel)GetValue(HdvViewModelProperty); }
            private set { SetValue(HdvViewModelPropertyKey, value); }

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
            get { return (ReadOnlyObservableCollection<ColumnViewModelPreset>)GetValue(TemplateColumnsProperty); }
            private set { SetValue(TemplateColumnsPropertyKey, value); }
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
            get { return (ReadOnlyObservableCollection<PresetManagerColumnViewModel>)GetValue(PresetColumnsProperty); }
            private set { SetValue(PresetColumnsPropertyKey, value); }
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
            get
            {
                return (ReadOnlyObservableCollection<PresetManagerColumnViewModel>)
                    GetValue(ConfigurablePresetColumnsProperty);
            }
            private set { SetValue(ConfigurablePresetColumnsPropertyKey, value); }
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
            get { return (PresetManagerColumnViewModel)GetValue(SelectedColumnProperty); }
            set { SetValue(SelectedColumnProperty, value); }
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
            get { return (string)GetValue(MangledPresetNameProperty); }
            private set { SetValue(MangledPresetNamePropertyKey, value); }
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
            get { return (string)GetValue(CurrentSelectedPresetNameProperty); }
            set { SetValue(CurrentSelectedPresetNamePropertyKey, value); }
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
            get { return (bool)GetValue(IsDialogStateDirtyProperty); }
            set { SetValue(IsDialogStateDirtyPropertyKey, value); }
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
            get { return (bool)GetValue(CanApplyProperty); }
            private set { SetValue(CanApplyPropertyKey, value); }
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
            get { return (bool)GetValue(IsCurrentPresetModifiedProperty); }
            private set { SetValue(IsCurrentPresetModifiedPropertyKey, value); }
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
                from presetName in HdvViewModel.PresetCollection.EnumerateAllPresetsByName()
                select new ApplyPresetHeaderCommand(this, presetName));
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
                if (newPreset == null)
                    newPreset = new AsyncDataViewModelPreset { Name = "New" };
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
            private static readonly IsEqual isEqualConverter = new IsEqual();
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
                from presetName in HdvViewModel.PresetCollection.EnumerateAllPresetsByName()
                select new ApplyPresetHeaderCommand(this, presetName));

            templatePreset = HdvViewModel.TemplatePreset;
            templateColumns.Clear();
            templateColumns.AddRange(
                templatePreset.ConfigurableColumns.OrderBy(x => x.Name));
            RefreshFromPreset(HdvViewModel.Preset);
        }

        private void RefreshFromPreset(string displayName)
        {
            var preset = persistenceManager.TryGetCachedVersion(displayName);
            if (preset == null)
                preset = HdvViewModel.PresetCollection.TryGetPresetByName(displayName);
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
                PersistenceManager.Instance.RemoveCachedVersion(currentPreset.Name);
                currentPreset = HdvViewModel.PresetCollection.EnumerateAllPresets().FirstOrDefault(p => p.Name == currentPreset.Name);
                RefreshFromPreset(currentPreset);
            }
        }

        public void SaveCurrentPresetAs(string newPresetName)
        {
            HdvViewModelPresetCollection presetCollection = null;
            if (HdvViewModel != null)
                presetCollection = HdvViewModel.PresetCollection;

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

            PresetCollectionManagerView.Instance.SavePresetToRepository(newPreset, isModified, name);
        }
    }

    public sealed class IsEqual : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null && value.Equals(parameter) ? Boxed.True : Boxed.False;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value.Equals(true))
                return parameter;
            return DependencyProperty.UnsetValue;
        }
    }

    public class HeaderDropDownMenu : DependencyObject
    {
        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register(
                nameof(IsOpen),
                typeof(bool),
                typeof(HeaderDropDownMenu),
                new PropertyMetadata(Boxed.False));

        private static readonly DependencyPropertyKey ItemsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(Items),
                typeof(ObservableCollection<GraphTreeItemHeaderCommand>),
                typeof(HeaderDropDownMenu),
                PropertyMetadataUtils.DefaultNull);

        public static readonly DependencyProperty ItemsProperty = ItemsPropertyKey.DependencyProperty;

        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(
                nameof(Header),
                typeof(object),
                typeof(HeaderDropDownMenu),
                new PropertyMetadata(
                    null, (d, e) => ((HeaderDropDownMenu)d).HeaderPropertyChanged(e)));

        public HeaderDropDownMenu()
        {
            Items = new ObservableCollection<GraphTreeItemHeaderCommand>();
        }

        private void HeaderPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            //base.CoerceValue(AutomationNameProperty);
        }

        public bool IsOpen
        {
            get { return (bool)GetValue(IsOpenProperty); }
            set { SetValue(IsOpenProperty, Boxed.Bool(value)); }
        }

        public object Header
        {
            get { return GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        public ObservableCollection<GraphTreeItemHeaderCommand> Items
        {
            get { return (ObservableCollection<GraphTreeItemHeaderCommand>)GetValue(ItemsProperty); }
            private set { SetValue(ItemsPropertyKey, value); }
        }
    }

    public class GraphTreeItemHeaderCommand : DependencyObject
    {
        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register("CommandParameter", typeof(object), typeof(GraphTreeItemHeaderCommand), new PropertyMetadata(null));
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register("Command", typeof(ICommand), typeof(GraphTreeItemHeaderCommand), new PropertyMetadata(null, (s, e) => ((GraphTreeItemHeaderCommand)s).CommandPropertyChanged(e)));
        public static readonly DependencyProperty DisplayNameProperty = DependencyProperty.Register("DisplayName", typeof(string), typeof(GraphTreeItemHeaderCommand), PropertyMetadataUtils.DefaultNull);
        private DelegateCommand executeCommand;
        public static readonly DependencyProperty IsCheckableProperty = DependencyProperty.Register("IsCheckable", typeof(bool), typeof(GraphTreeItemHeaderCommand), new PropertyMetadata(Boxed.False));
        public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register("IsChecked", typeof(bool), typeof(GraphTreeItemHeaderCommand), new PropertyMetadata(Boxed.False));
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register("IsEnabled", typeof(bool), typeof(GraphTreeItemHeaderCommand), new PropertyMetadata(Boxed.True));

        private void CommandPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
        }

        public void Execute()
        {
            VerifyAccess();
            OnExecute();
        }

        public virtual void OnExecute()
        {
            ICommand command = Command;
            object commandParameter = CommandParameter;
            if (command != null && command.CanExecute(commandParameter)) {
                command.Execute(commandParameter);
            }
        }

        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public object CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        public string DisplayName
        {
            get { return (string)GetValue(DisplayNameProperty); }
            set { SetValue(DisplayNameProperty, value); }
        }

        public ICommand ExecuteCommand
        {
            get
            {
                VerifyAccess();
                if (executeCommand == null) {
                    executeCommand = new DelegateCommand(obj => Execute());
                }
                return executeCommand;
            }
        }

        public bool IsCheckable
        {
            get { return ((bool)GetValue(IsCheckableProperty)); }
            set
            {
                SetValue(IsCheckableProperty, Boxed.Bool(value));
            }
        }

        public bool IsChecked
        {
            get { return ((bool)GetValue(IsCheckedProperty)); }
            set
            {
                SetValue(IsCheckedProperty, Boxed.Bool(value));
            }
        }

        public bool IsEnabled
        {
            get { return ((bool)GetValue(IsEnabledProperty)); }
            set { SetValue(IsEnabledProperty, Boxed.Bool(value)); }
        }
    }

    public interface DataTableGraphTreeItem
    {
        AsyncDataViewModel HdvViewModel { get; }
        event ValueChangedEventHandler<AsyncDataViewModelPreset> PresetChanged;
    }

    public static class CollectionExtensions
    {
        public static int IndexOf<TSource>(
            this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            int index = 0;
            foreach (var item in source) {
                if (predicate(item))
                    return index;
                ++index;
            }

            return -1;
        }
    }

    public class PresetManagerColumnContainerStyleSelector : StyleSelector
    {
        public override Style SelectStyle(object item, DependencyObject container)
        {
            var model = item as PresetManagerColumnViewModel;
            if (model == null)
                return base.SelectStyle(item, container);
            if (model.ColumnType == PresetManagerColumnType.Configurable)
                return ConfigurableColumnStyle;
            return SeparatorColumnStyle;
        }

        public Style ConfigurableColumnStyle { get; set; }
        public Style SeparatorColumnStyle { get; set; }
    }

    public class PresetManagerColumnDetailsTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            PresetManagerColumnViewModel model = item as PresetManagerColumnViewModel;
            if (model == null)
                return base.SelectTemplate(item, container);

            switch (model.ColumnType) {
                case PresetManagerColumnType.LeftFreezableAreaSeparator:
                    return LeftFreezableAreaSeparatorColumnTemplate;
                case PresetManagerColumnType.RightFreezableAreaSeparator:
                    return RightFreezableAreaSeparatorColumnTemplate;
                case PresetManagerColumnType.Configurable:
                    return ConfigurableColumnTemplate;
                default:
                    throw ExceptionUtils.InvalidEnumArgumentException(
                        model.ColumnType, "model.ColumnType");
            }
        }

        public DataTemplate ConfigurableColumnTemplate { get; set; }
        public DataTemplate LeftFreezableAreaSeparatorColumnTemplate { get; set; }
        public DataTemplate RightFreezableAreaSeparatorColumnTemplate { get; set; }
    }

    public enum PresetManagerColumnType
    {
        Configurable,
        LeftFreezableAreaSeparator,
        RightFreezableAreaSeparator,
    }

    public class PresetManagerColumnViewModel : DependencyObject
    {
        private bool refreshingFromPreset;
        private readonly SupportedFormat defaultSupportedFormat;

        public PresetManagerColumnViewModel(
            PresetManagerViewModel presetManager, PresetManagerColumnType columnType)
        {
            PresetManager = presetManager;
            ColumnType = columnType;
        }

        public PresetManagerColumnViewModel(
            PresetManagerViewModel presetManager,
            ColumnViewModelPreset preset,
            DataColumnView columnView)
        {
            if (presetManager == null)
                throw new ArgumentNullException(nameof(presetManager));
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));
            if (columnView == null)
                throw new ArgumentNullException(nameof(columnView));

            //if (preset.Name != columnView.ColumnName) {
            //    if (preset.IsFrozen)
            //        preset = preset.Clone();
            //    preset.Name = columnView.ColumnName;
            //}

            PresetManager = presetManager;
            Preset = preset;

            defaultSupportedFormat = columnView.FormatProvider.DefaultSupportedFormat();
            SupportedFormats = columnView.FormatProvider.SupportedFormats();

            RefreshFromPreset();
            CellFormat = GetSupportedFormat(columnView.Format);
            ColumnType = PresetManagerColumnType.Configurable;
        }

        public PresetManagerViewModel PresetManager { get; }
        public ColumnViewModelPreset Preset { get; }
        public PresetManagerColumnType ColumnType { get; }

        public IEnumerable<SupportedFormat> SupportedFormats { get; }
        public Visibility CellFormatVisibility =>
            SupportedFormats.Any() ? Visibility.Visible : Visibility.Collapsed;

        #region public Guid Id { get; set; }

        /// <summary>
        ///   Identifies the <see cref="Id"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IdProperty =
            DependencyProperty.Register(
                nameof(Id),
                typeof(Guid),
                typeof(PresetManagerColumnViewModel),
                new PropertyMetadata(null));

        /// <summary>
        ///   Gets or sets the id.
        /// </summary>
        public Guid Id
        {
            get { return (Guid)GetValue(IdProperty); }
            set { SetValue(IdProperty, value); }
        }

        #endregion

        #region public string Name { get; private set; }

        private static readonly DependencyPropertyKey NamePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(Name),
                typeof(string),
                typeof(PresetManagerColumnViewModel),
                new PropertyMetadata(string.Empty));

        /// <summary>
        ///   Identifies the <see cref="Name"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty NameProperty =
            NamePropertyKey.DependencyProperty;

        /// <summary>
        ///   Gets the name.
        /// </summary>
        public string Name
        {
            get { return (string)GetValue(NameProperty); }
            private set { SetValue(NamePropertyKey, value); }
        }

        #endregion

        #region public int Width { get; set; }

        /// <summary>
        ///   Identifies the <see cref="Width"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty WidthProperty =
            DependencyProperty.Register(
                nameof(Width),
                typeof(int),
                typeof(PresetManagerColumnViewModel),
                new PropertyMetadata(
                    0,
                    (d, e) => ((PresetManagerColumnViewModel)d).OnPresetPropertyChanged(),
                    (d, v) => ((PresetManagerColumnViewModel)d).CoerceWidth(v)));

        /// <summary>
        ///   Gets or sets the width.
        /// </summary>
        public int Width
        {
            get { return (int)GetValue(WidthProperty); }
            set { SetValue(WidthProperty, value); }
        }

        private object CoerceWidth(object baseValue)
        {
            return Boxed.Int32(((int)baseValue).Clamp(0, 10000));
        }

        #endregion

        #region public bool IsVisible { get; set; }

        public static readonly DependencyProperty IsVisibleProperty =
            DependencyProperty.Register(
                nameof(IsVisible),
                typeof(bool),
                typeof(PresetManagerColumnViewModel),
                new PropertyMetadata(
                    Boxed.True,
                    (d, e) => ((PresetManagerColumnViewModel)d).OnPresetPropertyChanged()));

        public bool IsVisible
        {
            get { return (bool)GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, value); }
        }

        #endregion

        #region public TextAlignment TextAlignment { get; set; }

        /// <summary>
        ///   Identifies the <see cref="TextAlignment"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextAlignmentProperty =
            DependencyProperty.Register(
                nameof(TextAlignment),
                typeof(TextAlignment),
                typeof(PresetManagerColumnViewModel),
                new PropertyMetadata(
                    TextAlignment.Left,
                    (d, e) => ((PresetManagerColumnViewModel)d).OnPresetPropertyChanged()));

        /// <summary>
        ///   Gets or sets the text alignment.
        /// </summary>
        public TextAlignment TextAlignment
        {
            get { return (TextAlignment)GetValue(TextAlignmentProperty); }
            set { SetValue(TextAlignmentProperty, value); }
        }

        #endregion

        #region public bool IsFrozen { get; set; }

        /// <summary>
        ///   Identifies the <see cref="IsFrozen"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsFrozenProperty =
            DependencyProperty.Register(
                nameof(IsFrozen),
                typeof(bool),
                typeof(PresetManagerColumnViewModel),
                new PropertyMetadata(false));

        /// <summary>
        ///   Gets or sets whether the column is frozen.
        /// </summary>
        public bool IsFrozen
        {
            get { return (bool)GetValue(IsFrozenProperty); }
            set { SetValue(IsFrozenProperty, value); }
        }

        #endregion

        #region public SupportedFormat CellFormat

        public static readonly DependencyProperty CellFormatProperty =
            DependencyProperty.Register(
                nameof(CellFormat),
                typeof(SupportedFormat),
                typeof(PresetManagerColumnViewModel),
                new PropertyMetadata(
                    new SupportedFormat(),
                    (d, e) => ((PresetManagerColumnViewModel)d).OnPresetPropertyChanged()));

        public SupportedFormat CellFormat
        {
            get { return (SupportedFormat)GetValue(CellFormatProperty); }
            private set { SetValue(CellFormatProperty, value); }
        }

        #endregion

        private void RefreshFromPreset()
        {
            refreshingFromPreset = true;
            try {
                Name = Preset.Name;
                AutomationProperties.SetName(this, Preset.Name);
                Id = Preset.Id;
                Width = Preset.Width;
                IsVisible = Preset.IsVisible;
                TextAlignment = Preset.TextAlignment;
                //HelpText = preset.HelpText;
            } finally {
                refreshingFromPreset = false;
            }
        }

        public void RefreshPositionDependentProperties()
        {
            int index = PresetManager.PresetColumns.IndexOf(this);
            var leftFrozenIndex = PresetManager.LastLeftFrozenIndex;
            var rightFrozenIndex = PresetManager.FirstRightFrozenIndex;

            IsFrozen =
                index >= 0 &&
                (leftFrozenIndex != -1 && index <= leftFrozenIndex ||
                 rightFrozenIndex != -1 && index >= rightFrozenIndex);
        }

        private void OnPresetPropertyChanged()
        {
            if (!refreshingFromPreset && UpdateColumnPreset())
                PresetManager.SetDialogStateDirty(true);
        }

        private bool UpdateColumnPreset()
        {
            if (Preset == null)
                return false;

            bool updated = false;
            Update(Width, Preset.Width, x => Preset.Width = x, ref updated);
            Update(IsVisible, Preset.IsVisible, x => Preset.IsVisible = x, ref updated);
            Update(TextAlignment, Preset.TextAlignment, x => Preset.TextAlignment = x, ref updated);
            Update(CellFormat, GetSupportedFormat(Preset.CellFormat),
                x => Preset.CellFormat = x.Format, ref updated);

            return updated;
        }

        private SupportedFormat GetSupportedFormat(string format)
        {
            var supportedFormat = SupportedFormats.FirstOrDefault(x => x.Format == format);
            return supportedFormat.HasValue ? supportedFormat : defaultSupportedFormat;
        }

        private static void Update<T>(T newValue, T currentValue, Action<T> updateValue, ref bool updated)
        {
            if (!EqualityComparer<T>.Default.Equals(newValue, currentValue)) {
                updateValue(newValue);
                updated = true;
            }
        }
    }

    public class PresetManagerDesignTimeModel : PresetManagerViewModel
    {
        public PresetManagerDesignTimeModel()
            : base(CreateModel(), PersistenceManager.Instance)
        {
        }

        private static AsyncDataViewModel CreateModel()
        {
            var idPreset = new ColumnViewModelPreset {
                Id = new Guid("A27E5F00-BCA0-4BFE-B43D-EAA4B3F20D42"),
                Name = "Id",
                IsVisible = true,
                Width = 80
            }.EnsureFrozen();
            var namePreset = new ColumnViewModelPreset {
                Id = new Guid("3050F05D-FDCC-43AC-AA63-72CF17E5B7FF"),
                Name = "Name",
                IsVisible = true,
                Width = 200
            }.EnsureFrozen();

            var template = new AsyncDataViewModelPreset();
            var table = new DataTable("Design");

            AddColumn(table, template, idPreset, DataColumn.Create(x => x));
            AddColumn(table, template, namePreset, DataColumn.Create(x => "Name" + x));

            var dataView = new DataView(table, new DefaultFormatProviderSource());
            return new AsyncDataViewModel(dataView, template, new HdvViewModelPresetCollection()) {
                Preset = template
            };
        }

        private static void AddColumn(
            DataTable table, AsyncDataViewModelPreset preset,
            ColumnViewModelPreset columnPreset, DataColumn column)
        {
            column.Id = columnPreset.Id;
            column.Name = columnPreset.Name;
            column.Width = columnPreset.Width;
            column.IsVisible = columnPreset.IsVisible;
            column.IsResizable = true;
            column.TextAlignment = columnPreset.TextAlignment;
            preset.ConfigurableColumns.Add(columnPreset);
            table.Add(column);
        }
    }
}
