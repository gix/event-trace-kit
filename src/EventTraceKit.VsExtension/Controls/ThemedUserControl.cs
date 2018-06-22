namespace EventTraceKit.VsExtension.Controls
{
    using System.ComponentModel;
    using System.Windows.Controls;
    using EventTraceKit.VsExtension.Resources;

    public class ThemedUserControl : UserControl
    {
        public ThemedUserControl()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
                Resources.MergedDictionaries.Insert(0, ThemeResourceManager.SharedResources);
        }
    }
}
