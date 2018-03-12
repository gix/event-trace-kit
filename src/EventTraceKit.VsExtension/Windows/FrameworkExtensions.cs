namespace EventTraceKit.VsExtension.Windows
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;

    public static class FrameworkExtensions
    {
        private static readonly Func<DependencyObject, DependencyObject, bool> MenuBase_IsDescendant;

        static FrameworkExtensions()
        {
            MenuBase_IsDescendant = (Func<DependencyObject, DependencyObject, bool>)
                typeof(MenuBase).GetMethod(
                    "IsDescendant",
                    BindingFlags.Static | BindingFlags.NonPublic,
                    null, new[] { typeof(DependencyObject), typeof(DependencyObject) }, null)
                    ?.CreateDelegate(typeof(Func<DependencyObject, DependencyObject, bool>)) ??
                throw new InvalidOperationException($"{typeof(MenuBase).Name}.IsDescendant not found");
        }

        public static T GetRootVisual<T>(this DependencyObject d) where T : Visual
        {
            while (true) {
                if (d is Visual visual) {
                    var source = PresentationSource.FromVisual(visual);
                    return source?.RootVisual as T;
                }

                if (d is FrameworkContentElement element) {
                    d = element.Parent;
                    continue;
                }

                return null;
            }
        }

        public static DependencyObject GetVisualOrLogicalParent(this DependencyObject sourceElement)
        {
            if (sourceElement == null)
                return null;
            if (sourceElement is Visual)
                return VisualTreeHelper.GetParent(sourceElement) ??
                       LogicalTreeHelper.GetParent(sourceElement);
            return LogicalTreeHelper.GetParent(sourceElement);
        }

        public static T FindVisualChild<T>(this Visual root) where T : Visual
        {
            return root.FindVisualChild<T>(v => v != null);
        }

        public static T FindVisualChild<T>(this Visual root, Predicate<T> predicate)
            where T : Visual
        {
            return root.FindVisualChildren(predicate).FirstOrDefault();
        }

        public static IEnumerable<T> FindVisualChildren<T>(
            this Visual root, Predicate<T> predicate) where T : Visual
        {
            return from v in root.EnumerateVisualTree().OfType<T>()
                   where predicate(v)
                   select v;
        }

        public static T FindVisualParent<T>(this Visual element) where T : Visual
        {
            for (Visual it = element; it != null; it = VisualTreeHelper.GetParent(it) as Visual) {
                if (it is T result)
                    return result;
            }

            return default;
        }

        public static T FindParent<T>(this FrameworkElement element)
            where T : FrameworkElement
        {
            for (var it = element.TemplatedParent as FrameworkElement;
                 it != null; it = it.TemplatedParent as FrameworkElement) {
                if (it is T result)
                    return result;
            }

            return null;
        }

        public static TAncestor FindAncestor<TAncestor>(this DependencyObject obj)
            where TAncestor : DependencyObject
        {
            return obj.FindAncestor<TAncestor>(x => true);
        }

        public static TAncestor FindAncestor<TAncestor>(
            this DependencyObject obj, Func<TAncestor, bool> predicate)
            where TAncestor : DependencyObject
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            for (obj = obj.GetVisualOrLogicalParent(); obj != null;
                 obj = obj.GetVisualOrLogicalParent()) {
                if (obj is TAncestor ancestor && predicate(ancestor))
                    return ancestor;
            }

            return null;
        }

        public static TAncestor FindAncestorOrSelf<TAncestor>(
            this DependencyObject obj) where TAncestor : DependencyObject
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            if (obj is TAncestor ancestor)
                return ancestor;

            return obj.FindAncestor<TAncestor>();
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
            where T : DependencyObject
        {
            if (obj == null)
                return default;

            for (int idx = 0; idx < VisualTreeHelper.GetChildrenCount(obj); ++idx) {
                DependencyObject child = VisualTreeHelper.GetChild(obj, idx);
                if (child == null)
                    continue;

                var descendant = child as T ?? child.FindDescendant<T>();
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
            where T : DependencyObject
        {
            return obj as T ?? obj.FindDescendant<T>();
        }

        public static IEnumerable<Visual> EnumerateVisualTree(this Visual root)
        {
            if (root == null)
                yield break;

            var queue = new Queue<DependencyObject>();
            queue.Enqueue(root);

            while (queue.Any()) {
                var item = queue.Dequeue();
                if (item is Visual visual)
                    yield return visual;

                int count = VisualTreeHelper.GetChildrenCount(item);
                for (int idx = 0; idx < count; ++idx)
                    queue.Enqueue(VisualTreeHelper.GetChild(item, idx));
            }
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

        public static bool IsDescendant(DependencyObject reference, DependencyObject node)
        {
            return MenuBase_IsDescendant(reference, node);
        }
    }
}
