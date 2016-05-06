namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Windows;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Media;
    using Microsoft.VisualStudio.PlatformUI;

    public static class FrameworkExtensions
    {
        public static T FindVisualParent<T>(this UIElement element) where T : UIElement
        {
            for (UIElement it = element; it != null; it = VisualTreeHelper.GetParent(it) as UIElement) {
                var result = it as T;
                if (result != null)
                    return result;
            }

            return default(T);
        }

        public static T FindParent<T>(this FrameworkElement element)
            where T : FrameworkElement
        {
            for (var it = element.TemplatedParent as FrameworkElement;
                 it != null; it = it.TemplatedParent as FrameworkElement) {
                T result = it as T;
                if (result != null)
                    return result;
            }

            return default(T);
        }

        public static T FindAncestor<T>(this DependencyObject child)
            where T : DependencyObject
        {
            return child.FindAncestor<T>(x => true);
        }

        public static T FindAncestor<T>(
            this DependencyObject child, Predicate<T> predicate)
            where T : DependencyObject
        {
            if (child == null)
                throw new ArgumentNullException(nameof(child));

            for (var d = child.GetVisualOrLogicalParent(); d != null;
                 d = d.GetVisualOrLogicalParent()) {
                T result = d as T;
                if (result != null && predicate(result))
                    return result;
            }

            return default(T);
        }

        public static Rect BoundsRelativeTo(
            this FrameworkElement element, Visual relativeTo)
        {
            return element
                .TransformToVisual(relativeTo)
                .TransformBounds(LayoutInformation.GetLayoutSlot(element));
        }

        public static void SetBinding(
            this FrameworkElement d, DependencyProperty dp, object source, string path)
        {
            var binding = new Binding(path) { Source = source };
            d.SetBinding(dp, binding);
        }
    }
}
