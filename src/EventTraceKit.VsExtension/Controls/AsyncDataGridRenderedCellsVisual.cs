namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Media;
    using EventTraceKit.VsExtension.Controls.Primitives;
    using EventTraceKit.VsExtension.Windows;

    public class AsyncDataGridRenderedCellsVisual : DrawingVisual
    {
        private AsyncDataGrid parentGrid;
        private readonly AsyncDataGridCellsPresenter cellsPresenter;

        public AsyncDataGridRenderedCellsVisual(
            AsyncDataGridCellsPresenter cellsPresenter)
        {
            this.cellsPresenter = cellsPresenter;
            VisualTextHintingMode = TextHintingMode.Fixed;
        }

        private AsyncDataGrid ParentGrid =>
            parentGrid ?? (parentGrid = cellsPresenter.FindAncestor<AsyncDataGrid>());

        public Rect RenderedViewport { get; private set; }

        internal void Update(Rect viewport, Size extentSize, bool forceUpdate)
        {
            VerifyAccess();

            Rect rect = Rect.Intersect(viewport, new Rect(extentSize));
            if (!forceUpdate && RenderedViewport.Contains(rect)) {
                Offset = RenderedViewport.Location - rect.Location;
                //cellsPresenter.SetOffsetForAllUIElements(Offset);
                return;
            }

            RenderedViewport = viewport;
            var viewModel = cellsPresenter.ViewModel;
            if (viewModel == null)
                return;

            double horizontalOffset = cellsPresenter.HorizontalOffset;
            var visibleColumns = cellsPresenter.VisibleColumns;
            int firstVisibleRow = cellsPresenter.FirstVisibleRowIndex;
            int lastVisibleRow = cellsPresenter.LastVisibleRowIndex;

            double[] columnBoundaries = ComputeColumnBoundaries(visibleColumns);

            int firstVisibleColumn = -1;
            int lastVisibleColumn = firstVisibleColumn - 1;
            for (int i = 0; i < visibleColumns.Count; ++i) {
                if (columnBoundaries[i + 1] >= horizontalOffset) {
                    if (firstVisibleColumn == -1)
                        firstVisibleColumn = i;
                    lastVisibleColumn = i;
                    double rightEdge = columnBoundaries[i + 1] - horizontalOffset;
                    if (rightEdge > viewport.Width)
                        break;
                }
            }

            if (!EnsureReadyForViewport(
                viewModel, firstVisibleColumn, lastVisibleColumn, firstVisibleRow,
                lastVisibleRow))
                return;

            Offset = new Vector();
            RenderedViewport = Rect.Empty;

            using (DrawingContext dc = RenderOpen()) {
                int rowCount = viewModel.RowCount;
                if (rowCount <= 0 || visibleColumns.Count <= 0)
                    return;

                double actualWidth = cellsPresenter.ActualWidth;
                double actualHeight = cellsPresenter.ActualHeight;
                double verticalOffset = cellsPresenter.VerticalOffset;
                double rowHeight = cellsPresenter.RowHeight;
                double height = Math.Min((rowCount * rowHeight) - verticalOffset, actualHeight);

                Brush frozenColumnBrush = cellsPresenter.FrozenColumnBrush;
                for (int col = firstVisibleColumn; col <= lastVisibleColumn; ++col) {
                    double leftEdge = columnBoundaries[col] - horizontalOffset;
                    double rightEdge = columnBoundaries[col + 1] - horizontalOffset;
                    double width = rightEdge - leftEdge;
                    if (visibleColumns[col].IsInFreezableArea()) {
                        dc.DrawRectangle(
                            frozenColumnBrush,
                            null, new Rect(leftEdge, 0, width, height));
                    }
                }

                Brush primaryBackground = cellsPresenter.PrimaryBackground;
                Brush secondaryBackground = cellsPresenter.SecondaryBackground;
                for (int row = firstVisibleRow; row <= lastVisibleRow; ++row) {
                    double topEdge = (row * rowHeight) - verticalOffset;
                    var background = row % 2 == 0
                        ? primaryBackground : secondaryBackground;
                    dc.DrawRectangle(
                        background, null,
                        new Rect(0, topEdge, actualWidth, rowHeight));
                }

                Brush selectionForeground = cellsPresenter.SelectionForeground;
                Brush selectionBackground = cellsPresenter.SelectionBackground;
                Pen selectionBorderPen = cellsPresenter.SelectionBorderPen;
                if (!ParentGrid.IsSelectionActive) {
                    selectionForeground = cellsPresenter.InactiveSelectionForeground;
                    selectionBackground = cellsPresenter.InactiveSelectionBackground;
                    selectionBorderPen = cellsPresenter.InactiveSelectionBorderPen;
                }

                bool hasVisibleSelection =
                    selectionForeground != null ||
                    selectionBackground != null ||
                    selectionBorderPen != null;

                if (hasVisibleSelection) {
                    var rowSelection = viewModel.RowSelection;

                    for (int row = firstVisibleRow; row <= lastVisibleRow; ++row) {
                        if (!rowSelection.Contains(row))
                            continue;

                        double topEdge = (row * rowHeight) - verticalOffset;
                        double bottomEdge = topEdge + rowHeight - 1;

                        dc.DrawRectangle(
                            selectionBackground, null,
                            new Rect(
                                new Point(0, topEdge),
                                new Point(actualWidth, bottomEdge + 1)));

                        if (!rowSelection.Contains(row - 1)) {
                            dc.DrawLineSnapped(
                                selectionBorderPen,
                                new Point(0, topEdge),
                                new Point(actualWidth, topEdge));
                        }

                        if (!rowSelection.Contains(row + 1)) {
                            dc.DrawLineSnapped(
                                selectionBorderPen,
                                new Point(0, bottomEdge),
                                new Point(actualWidth, bottomEdge));
                        }
                    }
                }

                Pen horizontalGridLinesPen = cellsPresenter.HorizontalGridLinesPen;
                if (horizontalGridLinesPen != null) {
                    for (int row = firstVisibleRow; row <= lastVisibleRow; ++row) {
                        double bottomEdge = ((row + 1) * rowHeight) - verticalOffset;
                        dc.DrawLineSnapped(
                            horizontalGridLinesPen,
                            new Point(0, bottomEdge),
                            new Point(actualWidth, bottomEdge));
                    }
                }

                if (visibleColumns.Count > 0) {
                    RenderCells(
                        dc, viewport, height, columnBoundaries,
                        firstVisibleColumn, lastVisibleColumn,
                        firstVisibleRow, lastVisibleRow);
                }

                int focusIndex = viewModel.FocusIndex;
                Pen focusBorderPen = cellsPresenter.FocusBorderPen;
                if (ParentGrid.IsSelectionActive
                    && focusBorderPen != null
                    && focusIndex >= firstVisibleRow
                    && focusIndex <= lastVisibleRow) {
                    double topEdge = (focusIndex * rowHeight) - verticalOffset;
                    double leftEdge = -horizontalOffset;
                    double rightEdge = columnBoundaries[columnBoundaries.Length - 1] - 1;
                    double bottomEdge = rowHeight - 1;
                    var bounds = new Rect(leftEdge, topEdge, rightEdge, bottomEdge);
                    dc.DrawRectangleSnapped(null, focusBorderPen, bounds);
                }
            }
        }

        private void RenderCells(
            DrawingContext context, Rect viewport, double height,
            double[] columnBoundaries,
            int firstVisibleColumn, int lastVisibleColumn,
            int firstVisibleRow, int lastVisibleRow)
        {
            double horizontalOffset = cellsPresenter.HorizontalOffset;
            double verticalOffset = cellsPresenter.VerticalOffset;
            double rowHeight = cellsPresenter.RowHeight;
            var visibleColumns = cellsPresenter.VisibleColumns;

            Typeface typeface = cellsPresenter.Typeface;
            double fontSize = cellsPresenter.FontSize;
            FlowDirection flowDirection = cellsPresenter.FlowDirection;
            CultureInfo currentCulture = CultureInfo.CurrentCulture;
            Pen verticalGridLinesPen = cellsPresenter.VerticalGridLinesPen;
            Brush separatorBrush = cellsPresenter.SeparatorBrush;
            Brush freezableAreaSeparatorBrush = cellsPresenter.FreezableAreaSeparatorBrush;
            Brush selectionForeground = cellsPresenter.SelectionForeground;
            if (!ParentGrid.IsSelectionActive)
                selectionForeground = cellsPresenter.InactiveSelectionForeground;
            var rowSelection = cellsPresenter.ViewModel.RowSelection;

            double padding = rowHeight / 10;
            double totalPadding = 2 * padding;

            for (int col = firstVisibleColumn; col <= lastVisibleColumn; ++col) {
                double leftEdge = columnBoundaries[col] - horizontalOffset;
                double rightEdge = columnBoundaries[col + 1] - horizontalOffset;

                if (verticalGridLinesPen != null) {
                    context.DrawLineSnapped(
                        verticalGridLinesPen,
                        new Point(leftEdge, 0),
                        new Point(leftEdge, height));
                }

                double cellWidth = rightEdge - leftEdge;
                var column = visibleColumns[col];

                if (column.IsSeparator) {
                    context.DrawRectangle(
                        separatorBrush, null,
                        new Rect(leftEdge, 0, cellWidth, height));
                } else if (column.IsFreezableAreaSeparator) {
                    context.DrawRectangle(
                        freezableAreaSeparatorBrush, null,
                        new Rect(leftEdge, 0, cellWidth, height));
                } else if (column.IsSafeToReadCellValuesFromUIThread) {
                    int viewportSizeHint = lastVisibleRow - firstVisibleRow + 1;
                    for (int row = firstVisibleRow; row <= lastVisibleRow; ++row) {
                        double topEdge = (row * rowHeight) - verticalOffset;
                        Brush foreground = cellsPresenter.Foreground;
                        if (rowSelection.Contains(row))
                            foreground = selectionForeground;

                        var value = column.GetCellValue(row, viewportSizeHint);
                        var formatted = new FormattedText(
                            value?.ToString() ?? string.Empty, currentCulture,
                            flowDirection, typeface, fontSize, foreground, null,
                            TextFormattingMode.Display);
                        formatted.MaxTextWidth = Math.Max(cellWidth - totalPadding, 0);
                        formatted.MaxTextHeight = rowHeight;
                        formatted.TextAlignment = column.TextAlignment;
                        formatted.Trimming = TextTrimming.CharacterEllipsis;

                        if (totalPadding < cellWidth) {
                            var offsetY = (rowHeight - formatted.Height) / 2;
                            var origin = new Point(
                                leftEdge + padding,
                                topEdge + offsetY);
                            origin = origin.Round(MidpointRounding.AwayFromZero);
                            context.DrawText(formatted, origin);
                        }
                    }
                }
            }

            double lastRightEdge = columnBoundaries[columnBoundaries.Length - 1];
            if (lastRightEdge <= viewport.Right && verticalGridLinesPen != null) {
                context.DrawLineSnapped(
                    verticalGridLinesPen,
                    new Point(lastRightEdge, 0),
                    new Point(lastRightEdge, height));
            }
        }

        private double[] ComputeColumnBoundaries(
            IList<AsyncDataGridColumn> visibleColumns)
        {
            var boundaries = new double[visibleColumns.Count + 1];
            double cumulativeWidth = 0.0;
            for (int i = 1; i < boundaries.Length; ++i) {
                cumulativeWidth += visibleColumns[i - 1].Width;
                boundaries[i] = cumulativeWidth;
            }

            return boundaries;
        }

        internal double GetColumnAutoSize(AsyncDataGridColumn column)
        {
            if (!column.IsVisible)// && !column.IsDisconnected)
                throw new InvalidOperationException();

            double width = 5.0;

            var viewModel = cellsPresenter.ViewModel;
            if (viewModel == null || viewModel.RowCount <= 0)
                return width;

            double rowHeight = cellsPresenter.RowHeight;
            int firstVisibleRow = cellsPresenter.FirstVisibleRowIndex;
            int lastVisibleRow = cellsPresenter.LastVisibleRowIndex;
            Typeface typeface = cellsPresenter.Typeface;
            double fontSize = cellsPresenter.FontSize;
            Brush foreground = cellsPresenter.Foreground;
            FlowDirection flowDirection = cellsPresenter.FlowDirection;
            CultureInfo currentCulture = CultureInfo.CurrentCulture;

            double padding = rowHeight / 10;
            double totalPadding = 2 * padding;
            int viewportSizeHint = lastVisibleRow - firstVisibleRow + 1;

            for (int row = firstVisibleRow; row <= lastVisibleRow; ++row) {
                var value = column.GetCellValue(row, viewportSizeHint);
                var formatted = new FormattedText(
                    value.ToString(), currentCulture, flowDirection, typeface,
                    fontSize, foreground, null, TextFormattingMode.Display);

                width = Math.Max(width, formatted.Width + totalPadding + 1);
            }

            return width;
        }

        private int cachedFirstVisibleColumn;
        private int cachedLastVisibleColumn;
        private int cachedFirstVisibleRow;
        private int cachedLastVisibleRow;
        private bool lastPrefetchCancelled;
        private bool isAsyncPrefetchInProgress;
        private object cachedDataValidityToken;

        private bool EnsureReadyForViewport(
            AsyncDataGridCellsPresenterViewModel dataViewModel,
            int firstVisibleColumn, int lastVisibleColumn, int firstVisibleRow, int lastVisibleRow)
        {
            VerifyAccess();
            if (isAsyncPrefetchInProgress)
                return true;

            if (cachedFirstVisibleColumn == firstVisibleColumn &&
                cachedLastVisibleColumn == lastVisibleColumn &&
                cachedFirstVisibleRow == firstVisibleRow &&
                cachedLastVisibleRow == lastVisibleRow &&
                //dataViewModel.IsValidDataValidityToken(cachedDataValidityToken) &&
                //!dataViewModel.RowSelection.IsRefreshNecessary() &&
                !lastPrefetchCancelled) {
                return true;
            }

            lastPrefetchCancelled = false;
            cachedFirstVisibleColumn = firstVisibleColumn;
            cachedLastVisibleColumn = lastVisibleColumn;
            cachedFirstVisibleRow = firstVisibleRow;
            cachedLastVisibleRow = lastVisibleRow;
            cachedDataValidityToken = dataViewModel.DataValidityToken;
            isAsyncPrefetchInProgress = true;

            Action<bool> callBackWhenFinished = delegate (bool wasCancelled) {
                lastPrefetchCancelled = wasCancelled;
                isAsyncPrefetchInProgress = false;
                if (!wasCancelled)
                    cellsPresenter.PostUpdateRendering();
            };
            Action<bool> highlightAndSelectionPrefetched = delegate (bool wasCancelled) {
                if (!wasCancelled)
                    cellsPresenter.PostUpdateRendering();
            };

            dataViewModel.PrefetchAllDataAndQueueUpdateRender(
                cellsPresenter, firstVisibleColumn, lastVisibleColumn,
                firstVisibleRow, lastVisibleRow, highlightAndSelectionPrefetched, callBackWhenFinished);
            return true; //FIXME: false
        }
    }
}
