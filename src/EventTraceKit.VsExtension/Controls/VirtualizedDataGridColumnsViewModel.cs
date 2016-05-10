namespace EventTraceKit.VsExtension.Controls
{
    using System.Collections.ObjectModel;
    using System.Windows;
    using EventTraceKit.VsExtension.Controls.Primitives;

    public class VirtualizedDataGridColumnsViewModel : DependencyObject
    {
        private readonly IDataView dataView;
        private readonly ObservableCollection<VirtualizedDataGridColumn> columns;
        private readonly ObservableCollection<VirtualizedDataGridColumn> visibleColumns;

        public VirtualizedDataGridColumnsViewModel(
            IDataView dataView, VirtualizedDataGridViewModel owner)
        {
            this.dataView = dataView;
            Owner = owner;
            columns = new ObservableCollection<VirtualizedDataGridColumn>();

            visibleColumns = new ObservableCollection<VirtualizedDataGridColumn>();
            VisibleColumns = new ReadOnlyObservableCollection<VirtualizedDataGridColumn>(visibleColumns);

            //ExpanderHeaderColumn = new ExpanderHeaderColumnViewModel(
            //    this, this.GetInternalColumnView<ExpanderHeaderColumn>(DataColumn.Create<ExpanderHeaderColumn>(new ExpanderHeaderColumn()), string.Empty, Guid.Empty), this.hdvViewModel);
        }

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
            foreach (var model in columns) {
                if (model.IsVisible)
                    visibleColumns.Add(model);
            }

            //this.AdjustFrozenCounts();

            //this.configurableColumns.Clear();
            //foreach (var model2 in this.columns) {
            //    if (model2.IsConfigurable) {
            //        this.configurableColumns.Add(model2);
            //    }
            //}

            //this.HdvViewModel.ColumnMetadataCollection.FillObservableCollection(this.columnMetadataEntryViewModels);
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

        internal void ApplyPresetAssumeGridModelInSync(/*HdvViewModelPreset preset*/)
        {
            //IsApplyingPreset = true;
            //base.Hdv.BeginDataUpdate();
            WritableColumns.Clear();
            int num = 0;
            foreach (IDataColumn column in dataView.Columns) {
                var columnViewModel = new VirtualizedDataGridColumn(
                    this, column, dataView);
                WritableColumns.Add(columnViewModel);

                //var columnPreset = preset.ConfigurableColumns[num];
                //columnViewModel.Width = columnPreset.Width;
                //columnViewModel.TextAlignment = columnPreset.TextAlignment;
                //columnViewModel.CellFormat = columnPreset.CellFormat;
                num++;
            }

            //preset.PlaceSeparatorsInList<HierarchicalDataGridColumnViewModel>(base.ColumnsWriteable, base.LeftFreezableAreaSeparatorColumn, base.RightFreezableAreaSeparatorColumn, this.graphingAreaSeparatorColumn, this.keysValuesSeparatorColumn, forcedColumnLeftCount);
            //base.Hdv.EndDataUpdate();
            RefreshAllObservableCollections();
            //IsApplyingPreset = false;
            //UpdateFreezableColumnsWidth();
        }
    }
}
