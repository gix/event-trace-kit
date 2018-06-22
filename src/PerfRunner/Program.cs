namespace PerfRunner
{
    using System;
    using System.Windows;
    using System.Windows.Threading;
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Attributes.Jobs;
    using EventTraceKit.VsExtension;
    using EventTraceKit.VsExtension.Controls;
    using EventTraceKit.VsExtension.Controls.Primitives;
    using EventTraceKit.VsExtension.Formatting;
    using EventTraceKit.VsExtension.Windows;

    [SimpleJob]
    public class CellsVisualBenchmark
    {
        private AsyncDataViewModel advModel;
        private AsyncDataGridCellsPresenterViewModel presenterViewModel;
        private AsyncDataGridCellsPresenter presenter;
        private DataView dataView;
        private int rowCount;

        [GlobalSetup]
        public void Setup()
        {
            var dataTable = new DataTable("Stub");

            var templatePreset = new AsyncDataViewModelPreset();
            for (int i = 0; i < 8; ++i) {
                int columnId = i + 1;

                var preset = new ColumnViewModelPreset {
                    Id = new Guid($"{columnId:X8}-0000-0000-0000-000000000000"),
                    Name = $"Column{columnId}",
                    IsVisible = true,
                    Width = 200
                }.EnsureFrozen();

                var column = DataColumn.Create(x => (x << 16 | columnId));
                column.Id = preset.Id;
                column.Name = preset.Name;
                column.IsVisible = preset.IsVisible;
                column.Width = preset.Width;

                dataTable.Columns.Add(column);

                templatePreset.ConfigurableColumns.Add(preset);
            }

            dataView = new DataView(dataTable, new DefaultFormatProviderSource());

            var workManager = new WorkManager(Dispatcher.CurrentDispatcher);
            var defaultPreset = templatePreset.Clone();
            var presetCollection = new AdvmPresetCollection();

            advModel = new AsyncDataViewModel(
                workManager, dataView, templatePreset, defaultPreset,
                presetCollection);

            presenterViewModel = new AsyncDataGridCellsPresenterViewModel(advModel);

            presenter = new AsyncDataGridCellsPresenter();
            presenter.ViewModel = presenterViewModel;
            presenter.VisibleColumns = advModel.GridViewModel.ColumnsModel.VisibleColumns;
            presenter.HorizontalGridLinesThickness = 0;
            presenter.VerticalGridLinesThickness = 0;
            presenter.AutoScroll = true;
            presenter.Arrange(new Rect(0, 0, 1200, 200));

            while (!advModel.IsReady)
                Dispatcher.CurrentDispatcher.DoEvents();
        }

        [Benchmark]
        [STAThread]
        public void WithoutPrefetch()
        {
            dataView.UpdateRowCount(rowCount);
            presenter.PerformRender(true);
            ++rowCount;
        }
    }

    public static class DispatcherExtensions
    {
        public static void DoEvents(this Dispatcher dispatcher)
        {
            var frame = new DispatcherFrame();
            dispatcher.InvokeAsync(
                () => frame.Continue = false, DispatcherPriority.ContextIdle);
            Dispatcher.PushFrame(frame);
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            //var summary = BenchmarkRunner.Run<TdhFormatter>();
            //BenchmarkRunner.Run<TraceLogFilterBenchmark>();
            //BenchmarkRunner.Run<CellsVisualBenchmark>();

            //var thread = new Thread(() => {
            //    var b = new XBenchmark();
            //    b.Setup();
            //    b.Foo();
            //});
            //thread.SetApartmentState(ApartmentState.STA);
            //thread.Start();
            //thread.Join();
        }
    }
}
