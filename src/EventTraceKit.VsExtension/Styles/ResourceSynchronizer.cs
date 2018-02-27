namespace EventTraceKit.VsExtension.Styles
{
    using System;
    using System.Collections.ObjectModel;
    using System.Windows;
    using Microsoft.VisualStudio.PlatformUI;
    using Microsoft.VisualStudio.Shell.Interop;

    public class ResourceSynchronizer
    {
        private readonly IVsFontAndColorStorage fncStorage;
        private readonly Collection<ResourceDictionary> resourceContainer;

        public ResourceSynchronizer(
            IVsFontAndColorStorage fncStorage,
            Collection<ResourceDictionary> resourceContainer)
        {
            if (resourceContainer == null)
                throw new ArgumentNullException(nameof(resourceContainer));
            this.resourceContainer = resourceContainer;
            this.fncStorage = fncStorage;
            UpdateValues();

            EnvironmentRenderCapabilities.Current.RenderCapabilitiesChanged += (s, e) => UpdateValues();
        }

        public ResourceDictionary Resources { get; set; }

        public void UpdateValues()
        {
            ResourceDictionary newResources = new FontAndColorsResourceDictionary(fncStorage);
            AddColorsAndBrushes(newResources);
            AddFonts(newResources);
            int index = resourceContainer.IndexOf(Resources);
            if (index < 0)
                resourceContainer.Add(newResources);
            else
                resourceContainer[index] = newResources;

            Resources = newResources;
        }

        private void AddColorsAndBrushes(ResourceDictionary newResources)
        {
            //AddSolidColorKeys(newResources);
        }

        private void AddSolidColorKeys(ResourceDictionary newResources)
        {
            AddKey(newResources, TraceLogColors.RowForegroundBrushKey);
            AddKey(newResources, TraceLogColors.RowBackgroundBrushKey);
            AddKey(newResources, TraceLogColors.AlternatingRowBackgroundBrushKey);
            AddKey(newResources, TraceLogColors.SelectedRowForegroundBrushKey);
            AddKey(newResources, TraceLogColors.SelectedRowBackgroundBrushKey);
            AddKey(newResources, TraceLogColors.InactiveSelectedRowForegroundBrushKey);
            AddKey(newResources, TraceLogColors.InactiveSelectedRowBackgroundBrushKey);
            AddKey(newResources, TraceLogColors.FrozenColumnBackgroundBrushKey);
        }

        private static void AddKey(ResourceDictionary resource, object key)
        {
            resource.Add(key, key);
        }

        private void AddFonts(ResourceDictionary newResources)
        {
            AddKey(newResources, TraceLogFonts.RowFontFamilyKey);
            AddKey(newResources, TraceLogFonts.RowFontSizeKey);
        }
    }
}
