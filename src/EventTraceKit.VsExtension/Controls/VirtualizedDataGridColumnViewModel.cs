namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Windows;

    public class VirtualizedDataGridColumnViewModel : DependencyObject
    {
        private readonly VirtualizedDataGridColumnsViewModel columnsViewModel;
        private readonly IVirtualizedDataGridViewColumn model;

        private bool isResizing;

        public VirtualizedDataGridColumnViewModel(
            VirtualizedDataGridColumnsViewModel columnsViewModel,
            IVirtualizedDataGridViewColumn model)
        {
            this.columnsViewModel = columnsViewModel;
            this.model = model;

            ColumnName = model.Name;
            Width = model.Width;
            IsVisible = model.IsVisible;
            IsResizable = model.IsResizable;
        }

        internal bool IsResizing => isResizing;

        public bool CanMove => IsVisible;

        public VirtualizedDataGridColumnsViewModel Columns => columnsViewModel;

        #region public double Width { get; set; }

        /// <summary>
        ///   Identifies the <see cref="Width"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty WidthProperty =
            DependencyProperty.Register(
                nameof(Width),
                typeof(double),
                typeof(VirtualizedDataGridColumnViewModel),
                new PropertyMetadata(
                    80.0,
                    (d, e) => ((VirtualizedDataGridColumnViewModel)d).OnWidthChanged(),
                    (d, v) => ((VirtualizedDataGridColumnViewModel)d).CoerceWidth((double)v)));

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
            double num = 16.0 + /*(!this.IsConfigurable ? 5.0 : 16.0) +*/ (IsKey ? 16.0 : 0.0);
            return Math.Max(baseValue, num);
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
                typeof(VirtualizedDataGridColumnViewModel),
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
                typeof(VirtualizedDataGridColumnViewModel),
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
                typeof(VirtualizedDataGridColumnViewModel),
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
                typeof(VirtualizedDataGridColumnViewModel),
                new PropertyMetadata(
                    TextAlignmentBoxes.Left,
                    (d, e) => ((VirtualizedDataGridColumnViewModel)d).OnUIPropertyChanged()));

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
            private set { SetValue(TextAlignmentPropertyKey, TextAlignmentBoxes.Box(value)); }
        }

        #endregion

        #region public bool IsResizable { get; private set; }

        private static readonly DependencyPropertyKey IsResizablePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsResizable),
                typeof(bool),
                typeof(VirtualizedDataGridColumnViewModel),
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
                typeof(VirtualizedDataGridColumnViewModel),
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
                typeof(VirtualizedDataGridColumnViewModel),
                new PropertyMetadata(
                    null,
                    (d, e) => ((VirtualizedDataGridColumnViewModel)d).OnColumnNameChanged(e)));

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

        public bool IsKey { get; } = false;

        public bool CanSort => false;

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

        private CellValue GetCachedCellValue(int rowIndex, int viewportSizeHint)
        {
            return new CellValue("foo", null, null);
        }

        public bool IsInFreezableArea()
        {
            return false;
        }
    }
}