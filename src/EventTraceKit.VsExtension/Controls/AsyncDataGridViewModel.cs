namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.VisualStudio.Imaging;
    using Microsoft.VisualStudio.Threading;
    using Threading;
    using Task = System.Threading.Tasks.Task;

    public class AsyncDataGridViewModel : DependencyObject
    {
        private readonly AsyncDataViewModel advModel;
        private CancellationTokenSource copySelectionCts;

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

        public static readonly DependencyProperty AutoScrollProperty =
            DependencyProperty.Register(
                nameof(AutoScroll),
                typeof(bool),
                typeof(AsyncDataGridViewModel),
                new FrameworkPropertyMetadata(
                    false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public bool AutoScroll
        {
            get => (bool)GetValue(AutoScrollProperty);
            set => SetValue(AutoScrollProperty, value);
        }

        public event EventHandler Updated;

        public event EventHandler DataInvalidated
        {
            add => advModel.DataInvalidated += value;
            remove => advModel.DataInvalidated -= value;
        }

        internal void RaiseUpdated()
        {
            Updated?.Invoke(this, EventArgs.Empty);
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

        public void BuildContextMenu(ContextMenu menu, int? rowIndex, int? columnIndex)
        {
            menu.Items.Add(new CopyContextMenu(this, CopyBehavior.Selection) {
                Icon = new CrispImage { Moniker = KnownMonikers.Copy }
            });

            var workflow = advModel.DataView.GetInteractionWorkflow<IContextMenuWorkflow>(rowIndex, columnIndex);
            if (workflow != null) {
                bool hasSeparator = false;
                foreach (var item in workflow.GetItems()) {
                    if (!hasSeparator) {
                        menu.Items.Add(new Separator());
                        hasSeparator = true;
                    }

                    menu.Items.Add(item);
                }
            }
        }

        public void CopyToClipboard(CopyBehavior copyBehavior)
        {
            switch (copyBehavior) {
                case CopyBehavior.Selection:
                    CopySelectionAsync().Forget();
                    break;
            }
        }

        private async Task CopySelectionAsync()
        {
            var visibleColumns = ColumnsModel.VisibleColumns;
            var rowSelection = RowSelection.GetSnapshot();

            if (!visibleColumns.Any() || rowSelection.Count == 0)
                return;

            var columns = visibleColumns
                .Where(x => !x.IsSeparator)
                .Select(x => new KeyValuePair<string, int>(x.ColumnName, x.ModelVisibleColumnIndex))
                .ToList();

            copySelectionCts?.Cancel();
            copySelectionCts = new CancellationTokenSource();

            var text = await ThreadingExtensions.RunWithProgress(
                "Copying…", "Rows",
                (cancel, progress) => {
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
                            progress.Report(new ProgressDelta(1, total));
                        });

                    var buffer = new StringBuilder();
                    foreach (var column in columns)
                        buffer.AppendFormat("{0}, ", column.Key);

                    buffer.Length -= 2;
                    buffer.AppendLine();

                    foreach (var formattedRow in formattedRows)
                        buffer.Append(formattedRow);

                    return buffer.ToString();
                },
                copySelectionCts.Token);

            ClipboardUtils.SetText(text);
        }
    }

    public sealed class SingleItemEnumerablePartitioner<T> : OrderablePartitioner<IReadOnlyList<T>>
    {
        private readonly IEnumerable<T> source;
        private readonly int batchSize;

        public SingleItemEnumerablePartitioner(IEnumerable<T> source, int batchSize)
            : base(keysOrderedInEachPartition: true,
                   keysOrderedAcrossPartitions: false,
                   keysNormalized: true)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            this.batchSize = batchSize;
        }

        public override bool SupportsDynamicPartitions => true;

        public override IList<IEnumerator<KeyValuePair<long, IReadOnlyList<T>>>>
            GetOrderablePartitions(int partitionCount)
        {
            if (partitionCount < 1)
                throw new ArgumentOutOfRangeException(nameof(partitionCount));

            var dynamicPartitioner = new DynamicGenerator(source.GetEnumerator(), batchSize, false);

            var partitions = new IEnumerator<KeyValuePair<long, IReadOnlyList<T>>>[partitionCount];
            for (int i = 0; i < partitionCount; ++i)
                partitions[i] = dynamicPartitioner.GetEnumerator();
            return partitions;
        }

        public override IEnumerable<KeyValuePair<long, IReadOnlyList<T>>> GetOrderableDynamicPartitions()
        {
            return new DynamicGenerator(source.GetEnumerator(), batchSize, true);
        }

        private class DynamicGenerator : IEnumerable<KeyValuePair<long, IReadOnlyList<T>>>, IDisposable
        {
            private readonly IEnumerator<T> sharedEnumerator;
            private readonly int batchSize;

            private long nextAvailablePosition;
            private int remainingPartitions;
            private bool disposed;

            public DynamicGenerator(IEnumerator<T> sharedEnumerator, int batchSize, bool requiresDisposal)
            {
                this.sharedEnumerator = sharedEnumerator;
                this.batchSize = batchSize;
                nextAvailablePosition = -1;
                remainingPartitions = requiresDisposal ? 1 : 0;
            }

            void IDisposable.Dispose()
            {
                if (!disposed && Interlocked.Decrement(ref remainingPartitions) == 0) {
                    disposed = true;
                    sharedEnumerator.Dispose();
                }
            }

            public IEnumerator<KeyValuePair<long, IReadOnlyList<T>>> GetEnumerator()
            {
                Interlocked.Increment(ref remainingPartitions);
                return GetEnumeratorCore();
            }

            IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

            private IEnumerator<KeyValuePair<long, IReadOnlyList<T>>> GetEnumeratorCore()
            {
                try {
                    while (true) {
                        T[] nextItem = new T[batchSize];
                        long position = 0;
                        lock (sharedEnumerator) {
                            for (int i = 0; i < nextItem.Length; ++i) {
                                if (sharedEnumerator.MoveNext()) {
                                    position = nextAvailablePosition++;
                                    nextItem[i] = sharedEnumerator.Current;
                                } else
                                    yield break;
                            }
                        }
                        yield return new KeyValuePair<long, IReadOnlyList<T>>(position, nextItem);
                    }
                } finally {
                    if (Interlocked.Decrement(ref remainingPartitions) == 0)
                        sharedEnumerator.Dispose();
                }
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
