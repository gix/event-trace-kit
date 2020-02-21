namespace EventTraceKit.VsExtension.Controls.Primitives
{
    using System;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;

    public class ColumnChooser : ContextMenu
    {
        private readonly ReadOnlyObservableCollection<AsyncDataGridColumn> columns;
        private readonly AsyncDataGridColumn[] sortedColumns;

        static ColumnChooser()
        {
            Type forType = typeof(ColumnChooser);
            DefaultStyleKeyProperty.OverrideMetadata(
                forType, new FrameworkPropertyMetadata(typeof(ContextMenu)));
        }

        public ColumnChooser(
            ReadOnlyObservableCollection<AsyncDataGridColumn> columns,
            AsyncDataViewModel viewModel)
        {
            if (columns == null)
                throw new ArgumentNullException(nameof(columns));

            this.columns = columns;

            ViewModel = viewModel;
            sortedColumns = new AsyncDataGridColumn[columns.Count];
            this.columns.CopyTo(sortedColumns, 0);
            Array.Sort(sortedColumns, CompareColumnNames);

            for (int i = 0; i < sortedColumns.Length; ++i)
                Items.Insert(i, CreateContainer(sortedColumns[i]));

            ((INotifyCollectionChanged)this.columns).CollectionChanged += ColumnsCollectionChangedHandler;

            CommandBindings.Add(
                new CommandBinding(ApplicationCommands.Close, CloseCommandExecuted));
        }

        private void ColumnsCollectionChangedHandler(object sender, NotifyCollectionChangedEventArgs e)
        {
        }

        protected override void OnClosed(RoutedEventArgs e)
        {
            base.OnClosed(e);
            ((INotifyCollectionChanged)columns).CollectionChanged -= ColumnsCollectionChangedHandler;
        }

        public AsyncDataViewModel ViewModel { get; }

        private void CloseCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private MenuItem CreateContainer(AsyncDataGridColumn column)
        {
            var item = new MenuItem {
                IsCheckable = true,
                StaysOpenOnClick = true,
                DataContext = column
            };
            item.SetBinding(MenuItem.IsCheckedProperty, new Binding(nameof(column.IsVisible)) {
                Mode = BindingMode.TwoWay
            });
            item.SetBinding(HeaderedItemsControl.HeaderProperty, new Binding(nameof(column.ColumnName)) {
                Mode = BindingMode.OneWay
            });
            return item;
        }

        private static int CompareColumnNames(
            AsyncDataGridColumn lhs, AsyncDataGridColumn rhs)
        {
            var comparisonType = StringComparison.CurrentCultureIgnoreCase;

            int cmp = string.Compare(lhs.ColumnName, rhs.ColumnName, comparisonType);
            if (cmp != 0)
                return cmp;

            return lhs.ModelVisibleColumnIndex.CompareTo(rhs.ModelVisibleColumnIndex);
        }
    }
}
