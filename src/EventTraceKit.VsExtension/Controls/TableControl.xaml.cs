namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;

    /// <summary>
    /// Interaction logic for TableControl.xaml
    /// </summary>
    public partial class TableControl : UserControl
    {
        private TableControlViewModel tableControlViewModel;
        private ContextMenu contextMenu = new ContextMenu();

        public TableControl()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;

            tableControlView.Loaded += OnTableControlViewLoaded;
            tableControlView.Unloaded += OnTableControlViewUnloaded;

            var view = tableControlView.View as GridView;
            if (view != null)
                view.Columns.CollectionChanged += OnGridViewColumnCollectionChanged;
        }

        private void OnTableControlViewLoaded(object sender, RoutedEventArgs e)
        {
            //tableControlViewModel.AttachEventManager(tableControlView);
            UpdateColumns(false);
            SizeChanged += OnSizeChanged;
        }

        private void OnTableControlViewUnloaded(object sender, RoutedEventArgs e)
        {
            //tableControlViewModel.DetachEventManager(tableControlView);
            SizeChanged -= OnSizeChanged;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width != e.PreviousSize.Width)
                UpdateColumns(true);
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (tableControlViewModel != null) {

            }

            tableControlView.ContextMenu = null;

            var viewModel = e.NewValue as TableControlViewModel;
            if (viewModel != null) {
                tableControlViewModel = viewModel;
                UpdateColumns(true);
                tableControlView.ContextMenu = BuildContextMenu();
            }
        }

        private ContextMenu BuildContextMenu()
        {
            var menu = new ContextMenu();

            foreach (TableHeaderViewModel model in tableControlViewModel.Headers) {
                var item = new MenuItem();
                item.Header = model.Header;
                item.IsCheckable = true;
                item.SetBinding(
                    MenuItem.IsCheckedProperty, model, nameof(model.IsVisible));
                item.Click += (sender, args) => {
                    model.IsVisible = !model.IsVisible;
                    if (model.IsVisible)
                        tableControlViewModel.AddColumn(model);
                    else
                        tableControlViewModel.RemoveColumn(model);
                };
                menu.Items.Add(item);
            }

            return menu;
        }

        private void OnGridViewColumnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (tableControlViewModel == null)
                return;

            if (e.Action == NotifyCollectionChangedAction.Move)
                MoveColumnHeader(e.NewStartingIndex);

            //tableControlViewModel.UpdateVisibleColumns();
        }

        private class EntryData
        {
            public readonly TableHeaderViewModel Header;
            public double Width;

            public EntryData(TableHeaderViewModel header, double width)
            {
                Header = header;
                Width = width;
            }
        }

        private void UpdateColumns(bool updateStarEntry = true)
        {
            double availableWidth =
                tableControlView.ActualWidth -
                SystemParameters.VerticalScrollBarWidth - 6.0;
            double totalColumnsWidth = 0.0;

            EntryData star = null;
            var columns = new List<EntryData>(tableControlViewModel.Headers.Count);
            foreach (TableHeaderViewModel model in tableControlViewModel.Headers.Where(x => x.IsVisible)) {
                double columnWidth = model.ColumnWidth;

                var column = new EntryData(model, columnWidth);
                if (availableWidth > 0.0) {
                    double minWidth = Math.Max(22.0, model.MinWidth);
                    double maxWidth = Math.Min(availableWidth, model.MaxWidth);
                    column.Width = Math.Ceiling(Math.Max(minWidth, Math.Min(maxWidth, columnWidth)));
                    totalColumnsWidth += column.Width;
                    if (model.IsStar)
                        star = column;
                }

                columns.Add(column);
            }

            if (star != null && (updateStarEntry || star.Header.ResetColumnWidth)) {
                star.Header.ResetColumnWidth = false;
                double remainingWidth = star.Width + (availableWidth - totalColumnsWidth);
                star.Width = Math.Max(remainingWidth, star.Header.MinWidth);
            }

            var view = (GridView)tableControlView.View;
            view.Columns.Clear();
            foreach (EntryData enty in columns) {
                enty.Header.ColumnWidth = enty.Width;
                view.Columns.Add(CreateGridViewColumn(enty.Header));
            }
        }

        private GridViewColumn CreateGridViewColumn(TableHeaderViewModel header)
        {
            return new GridViewColumn {
                Width = header.ColumnWidth,
                Header = header.Header,
                CellTemplate = CreateItemDataTemplate(header.MemberName)
            };
        }

        private static DataTemplate CreateItemDataTemplate(string columnName)
        {
            FrameworkElementFactory factory = new FrameworkElementFactory(typeof(ContentPresenter));
            factory.SetBinding(
                ContentPresenter.ContentProperty,
                new Binding(columnName));
            return new DataTemplate { VisualTree = factory };
        }

        private void MoveColumnHeader(int columnIndex)
        {
            var view = tableControlView.View as GridView;
            if (view == null)
                return;

            TableHeaderViewModel header = GetHeader(view.Columns[columnIndex]);
            if (header == null)
                return;

            var headers = tableControlViewModel.Headers;
            headers.Remove(header);

            if (columnIndex > 0) {
                TableHeaderViewModel prevHeader = GetHeader(view.Columns[columnIndex - 1]);
                int index;
                if (prevHeader != null && (index = headers.IndexOf(prevHeader)) > -1)
                    columnIndex = index + 1;
                else
                    columnIndex = headers.Count;
            }

            headers.Insert(columnIndex, header);
        }

        private TableHeaderViewModel GetHeader(GridViewColumn column)
        {
            var element = column.Header as FrameworkElement;
            return element?.DataContext as TableHeaderViewModel;
        }
    }
}
