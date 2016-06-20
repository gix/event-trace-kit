namespace EventTraceKit.VsExtension.Windows
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

            return null;
        }

        public static T FindAncestor<T>(this DependencyObject obj)
            where T : DependencyObject
        {
            return obj.FindAncestor<T>(x => true);
        }

        public static T FindAncestor<T>(
            this DependencyObject obj, Func<T, bool> predicate)
            where T : DependencyObject
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            for (obj = obj.GetVisualOrLogicalParent(); obj != null;
                 obj = obj.GetVisualOrLogicalParent()) {
                T ancestor = obj as T;
                if (ancestor != null && predicate(ancestor))
                    return ancestor;
            }

            return null;
        }

        /// <summary>
        ///   Finds the first visual descendant of the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///   The type of descendant to find.
        /// </typeparam>
        /// <param name="obj">The object at which to begin searching.</param>
        /// <returns>
        ///   The first visual descendant that satisfies the predicate, or
        ///   <see langword="null"/> if no descendant is found.
        /// </returns>
        public static T FindDescendant<T>(this DependencyObject obj)
            where T : class
        {
            if (obj == null)
                return default(T);

            for (int idx = 0; idx < VisualTreeHelper.GetChildrenCount(obj); ++idx) {
                DependencyObject child = VisualTreeHelper.GetChild(obj, idx);
                if (child == null)
                    continue;

                var descendant = child as T ?? child?.FindDescendant<T>();
                if (descendant != null)
                    return descendant;
            }

            return null;
        }

        /// <summary>
        ///   Finds the first visual descendant (or self) of the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///   The type of descendant to find.
        /// </typeparam>
        /// <param name="obj">The object at which to begin searching.</param>
        /// <returns>
        ///   The object itself, if of correct type. Otherwise the first visual
        ///   descendant that satisfies the predicate, or <see langword="null"/>
        ///   if no descendant is found.
        /// </returns>
        public static T FindDescendantOrSelf<T>(this DependencyObject obj)
            where T : class
        {
            return obj as T ?? obj.FindDescendant<T>();
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
