namespace EventTraceKit.VsExtension.Controls
{
    using System.Collections.ObjectModel;
    using System.Windows;
    using EventTraceKit.VsExtension.Controls.Primitives;

    public class VirtualizedDataGridColumnsViewModel : DependencyObject
    {
        private readonly ObservableCollection<VirtualizedDataGridColumn> columns;
        private readonly ObservableCollection<VirtualizedDataGridColumn> visibleColumns;
        private readonly ObservableCollection<VirtualizedDataGridColumn> configurableColumns;

        public VirtualizedDataGridColumnsViewModel(
            IDataView dataView, VirtualizedDataGridViewModel owner)
        {
            DataView = dataView;
            dataView.ColumnsViewModel = this;
            Owner = owner;
            columns = new ObservableCollection<VirtualizedDataGridColumn>();

            visibleColumns = new ObservableCollection<VirtualizedDataGridColumn>();
            VisibleColumns = new ReadOnlyObservableCollection<VirtualizedDataGridColumn>(visibleColumns);

            configurableColumns = new ObservableCollection<VirtualizedDataGridColumn>();
            ConfigurableColumns = new ReadOnlyObservableCollection<VirtualizedDataGridColumn>(configurableColumns);

            ExpanderHeaderColumn = new ExpanderHeaderColumn(this, dataView);
        }

        internal IDataView DataView { get; }
        internal VirtualizedDataGridViewModel Owner { get; }
        internal ObservableCollection<VirtualizedDataGridColumn> WritableColumns => columns;

        #region public ReadOnlyObservableCollection<VirtualizedDataGridColumnViewModel> VisibleColumns

        private static readonly DependencyPropertyKey VisibleColumnsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(VisibleColumns),
                typeof(ReadOnlyObservableCollection<VirtualizedDataGridColumn>),
                typeof(VirtualizedDataGridColumnsViewModel),
                new PropertyMetadata(
                    CollectionDefaults<VirtualizedDataGridColumn>.ReadOnlyObservable));

        public static readonly DependencyProperty VisibleColumnsProperty =
            VisibleColumnsPropertyKey.DependencyProperty;

        public ReadOnlyObservableCollection<VirtualizedDataGridColumn> VisibleColumns
        {
            get
            {
                return (ReadOnlyObservableCollection<VirtualizedDataGridColumn>)
                    GetValue(VisibleColumnsProperty);
            }
            private set { SetValue(VisibleColumnsPropertyKey, value); }
        }

        #endregion

        #region public ReadOnlyObservableCollection<VirtualizedDataGridColumn> ConfigurableColumns

        /// <summary>
        ///   Identifies the <see cref="ConfigurableColumns"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ConfigurableColumnsProperty =
            DependencyProperty.Register(
                nameof(ConfigurableColumns),
                typeof(ReadOnlyObservableCollection<VirtualizedDataGridColumn>),
                typeof(VirtualizedDataGridColumnsViewModel),
                new PropertyMetadata(null));

        /// <summary>
        ///   Gets or sets the configurable columns.
        /// </summary>
        public ReadOnlyObservableCollection<VirtualizedDataGridColumn> ConfigurableColumns
        {
            get
            {
                return (ReadOnlyObservableCollection<VirtualizedDataGridColumn>)
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
                typeof(VirtualizedDataGridColumnsViewModel),
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

        #region public VirtualizedDataGridColumnViewModel ExpanderHeaderColumn { get; private set; }

        private static readonly DependencyPropertyKey ExpanderHeaderColumnPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ExpanderHeaderColumn),
                typeof(VirtualizedDataGridColumn),
                typeof(VirtualizedDataGridColumnsViewModel),
                new PropertyMetadata(null));

        /// <summary>
        ///   Identifies the <see cref="ExpanderHeaderColumn"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ExpanderHeaderColumnProperty =
            ExpanderHeaderColumnPropertyKey.DependencyProperty;

        /// <summary>
        ///   Gets or sets the expander header column.
        /// </summary>
        protected VirtualizedDataGridColumn ExpanderHeaderColumn
        {
            get { return (VirtualizedDataGridColumn)GetValue(ExpanderHeaderColumnProperty); }
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
            VirtualizedDataGridColumn srcColumn,
            VirtualizedDataGridColumn dstColumn)
        {
            int oldIndex = columns.IndexOf(srcColumn);
            int newIndex = columns.IndexOf(dstColumn);
            if (oldIndex != newIndex)
                WritableColumns.Move(oldIndex, newIndex);
            RefreshAllObservableCollections();
        }

        internal void ApplyPresetAssumeGridModelInSync(HdvViewModelPreset preset)
        {
            //IsApplyingPreset = true;
            //base.Hdv.BeginDataUpdate();
            WritableColumns.Clear();
            int index = 0;
            foreach (IDataColumn dataColumn in DataView.Columns) {
                var column = new VirtualizedDataGridColumn(
                    this, dataColumn, DataView);
                WritableColumns.Add(column);

                //var columnPreset = preset.ConfigurableColumns[index];
                //column.Width = columnPreset.Width;
                //column.TextAlignment = columnPreset.TextAlignment;
                //column.CellFormat = columnPreset.CellFormat;
                ++index;
            }

            //preset.PlaceSeparatorsInList<HierarchicalDataGridColumnViewModel>(base.ColumnsWriteable, base.LeftFreezableAreaSeparatorColumn, base.RightFreezableAreaSeparatorColumn, this.graphingAreaSeparatorColumn, this.keysValuesSeparatorColumn, forcedColumnLeftCount);
            //base.Hdv.EndDataUpdate();
            RefreshAllObservableCollections();
            //IsApplyingPreset = false;
            //UpdateFreezableColumnsWidth();
        }
    }
}
