namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Windows;
    using System.Windows.Controls;

    public class VsMenuScrollViewer : ScrollViewer
    {
        static VsMenuScrollViewer()
        {
            Type forType = typeof(VsMenuScrollViewer);
            DefaultStyleKeyProperty.OverrideMetadata(
                forType, new FrameworkPropertyMetadata(forType));
        }
    }
}
