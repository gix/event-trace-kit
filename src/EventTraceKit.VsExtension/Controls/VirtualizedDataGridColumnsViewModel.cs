namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Collections.ObjectModel;
    using System.Windows;
    using System.Windows.Data;
    using EventTraceKit.VsExtension.Controls.Primitives;

    internal sealed class ExpanderHeaderColumnViewModel : VirtualizedDataGridColumnViewModel
    {
        public ExpanderHeaderColumnViewModel(
            VirtualizedDataGridColumnsViewModel columnsViewModel,
            IVirtualizedDataGridViewColumn model)
            : base(columnsViewModel, model)
        {
            IsVisible = true;

            //var binding = new Binding {
            //    Source = columnsViewModel,
            //    Path = new PropertyPath(nameof(columnsViewModel.ActualWidth)),
            //    Mode = BindingMode.OneWay
            //};
            //BindingOperations.SetBinding(this, WidthProperty, binding);
        }
    }

    public class VirtualizedDataGridColumnsViewModel : DependencyObject
    {
        private readonly ObservableCollection<VirtualizedDataGridColumnViewModel> columns;
        private readonly ObservableCollection<VirtualizedDataGridColumnViewModel> visibleColumns;

        public VirtualizedDataGridColumnsViewModel(IVirtualizedDataGridViewModel owner)
        {
            Owner = owner;
            columns = new ObservableCollection<VirtualizedDataGridColumnViewModel>();

            visibleColumns = new ObservableCollection<VirtualizedDataGridColumnViewModel>();
            VisibleColumns = new ReadOnlyObservableCollection<VirtualizedDataGridColumnViewModel>(visibleColumns);

            //ExpanderHeaderColumn = new ExpanderHeaderColumnViewModel(
            //    this, this.GetInternalColumnView<ExpanderHeaderColumn>(DataColumn.Create<ExpanderHeaderColumn>(new ExpanderHeaderColumn()), string.Empty, Guid.Empty), this.hdvViewModel);
        }

        internal IVirtualizedDataGridViewModel Owner { get; }
        internal ObservableCollection<VirtualizedDataGridColumnViewModel> WritableColumns => columns;

        #region public ReadOnlyObservableCollection<VirtualizedDataGridColumnViewModel> VisibleColumns

        private static readonly DependencyPropertyKey VisibleColumnsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(VisibleColumns),
                typeof(ReadOnlyObservableCollection<VirtualizedDataGridColumnViewModel>),
                typeof(VirtualizedDataGridColumnsViewModel),
                new PropertyMetadata(
                    CollectionDefaults<VirtualizedDataGridColumnViewModel>.ReadOnlyObservable));

        public static readonly DependencyProperty VisibleColumnsProperty =
            VisibleColumnsPropertyKey.DependencyProperty;

        public ReadOnlyObservableCollection<VirtualizedDataGridColumnViewModel> VisibleColumns
        {
            get
            {
                return (ReadOnlyObservableCollection<VirtualizedDataGridColumnViewModel>)
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
        ///   Gets the visibility.
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
                typeof(VirtualizedDataGridColumnViewModel),
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
        protected VirtualizedDataGridColumnViewModel ExpanderHeaderColumn
        {
            get { return (VirtualizedDataGridColumnViewModel)GetValue(ExpanderHeaderColumnProperty); }
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

        protected int FindColumnIndex(VirtualizedDataGridColumnViewModel column)
        {
            return columns.IndexOf(column);

            for (int i = 0; i < columns.Count; ++i) {
                if (columns[i] == column)
                    return i;
            }
            return -1;
        }

        public void TryMoveColumn(
            VirtualizedDataGridColumnViewModel srcColumn,
            VirtualizedDataGridColumnViewModel dstColumn)
        {
            int oldIndex = FindColumnIndex(srcColumn);
            int newIndex = FindColumnIndex(dstColumn);
            if (oldIndex != newIndex)
                WritableColumns.Move(oldIndex, newIndex);
            RefreshAllObservableCollections();
        }
    }
}