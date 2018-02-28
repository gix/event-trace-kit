namespace EventTraceKit.VsExtension.Tests
{
    using System.Windows.Threading;
    using Controls;
    using EventTraceKit.VsExtension.Formatting;
    using EventTraceKit.VsExtension.Views.PresetManager;
    using Xunit;

    public class PresetManagerTest
    {
        [Fact]
        public void Foo()
        {
            var dataTable = new DataTable("Foo");
            var dataView = new DataView(dataTable, new DefaultFormatProviderSource());
            var templatePreset = new AsyncDataViewModelPreset();
            var presetCollection = new AdvmPresetCollection();
            var workManager = new WorkManager(Dispatcher.CurrentDispatcher);
            var adv = new AsyncDataViewModel(workManager, dataView, templatePreset, templatePreset, presetCollection);
            var viewModel = new PresetManagerViewModel(adv);
        }
    }
}
