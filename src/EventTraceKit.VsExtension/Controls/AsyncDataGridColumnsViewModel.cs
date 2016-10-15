namespace EventTraceKit.VsExtension.Controls
{
    using System.Collections.ObjectModel;
    using System.Windows;
    using Primitives;

    public class AsyncDataGridColumnsViewModel : DependencyObject
    {
        private readonly ObservableCollection<AsyncDataGridColumn> columns;
        private readonly ObservableCollection<AsyncDataGridColumn> visibleColumns;
        private readonly ObservableCollection<AsyncDataGridColumn> configurableColumns;

        public AsyncDataGridColumnsViewModel(AsyncDataViewModel advModel)
        {
            AdvModel = advModel;
            columns = new ObservableCollection<AsyncDataGridColumn>();

            visibleColumns = new ObservableCollection<AsyncDataGridColumn>();
            VisibleColumns = new ReadOnlyObservableCollection<AsyncDataGridColumn>(visibleColumns);

            configurableColumns = new ObservableCollection<AsyncDataGridColumn>();
            ConfigurableColumns = new ReadOnlyObservableCollection<AsyncDataGridColumn>(configurableColumns);

            ExpanderHeaderColumn = new ExpanderHeaderColumn(this, advModel);
        }

        internal AsyncDataViewModel AdvModel { get; }
        internal ObservableCollection<AsyncDataGridColumn> WritableColumns => columns;

        #region public ReadOnlyObservableCollection<AsyncDataGridColumnViewModel> VisibleColumns

        private static readonly DependencyPropertyKey VisibleColumnsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(VisibleColumns),
                typeof(ReadOnlyObservableCollection<AsyncDataGridColumn>),
                typeof(AsyncDataGridColumnsViewModel),
                new PropertyMetadata(
                    CollectionDefaults<AsyncDataGridColumn>.ReadOnlyObservable));

        public static readonly DependencyProperty VisibleColumnsProperty =
            VisibleColumnsPropertyKey.DependencyProperty;

        public ReadOnlyObservableCollection<AsyncDataGridColumn> VisibleColumns
        {
            get
            {
                return (ReadOnlyObservableCollection<AsyncDataGridColumn>)
                    GetValue(VisibleColumnsProperty);
            }
            private set { SetValue(VisibleColumnsPropertyKey, value); }
        }

        #endregion

        #region public ReadOnlyObservableCollection<AsyncDataGridColumn> ConfigurableColumns

        /// <summary>
        ///   Identifies the <see cref="ConfigurableColumns"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ConfigurableColumnsProperty =
            DependencyProperty.Register(
                nameof(ConfigurableColumns),
                typeof(ReadOnlyObservableCollection<AsyncDataGridColumn>),
                typeof(AsyncDataGridColumnsViewModel),
                new PropertyMetadata(null));

        /// <summary>
        ///   Gets or sets the configurable columns.
        /// </summary>
        public ReadOnlyObservableCollection<AsyncDataGridColumn> ConfigurableColumns
        {
            get
            {
                return (ReadOnlyObservableCollection<AsyncDataGridColumn>)
                  GetValue(ConfigurableColumnsProperty);
            }
            private set { SetValue(ConfigurableColumnsProperty, value); }
        }

        #endregion

        #region public Visibility Visibility { get; private set; }

        private static readonly DependencyPropertyKey VisibilityPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(Visibility),
                typeof(Visibility),
                typeof(AsyncDataGridColumnsViewModel),
                new PropertyMetadata(Visibility.Visible));

        /// <summary>
        ///   Identifies the <see cref="Visibility"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty VisibilityProperty =
            VisibilityPropertyKey.DependencyProperty;

        /// <summary>
        ///   Gets or sets the visibility.
        /// </summary>
        public Visibility Visibility
        {
            get { return (Visibility)GetValue(VisibilityProperty); }
            set { SetValue(VisibilityPropertyKey, value); }
        }

        #endregion

        #region public AsyncDataGridColumnViewModel ExpanderHeaderColumn { get; private set; }

        private static readonly DependencyPropertyKey ExpanderHeaderColumnPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ExpanderHeaderColumn),
                typeof(AsyncDataGridColumn),
                typeof(AsyncDataGridColumnsViewModel),
                new PropertyMetadata(null));

        /// <summary>
        ///   Identifies the <see cref="ExpanderHeaderColumn"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ExpanderHeaderColumnProperty =
            ExpanderHeaderColumnPropertyKey.DependencyProperty;

        /// <summary>
        ///   Gets or sets the expander header column.
        /// </summary>
        protected AsyncDataGridColumn ExpanderHeaderColumn
        {
            get { return (AsyncDataGridColumn)GetValue(ExpanderHeaderColumnProperty); }
            set { SetValue(ExpanderHeaderColumnPropertyKey, value); }
        }

        #endregion

        #region public int LeftFrozenColumnCount { get; set; }

        /// <summary>
        ///   Identifies the <see cref="LeftFrozenColumnCount"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LeftFrozenColumnCountProperty =
            DependencyProperty.Register(
                nameof(LeftFrozenColumnCount),
                typeof(int),
                typeof(AsyncDataGridColumnsViewModel),
                new PropertyMetadata(0));

        /// <summary>
        ///   Gets or sets the left frozen column count.
        /// </summary>
        public int LeftFrozenColumnCount
        {
            get { return (int)GetValue(LeftFrozenColumnCountProperty); }
            set { SetValue(LeftFrozenColumnCountProperty, value); }
        }

        #endregion

        #region public int RightFrozenColumnCount { get; set; }

        /// <summary>
        ///   Identifies the <see cref="RightFrozenColumnCount"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RightFrozenColumnCountProperty =
            DependencyProperty.Register(
                nameof(RightFrozenColumnCount),
                typeof(int),
                typeof(AsyncDataGridColumnsViewModel),
                new PropertyMetadata(0));

        /// <summary>
        ///   Gets or sets the right frozen column count.
        /// </summary>
        public int RightFrozenColumnCount
        {
            get { return (int)GetValue(RightFrozenColumnCountProperty); }
            set { SetValue(RightFrozenColumnCountProperty, value); }
        }

        #endregion

        internal void RefreshAllObservableCollections()
        {
            visibleColumns.Clear();
            foreach (var column in columns) {
                if (column.IsVisible)
                    visibleColumns.Add(column);
            }

            //this.AdjustFrozenCounts();

            configurableColumns.Clear();
            foreach (var column in columns) {
                if (column.IsConfigurable)
                    configurableColumns.Add(column);
            }
        }

        public void TryMoveColumn(
            AsyncDataGridColumn srcColumn, AsyncDataGridColumn dstColumn)
        {
            int oldIndex = columns.IndexOf(srcColumn);
            int newIndex = columns.IndexOf(dstColumn);
            if (oldIndex != newIndex)
                WritableColumns.Move(oldIndex, newIndex);

            RefreshAllObservableCollections();
            AsyncDataViewModelPreset preset = AdvModel.CreatePresetFromModifiedUI();
            AdvModel.Preset = preset;
        }

        protected bool IsApplyingPreset { get; set; }

        internal void ApplyPresetAssumeGridModelInSync(AsyncDataViewModelPreset preset)
        {
            IsApplyingPreset = true;
            try {
                AdvModel.DataView.BeginDataUpdate();
                WritableColumns.Clear();

                int index = 0;
                foreach (var dataColumn in AdvModel.DataView.Columns) {
                    var column = new AsyncDataGridColumn(this, dataColumn, AdvModel, false);
                    WritableColumns.Add(column);

                    var columnPreset = preset.ConfigurableColumns[index];
                    column.Width = columnPreset.Width;
                    column.TextAlignment = columnPreset.TextAlignment;
                    column.CellFormat = columnPreset.CellFormat;
                    ++index;
                }

                LeftFrozenColumnCount = 0; //preset.LeftFrozenColumnCount;
                RightFrozenColumnCount = 0; //preset.RightFrozenColumnCount;
                                            //preset.PlaceSeparatorsInList(
                                            //    WritableColumns,
                                            //    LeftFreezableAreaSeparatorColumn,
                                            //    RightFreezableAreaSeparatorColumn);
                AdvModel.DataView.EndDataUpdate();
                RefreshAllObservableCollections();
            } finally {
                IsApplyingPreset = false;
                //UpdateFreezableColumnsWidth();
            }
        }

        internal void OnUIPropertyChanged(AsyncDataGridColumn column)
        {
            if (!IsApplyingPreset)
                AdvModel.RequestUpdate(false);
        }
    }
}
