namespace EventTraceKit.VsExtension.Controls
{
    using System.Windows;
    using EventTraceKit.VsExtension.Resources;
    using Microsoft.VisualStudio.PlatformUI;

    public class ThemedDialogWindow : DialogWindow
    {
        public ThemedDialogWindow()
        {
            Resources.MergedDictionaries.Insert(0, ThemeResourceManager.SharedResources);
            Style = (Style)Resources[typeof(ThemedDialogWindow)];
        }
    }
}
