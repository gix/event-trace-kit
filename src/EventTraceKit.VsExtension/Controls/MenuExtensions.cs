namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;

    public static class MenuItemHelper
    {
        public static double MaxMenuWidth => 660;
    }

    public static class ContextMenuHelper
    {
        private static readonly ConditionalWeakTable<IInputElement, object> SourceChangedHandlers =
            new ConditionalWeakTable<IInputElement, object>();

        #region public attached PopupAnimation PopupAnimation { get; set; }

        public static readonly DependencyProperty PopupAnimationProperty =
            DependencyProperty.RegisterAttached(
                "PopupAnimation",
                typeof(PopupAnimation),
                typeof(ContextMenuHelper),
                new PropertyMetadata(PopupAnimation.None, OnPopupAnimationChanged));

        public static PopupAnimation GetPopupAnimation(DependencyObject d)
        {
            if (d == null)
                throw new ArgumentNullException(nameof(d));
            return (PopupAnimation)d.GetValue(PopupAnimationProperty);
        }

        public static void SetPopupAnimation(DependencyObject d, PopupAnimation value)
        {
            if (d == null)
                throw new ArgumentNullException(nameof(d));
            d.SetValue(PopupAnimationProperty, value);
        }

        private static void OnPopupAnimationChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ContextMenu menu) {
                if (!SourceChangedHandlers.TryGetValue(menu, out var _)) {
                    PresentationSource.AddSourceChangedHandler(menu, OnSourceChanged);
                    SourceChangedHandlers.Add(menu, null);
                }
            }
        }

        private static void OnSourceChanged(object sender, SourceChangedEventArgs e)
        {
            if (e.NewSource == null)
                return;

            if (!(sender is ContextMenu menu))
                return;

            if (LogicalTreeHelper.GetParent(menu) is Popup popup) {
                var binding = new Binding {
                    Path = new PropertyPath(PopupAnimationProperty),
                    Source = menu
                };
                popup.SetBinding(Popup.PopupAnimationProperty, binding);
            }
        }

        #endregion
    }
}
