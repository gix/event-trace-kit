namespace EventTraceKit.VsExtension.Controls
{
    using System.Linq;
    using System.Windows;
    using System.Windows.Media;

    public static class VisualStateExtensions
    {
        public static FrameworkElement GetImplementationRoot(this DependencyObject d)
        {
            if (VisualTreeHelper.GetChildrenCount(d) == 1)
                return VisualTreeHelper.GetChild(d, 0) as FrameworkElement;
            return null;
        }

        public static VisualStateGroup TryGetVisualStateGroup(this DependencyObject d, string groupName)
        {
            var root = GetImplementationRoot(d);
            if (root == null)
                return null;
            return VisualStateManager.GetVisualStateGroups(root)
                ?.OfType<VisualStateGroup>()
                .FirstOrDefault(x => string.CompareOrdinal(groupName, x.Name) == 0);
        }
    }
}
