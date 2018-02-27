namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.VisualStudio.Imaging;
    using Microsoft.VisualStudio.Threading;
    using Threading;

    public class AsyncDataGridViewModel : DependencyObject
    {
        private readonly AsyncDataViewModel advModel;

        public AsyncDataGridViewModel(AsyncDataViewModel advModel)
        {
            this.advModel = advModel;
            ColumnsModel = new AsyncDataGridColumnsViewModel(advModel);
            CellsPresenter = new AsyncDataGridCellsPresenterViewModel(advModel);
        }

        public AsyncDataGridColumnsViewModel ColumnsModel { get; }

        public AsyncDataGridCellsPresenterViewModel CellsPresenter { get; }

        public AsyncDataGridRowSelection RowSelection => CellsPresenter.RowSelection;

        public int FocusIndex => CellsPresenter.FocusIndex;

        public event ItemEventHandler<bool> Updated;

        public event EventHandler DataInvalidated
        {
            add => advModel.DataInvalidated += value;
            remove => advModel.DataInvalidated -= value;
        }

        internal void RaiseUpdated(bool refreshViewModelFromModel = true)
        {
            Updated?.Invoke(this, new ItemEventArgs<bool>(refreshViewModelFromModel));
        }

        private class CopyContextMenu : MenuItemCommand
        {
            private readonly AsyncDataGridViewModel advModel;
            private readonly CopyBehavior behavior;

            public CopyContextMenu(AsyncDataGridViewModel advModel, CopyBehavior behavior)
            {
                this.advModel = advModel;
                this.behavior = behavior;
                switch (behavior) {
                    case CopyBehavior.Cell:
                        Header = "Copy Cell";
                        InputGestureText = "Ctrl+Shift+C";
                        return;

                    case CopyBehavior.Selection:
                        Header = "Copy Selection";
                        InputGestureText = "Ctrl+C";
                        return;
                }
            }

            protected override void ExecuteCore(object parameter)
            {
                advModel.CopyToClipboard(behavior);
            }
        }

        public ContextMenu BuildContextMenu()
        {
            var menu = new ContextMenu();
            menu.Items.Add(new CopyContextMenu(this, CopyBehavior.Selection) {
                Icon = new CrispImage { Moniker = KnownMonikers.Copy }
            });

            return menu;
        }

        public void CopyToClipboard(CopyBehavior copyBehavior)
        {
            switch (copyBehavior) {
                case CopyBehavior.Cell:
                    CopyCell();
                    break;

                case CopyBehavior.Selection:
                    CopySelection().Forget();
                    break;

                default:
                    throw new InvalidOperationException();
            }
        }

        private async Task CopySelection()
        {
            var advModel = ColumnsModel.Model;
            var visibleColumns = ColumnsModel.VisibleColumns;
            var rowSelection = RowSelection.GetSnapshot();

            if (!visibleColumns.Any() || rowSelection.Count == 0)
                return;

            var columns = visibleColumns
                .Where(x => !x.IsSeparator)
                .Select(x => new KeyValuePair<string, int>(x.ColumnName, x.ModelVisibleColumnIndex))
                .ToList();

            var text = await advModel.WorkManager.BackgroundTaskFactory.RunWithProgress(
                "Copying…",
                Application.Current.MainWindow,
                (cancel, progress) => {
                    var buffer = new StringBuilder();
                    foreach (var column in columns)
                        buffer.AppendFormat("{0}, ", column.Key);

                    buffer.Length -= 2;
                    buffer.AppendLine();

                    int total = rowSelection.Count;

                    var options = new ParallelOptions();
                    options.CancellationToken = cancel;

                    var formattedRows = new string[rowSelection.Count];
                    Parallel.ForEach(rowSelection, options,
                        (row, loopState, idx) => {
                            var buf = new StringBuilder();
                            foreach (var column in columns) {
                                var value = advModel.GetCellValue(row, column.Value);
                                buf.AppendFormat("{0}, ", value);
                            }
                            buf.Length -= 2;
                            buf.AppendLine();
                            formattedRows[idx] = buf.ToString();
                            progress.Report(new ProgressState(1, total));
                        });

                    foreach (var formattedRow in formattedRows)
                        buffer.Append(formattedRow);

                    return buffer.ToString();
                });

            ClipboardUtils.SetText(text);
        }

        private void CopyCell()
        {
            //var clickedColumn = this.ColumnsViewModel.ClickedColumn;
            //if (clickedColumn != null) {
            //    int focusIndex = this.FocusIndex;
            //    this.ColumnsViewModel.HdvViewModel.WorkManager.BackgroundThread.Send(delegate {
            //        CellValue cellValue = clickedColumn.GetCellValueNotCached(focusIndex);
            //        clipboardText = CellValueToClipboardString(cellValue, true, false);
            //        clipboardHTML = CellValueToClipboardHtmlString(cellValue);
            //    });
            //}
        }
    }

    public enum CopyBehavior
    {
        Selection,
        Cell,
        ColumnSelection
    }
}
