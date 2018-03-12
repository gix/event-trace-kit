namespace EventTraceKit.VsExtension.Controls
{
    using System.Windows.Controls;
    using EventTraceKit.VsExtension.Resources;

    public class ThemedUserControl : UserControl
    {
        public ThemedUserControl()
        {
            Resources.MergedDictionaries.Insert(0, ThemeResourceManager.SharedResources);
        }
    }
}
