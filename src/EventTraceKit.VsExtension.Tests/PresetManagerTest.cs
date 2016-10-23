﻿namespace EventTraceKit.VsExtension.Tests
{
    using Controls;
    using Formatting;
    using Xunit;

    public class PresetManagerTest
    {
        [Fact]
        public void Foo()
        {
            var dataTable = new DataTable("Foo");
            var dataView = new DataView(dataTable, new DefaultFormatProviderSource());
            var templatePreset = new AsyncDataViewModelPreset();
            var presetCollection = new AdvViewModelPresetCollection();
            var adv = new AsyncDataViewModel(dataView, templatePreset, templatePreset, presetCollection);
            var viewModel = new PresetManagerViewModel(adv);
        }
    }
}