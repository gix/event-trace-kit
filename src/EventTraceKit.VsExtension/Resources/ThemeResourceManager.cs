namespace EventTraceKit.VsExtension.Resources
{
    using System;
    using System.Windows;

    public static class ThemeResourceManager
    {
        private static readonly Lazy<ResourceDictionary> sharedResources;

        static ThemeResourceManager()
        {
            sharedResources = new Lazy<ResourceDictionary>(() => new ResourceDictionary {
                Source = new Uri("pack://application:,,,/EventTraceKit.VsExtension;component/Resources/SharedResources.xaml")
            });
        }

        public static ResourceDictionary SharedResources => sharedResources.Value;
    }
}
