namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using EventTraceKit.VsExtension.Windows;
    using Primitives;

    public interface IVirtualCollection
    {
        int FocusIndex { get; set; }
        int RowCount { get; }
    }

    public sealed class AsyncDataGridCellsPresenterViewModel
        : DependencyObject, IVirtualCollection
    {
        private readonly AsyncDataViewModel advModel;

        public AsyncDataGridCellsPresenterViewModel(AsyncDataViewModel advModel)
        {
            this.advModel = advModel;

            RowSelection = new AsyncDataGridRowSelection(this);
        }

        public int RowCount => advModel.DataView.RowCount;

        public AsyncDataGridRowSelection RowSelection { get; }

        #region public int FocusIndex { get; set; }

        public event EventHandler FocusIndexChanged;

        /// <summary>
        ///   Identifies the <see cref="FocusIndex"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FocusIndexProperty =
            DependencyProperty.Register(
                nameof(FocusIndex),
                typeof(int),
                typeof(AsyncDataGridCellsPresenterViewModel),
                new PropertyMetadata(
                    Boxed.Int32Zero,
                    (d, e) => ((AsyncDataGridCellsPresenterViewModel)d).OnFocusIndexChanged(e)));

        /// <summary>
        ///   Gets or sets the focux index.
        /// </summary>
        public int FocusIndex
        {
            get => (int)GetValue(FocusIndexProperty);
            set => SetValue(FocusIndexProperty, value);
        }

        private void OnFocusIndexChanged(DependencyPropertyChangedEventArgs e)
        {
            FocusIndexChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        public bool IsReady
        {
            get
            {
                VerifyAccess();
                return advModel.IsReady;
            }
        }

        public object DataValidityToken => null;

        public void RequestUpdate(bool updateFromViewModel)
        {
            VerifyAccess();
            ValidateIsReady();
            advModel.RequestUpdate(updateFromViewModel);
        }

        private void ValidateIsReady()
        {
            if (!IsReady)
                throw new InvalidOperationException();
        }

        internal void PrefetchAllDataAndQueueUpdateRender(
            AsyncDataGridCellsPresenter cellPresenter, int firstVisibleColumn,
            int lastVisibleColumn, int firstVisibleRow, int lastVisibleRow,
            Action<bool> selectionPrefetched, Action<bool> callBackWhenFinished)
        {
            advModel.VerifyIsReady();

            IList<AsyncDataGridColumn> visibleColumns = cellPresenter.VisibleColumns;
            for (int i = firstVisibleColumn; i <= lastVisibleColumn; ++i) {
                AsyncDataGridColumn column = visibleColumns[i];
                column.IsSafeToReadCellValuesFromUIThread = true; // FIXME: false
            }

            advModel.PerformAsyncReadOperation(cancellationToken => {
                selectionPrefetched(cancellationToken.IsCancellationRequested);
                if (!cancellationToken.IsCancellationRequested) {
                    var list = new List<AsyncDataGridColumn>();
                    for (int m = firstVisibleColumn; m <= lastVisibleColumn; m++) {
                        AsyncDataGridColumn item = visibleColumns[m];
                        list.Add(item);
                    }

                    int viewportSizeHint = (lastVisibleRow - firstVisibleRow) + 1;
                    foreach (AsyncDataGridColumn column in list) {
                        for (int i = firstVisibleRow; i <= lastVisibleRow; ++i) {
                            if (cancellationToken.IsCancellationRequested)
                                break;
                            column.GetCellValue(i, viewportSizeHint);
                        }
                        column.IsSafeToReadCellValuesFromUIThread = true;
                        cellPresenter.QueueRender(true);
                    }
                }

                callBackWhenFinished(cancellationToken.IsCancellationRequested);
            });
        }
    }
}
