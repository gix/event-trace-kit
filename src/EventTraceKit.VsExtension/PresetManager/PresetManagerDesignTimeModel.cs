namespace EventTraceKit.VsExtension
{
    using System;
    using System.Windows.Threading;
    using Windows;
    using Controls;
    using Formatting;

    public class PresetManagerDesignTimeModel : PresetManagerViewModel
    {
        public PresetManagerDesignTimeModel()
            : base(CreateModel())
        {
        }

        private static AsyncDataViewModel CreateModel()
        {
            var idPreset = new ColumnViewModelPreset {
                Id = new Guid("A27E5F00-BCA0-4BFE-B43D-EAA4B3F20D42"),
                Name = "Id",
                IsVisible = true,
                Width = 80
            }.EnsureFrozen();
            var namePreset = new ColumnViewModelPreset {
                Id = new Guid("3050F05D-FDCC-43AC-AA63-72CF17E5B7FF"),
                Name = "Name",
                IsVisible = true,
                Width = 200
            }.EnsureFrozen();

            var template = new AsyncDataViewModelPreset();
            var table = new DataTable("Design");

            AddColumn(table, template, idPreset, DataColumn.Create(x => x));
            AddColumn(table, template, namePreset, DataColumn.Create(x => "Name" + x));

            var dataView = new DataView(table, new DefaultFormatProviderSource());
            return new AsyncDataViewModel(
                new WorkManager(Dispatcher.CurrentDispatcher),
                dataView, template, template, new AdvmPresetCollection());
        }

        private static void AddColumn(
            DataTable table, AsyncDataViewModelPreset preset,
            ColumnViewModelPreset columnPreset, DataColumn column)
        {
            column.Id = columnPreset.Id;
            column.Name = columnPreset.Name;
            column.Width = columnPreset.Width;
            column.IsVisible = columnPreset.IsVisible;
            column.IsResizable = true;
            column.TextAlignment = columnPreset.TextAlignment;
            preset.ConfigurableColumns.Add(columnPreset);
            table.Add(column);
        }
    }
}
