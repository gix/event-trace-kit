namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Interop;
    using System.Windows.Markup;
    using EventTraceKit.VsExtension.Native;
    using Microsoft.VisualStudio.PlatformUI;
    using IServiceProvider = System.IServiceProvider;

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
            var menu = d as ContextMenu;
            if (menu != null) {
                object value;
                if (!SourceChangedHandlers.TryGetValue(menu, out value)) {
                    PresentationSource.AddSourceChangedHandler(menu, OnSourceChanged);
                    SourceChangedHandlers.Add(menu, null);
                }
            }
        }

        private static void OnSourceChanged(object sender, SourceChangedEventArgs e)
        {
            if (e.NewSource == null)
                return;

            var menu = sender as ContextMenu;
            if (menu == null)
                return;

            var popup = LogicalTreeHelper.GetParent(menu) as Popup;
            if (popup != null) {
                var binding = new Binding {
                    Path = new PropertyPath(PopupAnimationProperty),
                    Source = menu
                };
                popup.SetBinding(Popup.PopupAnimationProperty, binding);
            }
        }

        #endregion
    }

    public class VsMenuScrollViewer : ScrollViewer
    {
        static VsMenuScrollViewer()
        {
            Type forType = typeof(VsMenuScrollViewer);
            DefaultStyleKeyProperty.OverrideMetadata(
                forType, new FrameworkPropertyMetadata(forType));
        }
    }

    public sealed class MenuPopupPositionerExtension : MarkupExtension
    {
        private const double BorderThickness = 1.0;

        public string ElementName { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var service = (IProvideValueTarget)serviceProvider.GetService(
                typeof(IProvideValueTarget));

            var border = service.TargetObject as FrameworkElement;
            if (border == null)
                return this;

            Popup popup = border.FindAncestor<Popup>();
            if (popup != null)
                popup.Opened += (sender, args) => OnOpened(border, popup);

            return new Thickness(BorderThickness);
        }

        private void OnOpened(FrameworkElement border, Popup popup)
        {
            var element = border.FindName(ElementName) as FrameworkElement;
            if (element == null)
                throw new InvalidOperationException(
                    $"Element with name \"{ElementName}\" not found.");

            var hwndSource = (HwndSource)PresentationSource.FromVisual(popup.Child);
            if (hwndSource == null)
                return;

            RECT bounds;
            NativeMethods.GetWindowRect(hwndSource.Handle, out bounds);

            Point point = element.PointToScreen(new Point());
            if (popup.Placement == PlacementMode.Left || popup.Placement == PlacementMode.Right) {
                border.Visibility = Visibility.Collapsed;
                if (popup.Placement == PlacementMode.Left && bounds.Left > point.X)
                    popup.HorizontalOffset = 2.0;
                else if (popup.Placement == PlacementMode.Right && bounds.Left < point.X)
                    popup.HorizontalOffset = -2.0;
            } else if (point.Y > bounds.Top) {
                border.Visibility = Visibility.Hidden;
            } else {
                double left = (point.X - bounds.Left) * DpiHelper.DeviceToLogicalUnitsScalingFactorX;
                double width = (bounds.Left + bounds.Width - point.X) * DpiHelper.DeviceToLogicalUnitsScalingFactorX;
                border.Margin = new Thickness(left + 1, 0, 0, 0);
                border.Width = Clamp(width, 0, element.ActualWidth - 2);
                border.Visibility = Visibility.Visible;
            }
        }

        private static double Clamp(double value, double min, double max)
        {
            return Math.Min(Math.Max(value, min), max);
        }
    }
}
