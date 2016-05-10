namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Windows;

    public class VirtualizedDataGridColumn : DependencyObject
    {
        private readonly VirtualizedDataGridColumnsViewModel columnsViewModel;
        private readonly IDataColumn columnModel;
        private readonly IDataView dataView;

        private bool isResizing;

        public VirtualizedDataGridColumn(
            VirtualizedDataGridColumnsViewModel columnsViewModel,
            IDataColumn columnModel,
            IDataView dataView)
        {
            this.columnsViewModel = columnsViewModel;
            this.columnModel = columnModel;
            this.dataView = dataView;

            ColumnName = columnModel.Name;
            Width = columnModel.Width;
            TextAlignment = columnModel.TextAlignment;

            CoerceValue(WidthProperty);
            RefreshViewModelFromModel();
        }

        public VirtualizedDataGridColumnsViewModel Columns => columnsViewModel;

        public bool CanMove => IsVisible;

        public int ModelColumnIndex { get; private set; }

        internal bool IsResizing => isResizing;

        public int ModelVisibleColumnIndex { get; set; }

        #region public double Width { get; set; }

        /// <summary>
        ///   Identifies the <see cref="Width"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty WidthProperty =
            DependencyProperty.Register(
                nameof(Width),
                typeof(double),
                typeof(VirtualizedDataGridColumn),
                new PropertyMetadata(
                    80.0,
                    (d, e) => ((VirtualizedDataGridColumn)d).OnWidthChanged(),
                    (d, v) => ((VirtualizedDataGridColumn)d).CoerceWidth((double)v)));

        /// <summary>
        ///   Gets or sets the column width.
        /// </summary>
        public double Width
        {
            get { return (double)GetValue(WidthProperty); }
            set { SetValue(WidthProperty, value); }
        }

        private object CoerceWidth(double baseValue)
        {
            return Math.Max(baseValue, 16);
        }

        private void OnWidthChanged()
        {
            if (!isResizing) {
                //this.columnsViewModel.UpdateFreezableColumnsWidth();
            }

            OnUIPropertyChanged();
        }

        #endregion

        #region public bool IsVisible { get; set; }

        /// <summary>
        ///   Identifies the <see cref="IsVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsVisibleProperty =
            DependencyProperty.Register(
                nameof(IsVisible),
                typeof(bool),
                typeof(VirtualizedDataGridColumn),
                new PropertyMetadata(Boxed.False));

        /// <summary>
        ///   Gets or sets whether the column is visible.
        /// </summary>
        public bool IsVisible
        {
            get { return (bool)GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, Boxed.Bool(value)); }
        }

        #endregion

        #region public bool IsSeparator { get; set; }

        private static readonly DependencyPropertyKey IsSeparatorPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsSeparator),
                typeof(bool),
                typeof(VirtualizedDataGridColumn),
                new PropertyMetadata(Boxed.False));

        /// <summary>
        ///   Identifies the <see cref="IsSeparator"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsSeparatorProperty =
            IsSeparatorPropertyKey.DependencyProperty;

        /// <summary>
        ///   Gets a value indicating whether the column is a separator.
        /// </summary>
        public bool IsSeparator
        {
            get { return (bool)GetValue(IsSeparatorProperty); }
            private set { SetValue(IsSeparatorPropertyKey, value); }
        }

        #endregion

        #region public bool IsFreezableAreaSeparator { get; private set; }

        private static readonly DependencyPropertyKey IsFreezableAreaSeparatorPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsFreezableAreaSeparator),
                typeof(bool),
                typeof(VirtualizedDataGridColumn),
                new PropertyMetadata(Boxed.False));

        /// <summary>
        ///   Identifies the <see cref="IsFreezableAreaSeparator"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsFreezableAreaSeparatorProperty =
            IsFreezableAreaSeparatorPropertyKey.DependencyProperty;

        /// <summary>
        ///   Gets a value indicating whether the column is a freezable area
        ///   separator.
        /// </summary>
        public bool IsFreezableAreaSeparator
        {
            get { return (bool)GetValue(IsFreezableAreaSeparatorProperty); }
            private set { SetValue(IsFreezableAreaSeparatorPropertyKey, value); }
        }

        #endregion

        #region public TextAlignment TextAlignment { get; private set; }

        private static readonly DependencyPropertyKey TextAlignmentPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(TextAlignment),
                typeof(TextAlignment),
                typeof(VirtualizedDataGridColumn),
                new PropertyMetadata(
                    TextAlignmentBoxes.Left,
                    (d, e) => ((VirtualizedDataGridColumn)d).OnUIPropertyChanged()));

        /// <summary>
        ///   Identifies the <see cref="TextAlignment"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextAlignmentProperty =
            TextAlignmentPropertyKey.DependencyProperty;

        /// <summary>
        ///   Gets or sets the text alignment.
        /// </summary>
        public TextAlignment TextAlignment
        {
            get { return (TextAlignment)GetValue(TextAlignmentProperty); }
            set { SetValue(TextAlignmentPropertyKey, TextAlignmentBoxes.Box(value)); }
        }

        #endregion

        #region public bool IsResizable { get; private set; }

        private static readonly DependencyPropertyKey IsResizablePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsResizable),
                typeof(bool),
                typeof(VirtualizedDataGridColumn),
                new PropertyMetadata(Boxed.False));

        /// <summary>
        ///   Identifies the <see cref="IsResizable"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsResizableProperty =
            IsResizablePropertyKey.DependencyProperty;

        /// <summary>
        ///   Gets a value indicating whether the column is resizable.
        /// </summary>
        public bool IsResizable
        {
            get { return (bool)GetValue(IsResizableProperty); }
            protected set { SetValue(IsResizablePropertyKey, Boxed.Bool(value)); }
        }

        #endregion

        #region public bool IsExpanderHeader { get; private set; }

        private static readonly DependencyPropertyKey IsExpanderHeaderPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsExpanderHeader),
                typeof(bool),
                typeof(VirtualizedDataGridColumn),
                new PropertyMetadata(Boxed.False));

        /// <summary>
        ///   Identifies the <see cref="IsExpanderHeader"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsExpanderHeaderProperty =
            IsExpanderHeaderPropertyKey.DependencyProperty;

        /// <summary>
        ///   Gets a value indicating whether this is an expander header.
        /// </summary>
        public bool IsExpanderHeader
        {
            get { return (bool)GetValue(IsExpanderHeaderProperty); }
            private set { SetValue(IsExpanderHeaderPropertyKey, Boxed.Bool(value)); }
        }

        #endregion

        #region public string ColumnName { get; private set; }

        private static readonly DependencyPropertyKey ColumnNamePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ColumnName),
                typeof(string),
                typeof(VirtualizedDataGridColumn),
                new PropertyMetadata(
                    null,
                    (d, e) => ((VirtualizedDataGridColumn)d).OnColumnNameChanged(e)));

        /// <summary>
        ///   Identifies the <see cref="ColumnName"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColumnNameProperty =
            ColumnNamePropertyKey.DependencyProperty;

        /// <summary>
        ///   Gets or sets the column name.
        /// </summary>
        public string ColumnName
        {
            get { return (string)GetValue(ColumnNameProperty); }
            set { SetValue(ColumnNamePropertyKey, value); }
        }

        private void OnColumnNameChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateAutomationNameProperty();
            //CoerceValue(ColumnToolTipProperty);
        }

        #endregion

        public bool IsSafeToReadCellValuesFromUIThread { get; } = true;

        public bool CanSort { get; private set; } = false;

        private void UpdateAutomationNameProperty()
        {
            //string str;
            //string columnName = ColumnName;
            //if (!string.IsNullOrWhiteSpace(columnName)) {
            //    str = columnName;
            //} else {
            //    Type dataType = this.DataType;
            //    if (dataType == typeof(FreezableAreaSeparatorColumn)) {
            //        str = "Freezable Area Separator";
            //    } else if (dataType == typeof(KeysValuesSeparatorColumn)) {
            //        str = "Keys/Values Separator";
            //    } else {
            //        str = "Unknown";
            //    }
            //}

            //AutomationProperties.SetName(this, str);
        }

        private void OnUIPropertyChanged()
        {
            columnsViewModel.Owner.RaiseUpdate(false);
        }

        public void BeginPossiblyResizing()
        {
            isResizing = true;
        }

        public void EndPossiblyResizing(double horizontalChange)
        {
            isResizing = false;
            if (horizontalChange == 0)
                return;

            //this.columnsViewModel.UpdateFreezableColumnsWidth();
            //if (this.hdvViewModel.IsReady) {
            //    HdvViewModelPreset preset = this.HdvViewModel.CreatePresetFromUIThatHasBeenModified();
            //    preset.IsModifiedFromUI = true;
            //    this.hdvViewModel.HdvViewModelPreset = preset;
            //}
        }

        public CellValue GetCellValue(int rowIndex, int viewportSizeHint)
        {
            return GetCachedCellValue(rowIndex, viewportSizeHint);
        }

        private CellValue[] cachedRowValues = new CellValue[0];
        private int startCacheRowValueIndex;

        private void ClearCachedRows()
        {
            Array.Clear(cachedRowValues, 0, cachedRowValues.Length);
            //this.cachedHdvValidityToken = this.hdvViewModel.DataValidityToken;
            //this.cachedColumnValidityToken = this.ColumnModel.Column.DataValidityToken;
        }

        private int GetCacheIndex(int rowIndex)
        {
            int offset = rowIndex - startCacheRowValueIndex;
            if (rowIndex >= startCacheRowValueIndex &&
                offset < cachedRowValues.Length // &&
                                                //(this.hdvViewModel.IsValidDataValidityToken(this.cachedHdvValidityToken) &&
                                                //(this.cachedColumnValidityToken == this.ColumnModel.Column.DataValidityToken))
                )
                return offset;

            return -1;
        }

        private CellValue GetCachedCellValue(int rowIndex, int viewportSize)
        {
            int num = viewportSize * 3;
            if (cachedRowValues.Length < num) {
                cachedRowValues = new CellValue[num];
                startCacheRowValueIndex = -1;
            }

            int cacheIndex = GetCacheIndex(rowIndex);
            if (cacheIndex == -1) {
                ClearCachedRows();
                startCacheRowValueIndex = Math.Max(0, rowIndex - cachedRowValues.Length / 2);
                cacheIndex = rowIndex - startCacheRowValueIndex;
            }

            CellValue value = cachedRowValues[cacheIndex];
            if (value == null) {
                value = GetCellValueNotCached(rowIndex);
                cachedRowValues[cacheIndex] = value;
            }

            return value;

        }

        private CellValue GetCellValueNotCached(int rowIndex)
        {
            return dataView.GetCellValue(rowIndex, ModelVisibleColumnIndex);
        }

        public bool IsInFreezableArea()
        {
            return false;
        }

        private int GetModelColumnIndex()
        {
            int index = dataView.Columns.IndexOf(columnModel);
            if (index == -1)
                throw new Exception("Unable to find the model column index for: " + ColumnName);
            return index;
        }

        private int GetModelVisibleColumnIndex()
        {
            if (!columnModel.IsVisible)
                return -1;

            int index = dataView.VisibleColumns.IndexOf(columnModel);
            if (index == -1)
                throw new Exception("Unable to find the visible model column index for: " + ColumnName);
            return index;
        }

        internal void RefreshViewModelFromModel()
        {
            //if (columnModel.DataType == typeof(KeysValuesSeparatorColumn)) {
            //    IsSeparator = true;
            //    IsResizable = false;
            //    Width = 5.0;
            //} else if (columnModel.DataType == typeof(FreezableAreaSeparatorColumn)) {
            //    IsFreezableAreaSeparator = true;
            //    IsResizable = false;
            //    Width = 5.0;
            //} else if (columnModel.DataType == typeof(ExpanderHeaderColumn)) {
            //    IsResizable = false;
            //    IsExpanderHeader = true;
            //} else {
            IsResizable = true;
            //}

            ClearCachedRows();
            ModelColumnIndex = GetModelColumnIndex();
            ModelVisibleColumnIndex = GetModelVisibleColumnIndex();
            IsVisible = columnModel.IsVisible;
        }
    }
}
