namespace EventTraceKit.VsExtension.Controls
{
    using System.Collections.ObjectModel;
    using System.Windows;
    using EventTraceKit.VsExtension.Controls.Primitives;

    public class AsyncDataGridColumnsViewModel : DependencyObject
    {
        private readonly ObservableCollection<AsyncDataGridColumn> columns;
        private readonly ObservableCollection<AsyncDataGridColumn> visibleColumns;
        private readonly ObservableCollection<AsyncDataGridColumn> configurableColumns;

        public AsyncDataGridColumnsViewModel(DataViewViewModel hdvViewModel)
        {
            HdvViewModel = hdvViewModel;
            columns = new ObservableCollection<AsyncDataGridColumn>();

            visibleColumns = new ObservableCollection<AsyncDataGridColumn>();
            VisibleColumns = new ReadOnlyObservableCollection<AsyncDataGridColumn>(visibleColumns);

            configurableColumns = new ObservableCollection<AsyncDataGridColumn>();
            ConfigurableColumns = new ReadOnlyObservableCollection<AsyncDataGridColumn>(configurableColumns);

            ExpanderHeaderColumn = new ExpanderHeaderColumn(this, hdvViewModel);
        }

        internal DataViewViewModel HdvViewModel { get; }
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
            HdvViewModelPreset preset = HdvViewModel.CreatePresetFromModifiedUI();
            HdvViewModel.HdvViewModelPreset = preset;
        }

        internal void ApplyPresetAssumeGridModelInSync(HdvViewModelPreset preset)
        {
            IsApplyingPreset = true;
            HdvViewModel.DataView.BeginDataUpdate();
            WritableColumns.Clear();
            int index = 0;
            foreach (var dataColumn in HdvViewModel.DataView.Columns) {
                var column = new AsyncDataGridColumn(
                    this, dataColumn, HdvViewModel, false);
                WritableColumns.Add(column);

                var columnPreset = preset.ConfigurableColumns[index];
                column.Width = columnPreset.Width;
                column.TextAlignment = columnPreset.TextAlignment;
                column.CellFormat = columnPreset.CellFormat;
                ++index;
            }

            //preset.PlaceSeparatorsInList<HierarchicalDataGridColumnViewModel>(base.ColumnsWriteable, base.LeftFreezableAreaSeparatorColumn, base.RightFreezableAreaSeparatorColumn, this.graphingAreaSeparatorColumn, this.keysValuesSeparatorColumn, forcedColumnLeftCount);
            HdvViewModel.DataView.EndDataUpdate();
            RefreshAllObservableCollections();
            IsApplyingPreset = false;
            //UpdateFreezableColumnsWidth();
        }

        protected bool IsApplyingPreset { get; set; }

        internal void OnUIPropertyChanged(AsyncDataGridColumn column)
        {
            if (!IsApplyingPreset)
                HdvViewModel.RequestUpdate(false);
        }
    }
}
