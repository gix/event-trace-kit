namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Windows;

    public class AsyncDataGridColumn : DependencyObject
    {
        private readonly AsyncDataGridColumnsViewModel columns;
        private readonly DataColumnView columnModel;
        private readonly AsyncDataViewModel adv;
        private readonly bool isInitializing;

        private CellValue[] cachedRowValues = new CellValue[0];
        private int startCacheRowValueIndex;
        private bool isResizing;

        public AsyncDataGridColumn(
            AsyncDataGridColumnsViewModel columns,
            DataColumnView columnModel,
            AsyncDataViewModel adv,
            bool isDisconnected)
        {
            this.columns = columns;
            this.columnModel = columnModel;
            this.adv = adv;
            IsDisconnected = isDisconnected;

            isInitializing = true;
            if (isDisconnected) {
                ModelColumnIndex = -1;
                ModelVisibleColumnIndex = -1;
                IsVisible = true;
                Width = 45.0;
                TextAlignment = TextAlignment.Right;
            }

            ColumnName = columnModel.Name;
            TextAlignment = TextAlignment.Left;

            CoerceValue(WidthProperty);
            RefreshViewModelFromModel();
            isInitializing = false;
        }

        public AsyncDataGridColumnsViewModel Columns => columns;
        internal DataColumnView ColumnModel => columnModel;

        public bool CanMove => IsVisible && (!IsDisconnected || IsKeySeparator || IsFreezableAreaSeparator);

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
                typeof(AsyncDataGridColumn),
                new PropertyMetadata(
                    80.0,
                    (d, e) => ((AsyncDataGridColumn)d).OnUIPropertyChanged(e),
                    (d, v) => ((AsyncDataGridColumn)d).CoerceWidth((double)v)));

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
            return Math.Max(baseValue, IsConfigurable ? 16 : 5);
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
                typeof(AsyncDataGridColumn),
                new PropertyMetadata(Boxed.False, OnIsVisibleChanged));

        /// <summary>
        ///   Gets or sets whether the column is visible.
        /// </summary>
        public bool IsVisible
        {
            get { return (bool)GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, Boxed.Bool(value)); }
        }

        private static void OnIsVisibleChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (AsyncDataGridColumn)d;
            source.OnIsVisibleChanged((bool)e.NewValue);
        }

        private void OnIsVisibleChanged(bool newValue)
        {
            var preset = adv.Preset;
            if (ModelColumnIndex < 0 || adv == null || isInitializing ||
                preset.GetColumnVisibility(ModelColumnIndex) == newValue)
                return;

            adv.Preset = preset.SetColumnVisibility(ModelColumnIndex, newValue);
        }

        #endregion

        #region public bool IsKeySeparator { get; set; }

        private static readonly DependencyPropertyKey IsSeparatorPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsKeySeparator),
                typeof(bool),
                typeof(AsyncDataGridColumn),
                new PropertyMetadata(Boxed.False));

        /// <summary>
        ///   Identifies the <see cref="IsKeySeparator"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsKeySeparatorProperty =
            IsSeparatorPropertyKey.DependencyProperty;

        /// <summary>
        ///   Gets a value indicating whether the column is a key separator.
        /// </summary>
        public bool IsKeySeparator
        {
            get { return (bool)GetValue(IsKeySeparatorProperty); }
            private set { SetValue(IsSeparatorPropertyKey, value); }
        }

        #endregion

        #region public bool IsFreezableAreaSeparator { get; private set; }

        private static readonly DependencyPropertyKey IsFreezableAreaSeparatorPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsFreezableAreaSeparator),
                typeof(bool),
                typeof(AsyncDataGridColumn),
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
                typeof(AsyncDataGridColumn),
                new PropertyMetadata(
                    TextAlignmentBoxes.Left,
                    (d, e) => ((AsyncDataGridColumn)d).OnUIPropertyChanged(e)));

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

        #region public string CellFormat { get; set; }

        /// <summary>
        ///   Identifies the <see cref="CellFormatProperty"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CellFormatProperty =
            DependencyProperty.Register(
                nameof(CellFormat),
                typeof(string),
                typeof(AsyncDataGridColumn),
                new PropertyMetadata(
                    null,
                    (d, e) => ((AsyncDataGridColumn)d).OnUIPropertyChanged(e)));

        /// <summary>
        ///   Gets or sets the cell format.
        /// </summary>
        public string CellFormat
        {
            get { return (string)GetValue(CellFormatProperty); }
            set { SetValue(CellFormatProperty, value); }
        }

        #endregion

        #region public bool IsResizable { get; private set; }

        private static readonly DependencyPropertyKey IsResizablePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsResizable),
                typeof(bool),
                typeof(AsyncDataGridColumn),
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
                typeof(AsyncDataGridColumn),
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
                typeof(AsyncDataGridColumn),
                new PropertyMetadata(
                    null,
                    (d, e) => ((AsyncDataGridColumn)d).OnColumnNameChanged(e)));

        /// <summary>
        ///   Identifies the <see cref="ColumnName"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColumnNameProperty =
            ColumnNamePropertyKey.DependencyProperty;

        private object cachedAdvValidityToken;

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

        internal bool IsSafeToReadCellValuesFromUIThread { get; set; } = true;

        public bool CanSort { get; } = false;

        public bool IsConfigurable => !IsKeySeparator && !IsFreezableAreaSeparator && IsConnected;

        public bool IsSeparator => IsKeySeparator || IsFreezableAreaSeparator;

        public bool IsDisconnected { get; }

        public bool IsConnected => !IsDisconnected;

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

        private void OnUIPropertyChanged(DependencyPropertyChangedEventArgs args)
        {
            adv?.OnUIPropertyChanged(this, args);
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

            if (adv.IsReady) {
                AsyncDataViewModelPreset preset = adv.CreatePresetFromModifiedUI();
                preset.IsUIModified = true;
                adv.Preset = preset;
            }
        }

        public CellValue GetCellValue(int rowIndex, int viewportSizeHint)
        {
            if (!IsDisconnected)
                return GetCachedCellValue(rowIndex, viewportSizeHint);
            return GetDisconnectedCellValue(rowIndex);
        }

        public CellValue GetDisconnectedCellValue(int rowIndex)
        {
            return new CellValue(null, null, null);
        }

        private void ClearCachedRows()
        {
            Array.Clear(cachedRowValues, 0, cachedRowValues.Length);
            cachedAdvValidityToken = adv.DataValidityToken;
        }

        private int GetCacheIndex(int rowIndex)
        {
            int offset = rowIndex - startCacheRowValueIndex;
            if (rowIndex >= startCacheRowValueIndex &&
                offset < cachedRowValues.Length &&
                adv.IsValidDataValidityToken(cachedAdvValidityToken))
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

        public CellValue GetCellValueNotCached(int rowIndex)
        {
            return adv.GetCellValue(rowIndex, ModelVisibleColumnIndex);
        }

        public bool IsFrozen()
        {
            return columns.IsFrozenVisibleColumn(this);
        }

        private int GetModelColumnIndex()
        {
            if (IsDisconnected)
                return -1;

            int index = adv.DataView.Columns.IndexOf(columnModel);
            if (index == -1)
                throw new Exception("Unable to find the model column index for: " + ColumnName);
            return index;
        }

        private int GetModelVisibleColumnIndex()
        {
            if (IsDisconnected || !columnModel.IsVisible)
                return -1;

            int index = adv.DataView.VisibleColumns.IndexOf(columnModel);
            if (index == -1)
                throw new Exception("Unable to find the visible model column index for: " + ColumnName);
            return index;
        }

        private void RefreshViewModelFromModel()
        {
            if (this is FreezableAreaSeparatorColumn) {
                IsFreezableAreaSeparator = true;
                IsResizable = false;
                Width = 5.0;
                return;
            }

            if (this is ExpanderHeaderColumn) {
                IsExpanderHeader = true;
                IsResizable = false;
                return;
            }

            IsResizable = true;

            ClearCachedRows();
            ModelColumnIndex = GetModelColumnIndex();
            ModelVisibleColumnIndex = GetModelVisibleColumnIndex();
            IsVisible = columnModel.IsVisible;
        }

        public ColumnViewModelPreset ToPreset()
        {
            var preset = new ColumnViewModelPreset {
                Id = columnModel.ColumnId,
                Name = columnModel.Name,
                IsVisible = IsVisible,
                Width = (int)Width,
                TextAlignment = TextAlignment,
                CellFormat = CellFormat,
                HelpText = columnModel.HelpText,
            };
            return preset;
        }
    }
}
