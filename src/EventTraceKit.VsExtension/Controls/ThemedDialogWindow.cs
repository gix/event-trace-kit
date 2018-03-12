namespace EventTraceKit.VsExtension.Controls
{
    using System.Windows;
    using Microsoft.VisualStudio.PlatformUI;

    public class ThemedDialogWindow : DialogWindow
    {
        public ThemedDialogWindow()
        {
            Resources.MergedDictionaries.Insert(0, ThemeResourceManager.Resources);
            Style = (Style)Resources[typeof(ThemedDialogWindow)];
        }
    }
}
