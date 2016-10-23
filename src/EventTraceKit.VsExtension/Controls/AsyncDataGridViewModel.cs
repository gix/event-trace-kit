namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using Microsoft.VisualStudio.Imaging;

    public class AsyncDataGridViewModel : DependencyObject
    {
        public AsyncDataGridViewModel(
            AsyncDataViewModel advModel, AsyncDataGridColumnsViewModel columnsViewModel)
        {
            CellsPresenterViewModel = new AsyncDataGridCellsPresenterViewModel(advModel);
            ColumnsViewModel = columnsViewModel;
        }

        public AsyncDataGridCellsPresenterViewModel CellsPresenterViewModel { get; }

        public AsyncDataGridColumnsViewModel ColumnsViewModel { get; }

        public AsyncDataGridRowSelection RowSelection =>
            CellsPresenterViewModel.RowSelection;

        public int FocusIndex => CellsPresenterViewModel.FocusIndex;

        public event ItemEventHandler<bool> Updated;

        internal void RaiseUpdated(bool refreshViewModelFromModel = true)
        {
            Updated?.Invoke(this, new ItemEventArgs<bool>(refreshViewModelFromModel));
        }

        private class CopyCellContextMenu : MenuItem, ICommand
        {
            private readonly AsyncDataGridViewModel advModel;
            private readonly CopyBehavior behavior;

            public CopyCellContextMenu(AsyncDataGridViewModel advModel, CopyBehavior behavior)
            {
                Command = this;

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

            bool ICommand.CanExecute(object parameter)
            {
                return true;
            }

            void ICommand.Execute(object parameter)
            {
                advModel.CopyToClipboard(behavior);
            }

            private event EventHandler CanExecuteChanged;

            event EventHandler ICommand.CanExecuteChanged
            {
                add { CanExecuteChanged += value; }
                remove { CanExecuteChanged -= value; }
            }
        }

        public ContextMenu BuildContextMenu()
        {
            var menu = new ContextMenu();
            menu.Items.Add(new CopyCellContextMenu(this, CopyBehavior.Selection) {
                Icon = new CrispImage {
                    Moniker = KnownMonikers.Copy
                }
            });
            //menu.Items.Add(new CopyCellContextMenu(this, CopyBehavior.Cell));

            return menu;
        }

        public void CopyToClipboard(CopyBehavior copyBehavior)
        {
            switch (copyBehavior) {
                case CopyBehavior.Cell:
                    CopyCell();
                    break;

                case CopyBehavior.Selection:
                    CopySelection();
                    break;

                default:
                    throw new InvalidOperationException();
            }
        }

        private bool CopySelection()
        {
            var advModel = ColumnsViewModel.AdvModel;
            var visibleColumns = ColumnsViewModel.VisibleColumns;
            var rowSelection = RowSelection.GetSnapshot();

            if (!visibleColumns.Any() || rowSelection.Count == 0)
                return true;

            var columns = visibleColumns
                .Where(x => !x.IsSeparator)
                .Select(x => new KeyValuePair<string, int>(x.ColumnName, x.ModelVisibleColumnIndex))
                .ToList();

            var text = advModel.WorkManager.BackgroundThread.Send(() => {
                var buffer = new StringBuilder();
                foreach (var column in columns)
                    buffer.AppendFormat("{0}, ", column.Key);

                buffer.Length -= 2;
                buffer.AppendLine();

                foreach (var row in rowSelection) {
                    foreach (var column in columns) {
                        string cell = advModel.GetCellValue(row, column.Value).ToString();
                        buffer.AppendFormat("{0}, ", cell);
                    }

                    buffer.Length -= 2;
                    buffer.AppendLine();
                }

                return buffer.ToString();
            });

            return ClipboardUtils.SetText(text);
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

    public static class ClipboardUtils
    {
        public static bool SetText(string text)
        {
            var data = new DataObject();
            if (text != null)
                data.SetData(DataFormats.Text, text);

            return TrySetDataObject(data);
        }

        public static bool TrySetDataObject(object data)
        {
            try {
                Clipboard.SetDataObject(data);
                return true;
            } catch (ExternalException) {
                return false;
            }
        }
    }

    public enum CopyBehavior
    {
        Selection,
        Cell,
        ColumnSelection
    }
}

