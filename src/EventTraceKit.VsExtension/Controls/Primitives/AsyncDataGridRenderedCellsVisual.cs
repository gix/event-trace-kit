namespace EventTraceKit.VsExtension.Controls.Primitives
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Media;
    using Extensions;
    using Windows;

    public class AsyncDataGridRenderedCellsVisual : DrawingVisual
    {
        private readonly AsyncDataGridCellsPresenter cellsPresenter;
        private readonly List<RenderedRow> renderedRowCache = new List<RenderedRow>();

        private AsyncDataGrid parentGrid;

        private bool rowCacheInvalid;
        private bool frozenRowCacheInvalid;
        private bool prevHasFrozenColumns;
        private double prevRenderedWidth;
        private RectangleGeometry nonFrozenAreaClip;
        private DrawingVisual focusVisual;

        private struct RenderedRow
        {
            public RenderedRow(
                int rowIndex, int styleHash, DrawingVisual visual,
                DrawingVisual frozenVisual)
            {
                RowIndex = rowIndex;
                StyleHash = styleHash;
                Visual = visual;
                FrozenVisual = frozenVisual;
            }

            public int RowIndex { get; }
            public DrawingVisual Visual { get; }
            public DrawingVisual FrozenVisual { get; set; }
            public int StyleHash { get; }
        }

        public AsyncDataGridRenderedCellsVisual(
            AsyncDataGridCellsPresenter cellsPresenter)
        {
            this.cellsPresenter = cellsPresenter;
            VisualTextHintingMode = TextHintingMode.Fixed;
        }

        internal void InvalidateRowCache()
        {
            rowCacheInvalid = true;
        }

        private AsyncDataGrid ParentGrid =>
            parentGrid ?? (parentGrid = cellsPresenter.FindAncestor<AsyncDataGrid>());

        private bool IsSelectionActive => ParentGrid?.IsSelectionActive ?? false;

        public Rect RenderedViewport { get; private set; }

        internal void Update(Rect viewport, Size extentSize, bool forceUpdate)
        {
            VerifyAccess();

            Rect rect = Rect.Intersect(viewport, new Rect(extentSize));
            if (!forceUpdate && RenderedViewport.Contains(rect)) {
                Offset = RenderedViewport.Location - rect.Location;
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

            double[] columnEdges = ComputeColumnEdges(visibleColumns);

            // All columns are visible because we cache the whole row.
            int firstVisibleColumn = 0;
            int lastVisibleColumn = visibleColumns.Count - 1;

            int firstNonFrozenColumn = firstVisibleColumn;
            int lastNonFrozenColumn = lastVisibleColumn;
            for (int i = 0; i < visibleColumns.Count; ++i) {
                if (!visibleColumns[i].IsFrozen) {
                    firstNonFrozenColumn = i;
                    break;
                }
            }
            for (int i = visibleColumns.Count - 1; i >= 0; --i) {
                if (!visibleColumns[i].IsFrozen) {
                    lastNonFrozenColumn = i;
                    break;
                }
            }

            Offset = new Vector();
            RenderedViewport = Rect.Empty;
            Children.Clear();

            using (DrawingContext dc = RenderOpen()) {
                int rowCount = viewModel.RowCount;
                if (rowCount <= 0 || visibleColumns.Count <= 0)
                    return;

                double canvasWidth = cellsPresenter.ActualWidth;
                double canvasHeight = cellsPresenter.ActualHeight;
                double verticalOffset = cellsPresenter.VerticalOffset;
                double rowHeight = cellsPresenter.RowHeight;
                double columnHeight = Math.Min((rowCount * rowHeight) - verticalOffset, canvasHeight);

                Brush primaryBackground = cellsPresenter.PrimaryBackground;
                Brush secondaryBackground = cellsPresenter.SecondaryBackground;
                for (int row = firstVisibleRow; row <= lastVisibleRow; ++row) {
                    double topEdge = (row * rowHeight) - verticalOffset;
                    var background = row % 2 == 0
                        ? primaryBackground : secondaryBackground;
                    dc.DrawRectangle(
                        background, null,
                        new Rect(0, topEdge, canvasWidth, rowHeight));
                }

                Brush frozenColumnBackground = cellsPresenter.FrozenColumnBackground;
                if (firstNonFrozenColumn > firstVisibleColumn) {
                    double leftEdge = columnEdges[firstVisibleColumn];
                    double rightEdge = columnEdges[firstNonFrozenColumn];
                    double width = rightEdge - leftEdge;
                    dc.DrawRectangle(
                        frozenColumnBackground,
                        null, new Rect(leftEdge, 0, width, columnHeight));
                }

                if (lastNonFrozenColumn < lastVisibleColumn) {
                    double width = columnEdges[lastVisibleColumn + 1] - columnEdges[lastNonFrozenColumn + 1];
                    double leftEdge = canvasWidth - width;
                    dc.DrawRectangle(
                        frozenColumnBackground,
                        null, new Rect(leftEdge, 0, width, columnHeight));
                }

                Brush selectionForeground = cellsPresenter.SelectionForeground;
                Brush selectionBackground = cellsPresenter.SelectionBackground;
                Pen selectionBorderPen = cellsPresenter.SelectionBorderPen;
                if (!IsSelectionActive) {
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
                                new Point(canvasWidth, bottomEdge + 1)));

                        if (!rowSelection.Contains(row - 1)) {
                            dc.DrawLineSnapped(
                                selectionBorderPen,
                                new Point(0, topEdge),
                                new Point(canvasWidth, topEdge));
                        }

                        if (!rowSelection.Contains(row + 1)) {
                            dc.DrawLineSnapped(
                                selectionBorderPen,
                                new Point(0, bottomEdge),
                                new Point(canvasWidth, bottomEdge));
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
                            new Point(canvasWidth, bottomEdge));
                    }
                }

                bool hasLeftFrozenColumns = firstNonFrozenColumn > firstVisibleColumn;
                bool hasRightFrozenColumns = lastNonFrozenColumn < lastVisibleColumn;
                bool hasFrozenColumns = hasLeftFrozenColumns ||
                                        hasRightFrozenColumns;

                if (rowCacheInvalid ||
                    (hasFrozenColumns && prevRenderedWidth != canvasWidth) ||
                    hasFrozenColumns != prevHasFrozenColumns) {
                    frozenRowCacheInvalid = true;
                    focusVisual = null;
                    nonFrozenAreaClip = null;
                }

                prevHasFrozenColumns = hasFrozenColumns;
                prevRenderedWidth = canvasWidth;

                PreTrimRowCache(firstVisibleRow, lastVisibleRow);

                if (visibleColumns.Count > 0) {
                    RenderCells(
                        dc, viewport, canvasWidth, columnHeight, columnEdges,
                        firstVisibleColumn, lastVisibleColumn,
                        firstVisibleRow, lastVisibleRow,
                        firstNonFrozenColumn, lastNonFrozenColumn);
                }

                PostTrimRowCache(firstVisibleRow, lastVisibleRow);

                frozenRowCacheInvalid = false;

                int focusIndex = viewModel.FocusIndex;
                Pen focusBorderPen = cellsPresenter.FocusBorderPen;
                if (IsSelectionActive
                    && focusBorderPen != null
                    && focusIndex >= firstVisibleRow
                    && focusIndex <= lastVisibleRow) {
                    if (focusVisual == null) {
                        double width;
                        if (hasRightFrozenColumns) {
                            width = canvasWidth;
                            if (!hasLeftFrozenColumns)
                                width += horizontalOffset;
                        } else {
                            width = columnEdges[columnEdges.Length - 1];
                            if (hasLeftFrozenColumns)
                                width -= horizontalOffset;
                        }

                        var bounds = new Rect(0, 0, width - 1, rowHeight - 1);

                        focusVisual = new DrawingVisual();
                        focusVisual.Transform = new TranslateTransform();
                        var context = focusVisual.RenderOpen();
                        context.DrawRectangleSnapped(null, focusBorderPen, bounds);
                        context.Close();
                    }

                    double x = hasLeftFrozenColumns ? 0 : -horizontalOffset;
                    double y = (focusIndex * rowHeight) - verticalOffset;
                    AddAtOffset(focusVisual, x, y);
                }
            }
        }

        private void AddAtOffset(ContainerVisual visual, double x, double y)
        {
            Children.Add(visual);
            var transform = (TranslateTransform)visual.Transform;
            transform.X = x;
            transform.Y = y;
        }

        private void RenderCells(
            DrawingContext context, Rect viewport, double canvasWidth,
            double columnHeight, double[] columnEdges,
            int firstVisibleColumn, int lastVisibleColumn,
            int firstVisibleRow, int lastVisibleRow,
            int firstNonFrozenColumn, int lastNonFrozenColumn)
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
            Brush keySeparatorBrush = cellsPresenter.KeySeparatorBrush;
            Brush freezableAreaSeparatorBrush = cellsPresenter.FreezableAreaSeparatorBrush;
            Brush selectionForeground = cellsPresenter.SelectionForeground;
            if (!IsSelectionActive)
                selectionForeground = cellsPresenter.InactiveSelectionForeground;
            var rowSelection = cellsPresenter.ViewModel.RowSelection;

            double padding = rowHeight / 10;
            double totalPadding = 2 * padding;

            for (int col = firstVisibleColumn; col <= lastVisibleColumn; ++col) {
                double leftEdge = columnEdges[col] - horizontalOffset;
                double rightEdge = columnEdges[col + 1] - horizontalOffset;
                double cellWidth = rightEdge - leftEdge;

                if (verticalGridLinesPen != null) {
                    context.DrawLineSnapped(
                        verticalGridLinesPen,
                        new Point(leftEdge, 0),
                        new Point(leftEdge, columnHeight));
                }

                var column = visibleColumns[col];
                if (column.IsKeySeparator) {
                    context.DrawRectangle(
                        keySeparatorBrush, null,
                        new Rect(leftEdge, 0, cellWidth, columnHeight));
                } else if (column.IsFreezableAreaSeparator) {
                    context.DrawRectangle(
                        freezableAreaSeparatorBrush, null,
                        new Rect(leftEdge, 0, cellWidth, columnHeight));
                }
            }

            double lastRightEdge = columnEdges[columnEdges.Length - 1];
            if (lastRightEdge <= viewport.Right && verticalGridLinesPen != null) {
                context.DrawLineSnapped(
                    verticalGridLinesPen,
                    new Point(lastRightEdge, 0),
                    new Point(lastRightEdge, columnHeight));
            }

            bool hasFrozenColumns = firstNonFrozenColumn > firstVisibleColumn ||
                                    lastNonFrozenColumn < lastVisibleColumn;

            int viewportSizeHint = lastVisibleRow - firstVisibleRow + 1;
            for (int row = firstVisibleRow; row <= lastVisibleRow; ++row) {
                double rowX = -horizontalOffset;
                double rowY = (row * rowHeight) - verticalOffset;

                Brush foreground = cellsPresenter.Foreground;
                if (rowSelection.Contains(row))
                    foreground = selectionForeground;

                int styleHash = ComputeRowStyleHash(rowHeight, flowDirection, typeface, fontSize, foreground);

                if (!TryGetCachedRow(row, styleHash, out var rowVisual)) {
                    var rowContext = rowVisual.RenderOpen();

                    for (int col = firstNonFrozenColumn; col <= lastNonFrozenColumn; ++col) {
                        var column = visibleColumns[col];
                        if (column.IsKeySeparator || column.IsFreezableAreaSeparator)
                            continue;

                        double topEdge = 0;
                        double leftEdge = columnEdges[col];
                        double rightEdge = columnEdges[col + 1];
                        double cellWidth = rightEdge - leftEdge;

                        var value = column.GetCellValue(row, viewportSizeHint);
                        if (value != null) {
                            var formatted = new FormattedText(
                                value.ToString(), currentCulture,
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
                                rowContext.DrawText(formatted, origin);
                            }
                        }
                    }

                    rowContext.Close();
                }

                AddAtOffset(rowVisual, rowX, rowY);

                if (hasFrozenColumns) {
                    if (nonFrozenAreaClip == null) {
                        double nonFrozenLeftEdge = columnEdges[firstNonFrozenColumn];
                        double nonFrozenRightEdge = canvasWidth - (columnEdges[lastVisibleColumn + 1] - columnEdges[lastNonFrozenColumn + 1]);

                        var nonFrozenArea = new Rect(
                            nonFrozenLeftEdge, 0,
                            Math.Max(nonFrozenRightEdge - nonFrozenLeftEdge, 0), rowHeight);

                        nonFrozenAreaClip = new RectangleGeometry(nonFrozenArea) {
                            Transform = new TranslateTransform(horizontalOffset, 0)
                        };
                    }

                    rowVisual.Clip = nonFrozenAreaClip;
                    ((TranslateTransform)nonFrozenAreaClip.Transform).X = horizontalOffset;

                    if (!TryGetCachedFrozenRow(row, styleHash, out var frozenRowVisual)) {
                        var rowContext = frozenRowVisual.RenderOpen();

                        for (int col = firstVisibleColumn; col <= lastVisibleColumn; ++col) {
                            var column = visibleColumns[col];
                            if (column.IsKeySeparator || column.IsFreezableAreaSeparator)
                                continue;
                            if (col >= firstNonFrozenColumn && col <= lastNonFrozenColumn)
                                continue;

                            double topEdge = 0;
                            double leftEdge = columnEdges[col];
                            double rightEdge = columnEdges[col + 1];
                            double cellWidth = rightEdge - leftEdge;

                            if (col > lastNonFrozenColumn) {
                                double distance = columnEdges[lastVisibleColumn + 1] - columnEdges[col];
                                leftEdge = canvasWidth - distance;
                                rightEdge = leftEdge + cellWidth;
                            }

                            var value = column.GetCellValue(row, viewportSizeHint);
                            if (value != null) {
                                var formatted = new FormattedText(
                                    value.ToString(), currentCulture,
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
                                    rowContext.DrawText(formatted, origin);
                                }
                            }
                        }

                        rowContext.Close();
                    }

                    AddAtOffset(frozenRowVisual, 0, rowY);
                }
            }
        }

        private void PreTrimRowCache(int firstVisibleRow, int lastVisibleRow)
        {
            if (renderedRowCache.Count > 0) {
                bool hasAnyRowCached =
                    renderedRowCache[0].RowIndex <= lastVisibleRow &&
                    renderedRowCache[renderedRowCache.Count - 1].RowIndex >= firstVisibleRow;
                if (!hasAnyRowCached)
                    rowCacheInvalid = true;
            }

            if (rowCacheInvalid) {
                renderedRowCache.Clear();
                rowCacheInvalid = false;
            }
        }

        private void PostTrimRowCache(int firstVisibleRow, int lastVisibleRow)
        {
            int visibleRows = lastVisibleRow - firstVisibleRow + 1;
            int overhang = Math.Min(2, visibleRows / 3);

            int firstCachedRow = firstVisibleRow - overhang;
            int lastCachedRow = lastVisibleRow + overhang;

            int begin = 0;
            int end = renderedRowCache.Count - 1;

            int validBegin = begin;
            for (int i = begin; i < renderedRowCache.Count; ++i) {
                if (renderedRowCache[i].RowIndex >= firstCachedRow) {
                    validBegin = i;
                    break;
                }
            }

            int validEnd = end;
            for (int i = end; i >= 0; --i) {
                if (renderedRowCache[i].RowIndex <= lastCachedRow) {
                    validEnd = i;
                    break;
                }
            }

            int count = end - validEnd;
            if (count != 0)
                renderedRowCache.RemoveRange(validEnd, count);

            count = validBegin - begin;
            if (count != 0)
                renderedRowCache.RemoveRange(begin, count);
        }

        private static int ComputeRowStyleHash(
            double rowHeight,
            FlowDirection flowDirection,
            Typeface typeface,
            double fontSize,
            Brush foreground)
        {
            const int primeFactor = 397;
            unchecked {
                int hash = rowHeight.GetHashCode();
                hash = (hash * primeFactor) ^ flowDirection.GetHashCode();
                hash = (hash * primeFactor) ^ (typeface?.GetHashCode() ?? 0);
                hash = (hash * primeFactor) ^ fontSize.GetHashCode();
                hash = (hash * primeFactor) ^ (foreground?.GetHashCode() ?? 0);
                return hash;
            }
        }

        private bool TryGetCachedRow(
            int row, int styleHash, out DrawingVisual rowVisual)
        {
            int idx = renderedRowCache.BinarySearch(row, (x, y) => x.RowIndex.CompareTo(y));
            if (idx >= 0) {
                rowVisual = renderedRowCache[idx].Visual;
                if (renderedRowCache[idx].StyleHash == styleHash)
                    return true;

                renderedRowCache[idx] = new RenderedRow(row, styleHash, rowVisual, null);
                return false;
            }

            rowVisual = new DrawingVisual();
            rowVisual.Transform = new TranslateTransform();
            renderedRowCache.Insert(~idx, new RenderedRow(row, styleHash, rowVisual, null));
            return false;
        }

        private bool TryGetCachedFrozenRow(int row, int styleHash, out DrawingVisual rowVisual)
        {
            int idx = renderedRowCache.BinarySearch(row, (x, y) => x.RowIndex.CompareTo(y));
            Debug.Assert(idx >= 0);

            var entry = renderedRowCache[idx];
            if (!frozenRowCacheInvalid && entry.StyleHash == styleHash && entry.FrozenVisual != null) {
                rowVisual = entry.FrozenVisual;
                return true;
            }

            if (frozenRowCacheInvalid || entry.FrozenVisual == null) {
                rowVisual = new DrawingVisual();
                rowVisual.Transform = new TranslateTransform();
                entry.FrozenVisual = rowVisual;
                renderedRowCache[idx] = entry;
            } else {
                rowVisual = entry.FrozenVisual;
            }

            return false;
        }

        private double[] ComputeColumnEdges(IList<AsyncDataGridColumn> visibleColumns)
        {
            var edges = new double[visibleColumns.Count + 1];

            double cumulativeWidth = 0;
            for (int i = 1; i < edges.Length; ++i) {
                cumulativeWidth += visibleColumns[i - 1].Width;
                edges[i] = cumulativeWidth;
            }

            return edges;
        }

        internal double GetColumnAutoSize(AsyncDataGridColumn column)
        {
            if (!column.IsVisible && !column.IsDisconnected)
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
    }
}
