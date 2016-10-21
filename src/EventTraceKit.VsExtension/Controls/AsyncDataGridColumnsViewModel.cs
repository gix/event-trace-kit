namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows;
    using Collections;
    using Primitives;

    public class AsyncDataGridColumnsViewModel : DependencyObject
    {
        private readonly ObservableCollection<AsyncDataGridColumn> columns;
        private readonly ObservableCollection<AsyncDataGridColumn> visibleColumns;
        private readonly ObservableCollection<AsyncDataGridColumn> configurableColumns;
        private bool isApplyingPreset;

        public AsyncDataGridColumnsViewModel(AsyncDataViewModel advModel)
        {
            AdvModel = advModel;
            columns = new ObservableCollection<AsyncDataGridColumn>();

            visibleColumns = new ObservableCollection<AsyncDataGridColumn>();
            VisibleColumns = new ReadOnlyObservableCollection<AsyncDataGridColumn>(visibleColumns);

            configurableColumns = new ObservableCollection<AsyncDataGridColumn>();
            ConfigurableColumns = new ReadOnlyObservableCollection<AsyncDataGridColumn>(configurableColumns);

            ExpanderHeaderColumn = new ExpanderHeaderColumn(this, advModel);
            LeftFreezableAreaSeparatorColumn = new FreezableAreaSeparatorColumn(this, advModel);
            RightFreezableAreaSeparatorColumn = new FreezableAreaSeparatorColumn(this, advModel);
        }

        internal AsyncDataViewModel AdvModel { get; }
        internal ObservableCollection<AsyncDataGridColumn> WritableColumns => columns;

        #region public ReadOnlyObservableCollection<AsyncDataGridColumn> VisibleColumns

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

        #region public AsyncDataGridColumn ExpanderHeaderColumn { get; private set; }

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

        #region public int LeftFrozenVisibleColumnCount { get; set; }

        /// <summary>
        ///   Identifies the <see cref="LeftFrozenVisibleColumnCount"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LeftFrozenVisibleColumnCountProperty =
            DependencyProperty.Register(
                nameof(LeftFrozenVisibleColumnCount),
                typeof(int),
                typeof(AsyncDataGridColumnsViewModel),
                new PropertyMetadata(0));

        /// <summary>
        ///   Gets or sets the left frozen visible column count.
        /// </summary>
        public int LeftFrozenVisibleColumnCount
        {
            get { return (int)GetValue(LeftFrozenVisibleColumnCountProperty); }
            set { SetValue(LeftFrozenVisibleColumnCountProperty, value); }
        }

        #endregion

        #region public int RightFrozenVisibleColumnCount { get; set; }

        /// <summary>
        ///   Identifies the <see cref="RightFrozenVisibleColumnCount"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RightFrozenVisibleColumnCountProperty =
            DependencyProperty.Register(
                nameof(RightFrozenVisibleColumnCount),
                typeof(int),
                typeof(AsyncDataGridColumnsViewModel),
                new PropertyMetadata(0));

        /// <summary>
        ///   Gets or sets the right frozen visible column count.
        /// </summary>
        public int RightFrozenVisibleColumnCount
        {
            get { return (int)GetValue(RightFrozenVisibleColumnCountProperty); }
            set { SetValue(RightFrozenVisibleColumnCountProperty, value); }
        }

        #endregion

        public AsyncDataGridColumn LeftFreezableAreaSeparatorColumn { get; }

        public AsyncDataGridColumn RightFreezableAreaSeparatorColumn { get; }

        private void RefreshColumnCollections()
        {
            visibleColumns.Clear();
            visibleColumns.AddRange(columns.Where(x => x.IsVisible));

            configurableColumns.Clear();
            configurableColumns.AddRange(columns.Where(x => x.IsConfigurable));

            ColumnsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void TryMoveColumn(
            AsyncDataGridColumn srcColumn, AsyncDataGridColumn dstColumn)
        {
            int oldIndex = columns.IndexOf(srcColumn);
            int newIndex = columns.IndexOf(dstColumn);

            if (IsFrozenVisibleColumnIndex(oldIndex) != IsFrozenVisibleColumnIndex(newIndex))
                return;

            if (oldIndex != newIndex)
                WritableColumns.Move(oldIndex, newIndex);

            RefreshColumnCollections();
            AdvModel.Preset = AdvModel.CreatePresetFromModifiedUI();
        }

        internal void ApplyPresetAssumeGridModelInSync(AsyncDataViewModelPreset preset)
        {
            isApplyingPreset = true;
            try {
                var dataView = AdvModel.DataView;
                dataView.BeginDataUpdate();
                WritableColumns.Clear();

                for (int i = 0; i < dataView.Columns.Count; ++i) {
                    var columnPreset = preset.ConfigurableColumns[i];
                    var column = new AsyncDataGridColumn(this, dataView.Columns[i], AdvModel, false);
                    column.Width = columnPreset.Width;
                    column.TextAlignment = columnPreset.TextAlignment;
                    column.CellFormat = columnPreset.CellFormat;
                    WritableColumns.Add(column);
                }

                int leftFrozenVisibleColumnCount = 0;
                for (int i = 0; i < preset.LeftFrozenColumnCount; ++i) {
                    if (WritableColumns[i].IsVisible)
                        ++leftFrozenVisibleColumnCount;
                }

                int rightFrozenVisibleColumnCount = 0;
                for (int i = 0; i < preset.RightFrozenColumnCount; ++i) {
                    if (WritableColumns[preset.ConfigurableColumns.Count - i - 1].IsVisible)
                        ++rightFrozenVisibleColumnCount;
                }

                LeftFrozenColumnCount = preset.LeftFrozenColumnCount;
                RightFrozenColumnCount = preset.RightFrozenColumnCount;
                LeftFrozenVisibleColumnCount = leftFrozenVisibleColumnCount;
                RightFrozenVisibleColumnCount = rightFrozenVisibleColumnCount;

                //preset.PlaceSeparatorsInList(
                //    WritableColumns,
                //    LeftFreezableAreaSeparatorColumn,
                //    RightFreezableAreaSeparatorColumn);

                dataView.EndDataUpdate();
                RefreshColumnCollections();
            } finally {
                isApplyingPreset = false;
            }
        }

        public event EventHandler ColumnsChanged;

        internal void OnUIPropertyChanged(
            AsyncDataGridColumn column, DependencyPropertyChangedEventArgs args)
        {
            if (!isApplyingPreset)
                AdvModel.RequestUpdate(false);

            if (args.Property == AsyncDataGridColumn.WidthProperty) {
                ColumnsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool IsFrozenVisibleColumn(AsyncDataGridColumn column)
        {
            int index = visibleColumns.IndexOf(column);
            if (index == -1)
                return false;

            return IsFrozenVisibleColumnIndex(index);
        }

        private bool IsFrozenVisibleColumnIndex(int index)
        {
            return index < LeftFrozenVisibleColumnCount ||
                   index >= visibleColumns.Count - RightFrozenVisibleColumnCount;
        }
    }
}
