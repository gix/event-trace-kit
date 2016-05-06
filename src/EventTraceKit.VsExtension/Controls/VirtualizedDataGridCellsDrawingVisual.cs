namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Media;
    using EventTraceKit.VsExtension.Controls.Primitives;

    public class VirtualizedDataGridCellsDrawingVisual : DrawingVisual
    {
        private readonly VirtualizedDataGridCellsPresenter cellsPresenter;
        private SolidColorBrush backgroundBrush1;
        private SolidColorBrush backgroundBrush2;

        public VirtualizedDataGridCellsDrawingVisual(
            VirtualizedDataGridCellsPresenter cellsPresenter)
        {
            this.cellsPresenter = cellsPresenter;
            VisualTextHintingMode = TextHintingMode.Fixed;
        }

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

            //if (!EnsureReadyForViewport(
            //    viewModel, visibleColumnFirst, visibleColumnLast, firstVisibleRowIndex,
            //    lastVisibleRowIndex) || !this.isHighlightDataPrefetched)
            //    return;

            //cellsPresenter.RenderCount++;
            Offset = new Vector();
            RenderedViewport = Rect.Empty;
            //cellsPresenter.RemoveAndReleaseAllUIElements();

            using (DrawingContext context = RenderOpen()) {
                int rowCount = viewModel.RowCount;
                if (rowCount <= 0)
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
                        context.DrawRectangle(
                            frozenColumnBrush,
                            null, new Rect(leftEdge, 0, width, height));
                    }
                }

                for (int row = firstVisibleRow; row <= lastVisibleRow; ++row) {
                    double y = (row * rowHeight) - verticalOffset;
                    var background = row % 2 == 0
                        ? (backgroundBrush1 ?? (backgroundBrush1 = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF))))
                        : (backgroundBrush2 ?? (backgroundBrush2 = new SolidColorBrush(Color.FromRgb(0xF7, 0xF7, 0xF7))));
                    context.DrawRectangle(
                        background, null,
                        new Rect(0, y, actualWidth, rowHeight));
                }

                Pen pen = cellsPresenter.HorizontalGridLinesPen;
                if (pen != null) {
                    for (int row = firstVisibleRow; row <= lastVisibleRow; ++row) {
                        double y = ((row + 1) * rowHeight) - verticalOffset;
                        context.DrawLineSnapped(
                            pen, new Point(0.0, y), new Point(actualWidth, y));
                    }
                }

                //        Brush selectionForeground = this.cellsPresenter.SelectionForeground;
                //        Brush selectionBackground = this.cellsPresenter.SelectionBackground;
                //        Brush pITHighlightBackground = this.cellsPresenter.PITHighlightBackground;
                //        Brush highlightBackground = this.cellsPresenter.HighlightBackground;
                //        Pen pen2 = this.cellsPresenter.SelectionBorderPenCache.GetValue();
                //        bool flag2 = ((selectionBackground != null) || (selectionForeground != null)) ||
                //                     (pen2 != null);
                //        RowHighlight pITRowHighlight = viewModel.PITRowHighlight;
                //        RowHighlight rowHighlight = viewModel.RowHighlight;
                //        CellsPresenterRowSelection rowSelection = viewModel.RowSelection;
                //        if (((pITRowHighlight != null) || (rowHighlight != null)) || flag2) {
                //            for (int m = firstVisibleRowIndex; m <= lastVisibleRowIndex; m++) {
                //                double num21 = ((m + 1) * rowHeight) - verticalOffset;
                //                float highlightForRow = 0f;
                //                Brush brush = null;
                //                if (pITRowHighlight != null) {
                //                    highlightForRow = pITRowHighlight.GetHighlightForRow(m);
                //                    if (highlightForRow != 0f) {
                //                        brush = this.GetBlendedBrush(
                //                            pITHighlightBackground, 0.25, 1.0, (double)highlightForRow);
                //                        context.DrawRectangle(
                //                            brush, null,
                //                            new Rect(
                //                                new Point(0.0, num21 - rowHeight),
                //                                new Point(actualWidth, num21)));
                //                    }
                //                }
                //                if (rowHighlight != null) {
                //                    highlightForRow = rowHighlight.GetHighlightForRow(m);
                //                    if (highlightForRow != 0f) {
                //                        brush = this.GetBlendedBrush(
                //                            highlightBackground, 0.25, 1.0, (double)highlightForRow);
                //                        context.DrawRectangle(
                //                            brush, null,
                //                            new Rect(
                //                                new Point(0.0, num21 - rowHeight),
                //                                new Point(actualWidth, num21)));
                //                    }
                //                }
                //                if (flag2 && rowSelection.Contains(m)) {
                //                    float percentSelected = rowSelection.GetPercentSelected(m);
                //                    Brush brush7 = this.GetBlendedBrush(
                //                        selectionBackground, 0.25, 1.0, (double)percentSelected);
                //                    context.DrawRectangle(
                //                        brush7, null,
                //                        new Rect(
                //                            new Point(0.0, num21 - rowHeight),
                //                            new Point(actualWidth, num21)));
                //                    if (!rowSelection.Contains(m - 1)) {
                //                        context.DrawLineSnapped(
                //                            pen2, new Point(0.0, num21 - rowHeight),
                //                            new Point(actualWidth, num21 - rowHeight));
                //                    }
                //                    if (!rowSelection.Contains(m + 1)) {
                //                        context.DrawLineSnapped(
                //                            pen2, new Point(0.0, num21), new Point(actualWidth, num21));
                //                    }
                //                }
                //            }
                //        }

                //int focusIndex = viewModel.FocusIndex;
                //if (focusIndex >= firstVisibleRowIndex && focusIndex <= lastVisibleRowIndex) {
                //    Pen pen3 = this.cellsPresenter.FocusBorderPenCache.GetValue();
                //    if (pen3 != null) {
                //        double num25 = ((focusIndex + 1) * rowHeight) - verticalOffset;
                //        context.DrawLineSnapped(
                //            pen3, new Point(0.0, num25 - rowHeight),
                //            new Point(actualWidth, num25 - rowHeight));
                //        context.DrawLineSnapped(
                //            pen3, new Point(0.0, num25), new Point(actualWidth, num25));
                //    }
                //}

                if (visibleColumns.Count > 0) {
                    RenderCells(
                        context, viewport, height, columnBoundaries,
                        firstVisibleColumn, lastVisibleColumn,
                        firstVisibleRow, lastVisibleRow);
                }
            }
        }

        private void RenderCells(
            DrawingContext context, Rect viewport, double height, double[] columnBoundaries,
            int firstVisibleColumn, int lastVisibleColumn,
            int firstVisibleRow, int lastVisibleRow)
        {
            double horizontalOffset = cellsPresenter.HorizontalOffset;
            double verticalOffset = cellsPresenter.VerticalOffset;
            var visibleColumns = cellsPresenter.VisibleColumns;
            double rowHeight = cellsPresenter.RowHeight;

            Typeface typeface = cellsPresenter.Typeface;
            double fontSize = cellsPresenter.FontSize;
            FlowDirection flowDirection = cellsPresenter.FlowDirection;
            CultureInfo currentCulture = CultureInfo.CurrentCulture;
            Pen verticalGridLinesPen = cellsPresenter.VerticalGridLinesPen;
            Brush separatorBrush = cellsPresenter.SeparatorBrush;
            Brush freezableAreaSeparatorBrush = cellsPresenter.FreezableAreaSeparatorBrush;

            double padding = rowHeight * 0.1;
            double totalPadding = 2 * padding;

            for (int n = firstVisibleColumn; n <= lastVisibleColumn; n++) {
                double leftEdge = columnBoundaries[n] - horizontalOffset;
                double rightEdge = columnBoundaries[n + 1] - horizontalOffset;

                if (verticalGridLinesPen != null) {
                    context.DrawLineSnapped(
                        verticalGridLinesPen,
                        new Point(leftEdge, 0.0),
                        new Point(leftEdge, height));
                }

                double cellWidth = rightEdge - leftEdge;
                var column = visibleColumns[n];

                if (column.IsSeparator) {
                    context.DrawRectangle(
                        separatorBrush, null,
                        new Rect(leftEdge, 0.0, cellWidth, height));
                } else if (column.IsFreezableAreaSeparator) {
                    context.DrawRectangle(
                        freezableAreaSeparatorBrush, null,
                        new Rect(leftEdge, 0.0, cellWidth, height));
                } else if (column.IsSafeToReadCellValuesFromUIThread) {
                    int viewportSizeHint = lastVisibleRow - firstVisibleRow + 1;
                    for (int row = firstVisibleRow; row <= lastVisibleRow; ++row) {
                        double topEdge = (row * rowHeight) - verticalOffset;
                        CellValue value = column.GetCellValue(row, viewportSizeHint);
                        bool xf = true;// !cellValue.Info.IsReplicatedKey || cellValue.UsesAsciiGraphics) {
                        if (xf) {
                            Brush averageForeground = GetAverageForegroundBrush(column, value);
                            //     if (rowSelection.Contains(num35) &&
                            //         (rowSelection.GetPercentSelected(num35) > 0.5))
                            //         averageForeground = selectionForeground;

                            //     int indentationPerLevel =
                            //         this.cellsPresenter.ViewModel.IndentationPerLevel;
                            //     if ((indentationPerLevel > 0) && cellValue.Info.IsKey) {
                            //         num37 += (cellValue.Info.AsKey.Indent *
                            //                   indentationPerLevel) * 0x10;
                            //     }


                            string text = value.ToString();
                            var formatted = new FormattedText(
                                text, currentCulture, flowDirection, typeface,
                                fontSize, averageForeground, null,
                                TextFormattingMode.Display);
                            formatted.MaxTextWidth = Math.Max(cellWidth - totalPadding, 0.0);
                            formatted.MaxTextHeight = rowHeight - padding;
                            formatted.TextAlignment = column.TextAlignment;
                            formatted.Trimming = TextTrimming.CharacterEllipsis;

                            if (totalPadding < cellWidth) {
                                var point = new Point(leftEdge + padding, topEdge + padding);
                                var origin = point.Round(MidpointRounding.AwayFromZero);
                                context.DrawText(formatted, origin);
                            }
                        }
                    }
                }
            }

            double lastEdge = columnBoundaries[columnBoundaries.Length - 1];
            if (lastEdge <= viewport.Right && verticalGridLinesPen != null) {
                context.DrawLineSnapped(
                    verticalGridLinesPen,
                    new Point(lastEdge, 0.0),
                    new Point(lastEdge, height));
            }
        }

        private Brush GetAverageForegroundBrush(
            VirtualizedDataGridColumnViewModel column, CellValue value)
        {
            Brush averageForeground = cellsPresenter.Foreground;

            //if (!value.Info.IsAggregated)
            //    return averageForeground;

            //switch (column.AggregationMode) {
            //    case AggregationMode.Average:
            //        averageForeground = cellsPresenter.AverageForeground;
            //        break;
            //    case AggregationMode.Sum:
            //        averageForeground = cellsPresenter.SumForeground;
            //        break;
            //    case AggregationMode.Count:
            //        averageForeground = cellsPresenter.CountForeground;
            //        break;
            //    case AggregationMode.Min:
            //        averageForeground = cellsPresenter.MinForeground;
            //        break;
            //    case AggregationMode.Max:
            //        averageForeground = cellsPresenter.MaxForeground;
            //        break;
            //    case AggregationMode.UniqueCount:
            //        averageForeground = cellsPresenter.UniqueCountForeground;
            //        break;
            //    case AggregationMode.Peak:
            //        averageForeground = cellsPresenter.PeakForeground;
            //        break;
            //}

            return averageForeground;
        }

        private double[] ComputeColumnBoundaries(
            IList<VirtualizedDataGridColumnViewModel> visibleColumns)
        {
            var boundaries = new double[visibleColumns.Count + 1];
            double cumulativeWidth = 0.0;
            for (int i = 1; i < boundaries.Length; ++i) {
                cumulativeWidth += visibleColumns[i - 1].Width;
                boundaries[i] = cumulativeWidth;
            }

            return boundaries;
        }

        internal double GetColumnAutoSize(VirtualizedDataGridColumnViewModel column)
        {
            if (!column.IsVisible)// && !column.IsDisconnected)
                throw new InvalidOperationException();

            double width = 5.0;

            var viewModel = cellsPresenter.ViewModel;
            if (viewModel == null || viewModel.RowCount <= 0)
                return width;

            int firstVisibleRowIndex = cellsPresenter.FirstVisibleRowIndex;
            int lastVisibleRowIndex = cellsPresenter.LastVisibleRowIndex;
            Typeface typeface = cellsPresenter.Typeface;
            double fontSize = cellsPresenter.FontSize;
            Brush foreground = cellsPresenter.Foreground;
            FlowDirection flowDirection = cellsPresenter.FlowDirection;
            CultureInfo currentCulture = CultureInfo.CurrentCulture;

            double num8 = 2 * (cellsPresenter.RowHeight * 0.1);
            int viewportSizeHint = lastVisibleRowIndex - firstVisibleRowIndex + 1;

            for (int i = firstVisibleRowIndex; i <= lastVisibleRowIndex; ++i) {
                string text = column.GetCellValue(i, viewportSizeHint).ToString();
                var formatted = new FormattedText(
                    text, currentCulture, flowDirection, typeface,
                    fontSize, foreground, null, TextFormattingMode.Display);

                double num11 = column.IsKey ? 16.0 : 0.0;
                width = Math.Max(width, formatted.Width + num8 + num11 + 1.0);
            }

            return width;
        }
    }
}
