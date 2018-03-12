namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Windows;

    public static class ThemeResourceManager
    {
        private static readonly Lazy<ResourceDictionary> resources;

        static ThemeResourceManager()
        {
            resources = new Lazy<ResourceDictionary>(() => new ResourceDictionary {
                Source = new Uri("pack://application:,,,/EventTraceKit.VsExtension;component/Themes/generic.xaml")
            });
        }

        public static ResourceDictionary Resources => resources.Value;
    }
}
