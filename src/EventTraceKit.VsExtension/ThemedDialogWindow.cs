namespace EventTraceKit.VsExtension
{
    using System;
    using System.Windows;
    using Windows;
    using Microsoft.VisualStudio.PlatformUI;

    public class ThemedDialogWindow : DialogWindow
    {
        static ThemedDialogWindow()
        {
            //DefaultStyleKeyProperty.OverrideMetadata<ThemedDialogWindow>();
        }

        public ThemedDialogWindow()
        {
            Resources.MergedDictionaries.Add(new ResourceDictionary {
                Source = new Uri("pack://application:,,,/EventTraceKit.VsExtension;component/Themes/generic.xaml")
            });

            Style = (Style)Resources[typeof(ThemedDialogWindow)];
        }
    }
}
