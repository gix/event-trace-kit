namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Shapes;
    using Microsoft.Expression.Interactivity.Layout;

    public class ReorderStackPanel : StackPanel
    {
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            return base.ArrangeOverride(arrangeSize);

            bool isHorizontal = Orientation == Orientation.Horizontal;
            Rect finalRect = new Rect(arrangeSize);

            if (ScrollOwner != null) {
                //if (isHorizontal) {
                //    finalRect.X = ComputePhysicalFromLogicalOffset(scrollData.ComputedOffset.X, true);
                //    finalRect.Y = -1.0 * scrollData.ComputedOffset.Y;
                //} else {
                //    finalRect.X = -1.0 * scrollData.ComputedOffset.X;
                //    finalRect.Y = ComputePhysicalFromLogicalOffset(scrollData.ComputedOffset.Y, false);
                //}
            }

            double lastLength = 0.0;
            for (int index = 0; index < Children.Count; ++index) {
                UIElement element = Children[index];
                if (element == null)
                    continue;

                if (isHorizontal) {
                    finalRect.X += lastLength;
                    finalRect.Width = element.DesiredSize.Width;
                    finalRect.Height = Math.Max(arrangeSize.Height, element.DesiredSize.Height);
                    lastLength = finalRect.Width;
                } else {
                    finalRect.Y += lastLength;
                    finalRect.Width = Math.Max(arrangeSize.Width, element.DesiredSize.Width);
                    finalRect.Height = element.DesiredSize.Height;
                    lastLength = finalRect.Height;
                }

                element.Arrange(finalRect);
                continue;

                if (index == 1) {
                    element.RenderTransform = new TranslateTransform(-10000.0, -10000.0);
                } else if (index == 2) {
                    var size = element.RenderSize;

                    var canvas = new Canvas {
                        Width = size.Width,
                        Height = size.Height,
                        IsHitTestVisible = false
                    };
                    var rectangle = new Rectangle {
                        Width = size.Width,
                        Height = size.Height,
                        IsHitTestVisible = false
                    };
                    rectangle.Fill = new VisualBrush(element);
                    canvas.Children.Add(rectangle);

                    var adornerContainer = new AdornerContainer(element) {
                        Child = canvas
                    };

                    AdornerLayer.GetAdornerLayer(this).Add(adornerContainer);

                    element.RenderTransform = new TranslateTransform(-10000.0, -10000.0);
                    canvas.RenderTransform = new TranslateTransform(10000.0, 10000.0);

                    var animation = new DoubleAnimation {
                        Duration = TimeSpan.FromMilliseconds(500),
                        From = 0,
                        To = -Children[index - 1].RenderSize.Width
                    };
                    animation.EasingFunction = new CubicEase {
                        EasingMode = EasingMode.EaseInOut
                    };

                    Storyboard.SetTarget(animation, rectangle);
                    Storyboard.SetTargetProperty(
                        animation,
                        new PropertyPath("(Canvas.Left)"));

                    var storyboard = new Storyboard();
                    storyboard.AutoReverse = true;
                    storyboard.RepeatBehavior = RepeatBehavior.Forever;
                    storyboard.Children.Add(animation);
                    storyboard.Begin();
                }
            }

            Ghost();
            return arrangeSize;
        }

        private bool ghosted;

        private Canvas adornerCanvas;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (adornerCanvas == null)
                return;

            Point pt = e.GetPosition(this);
            var c1 = adornerCanvas.Children[1];
            Canvas.SetLeft(c1, pt.X);
        }

        private void Ghost()
        {
            if (ghosted)
                return;

            //ghosted = true;
            if (adornerCanvas == null) {
                var canvas = new Canvas {
                    Width = ActualWidth,
                    Height = ActualHeight,
                    IsHitTestVisible = false
                };

                canvas.SetBinding(WidthProperty, this, nameof(ActualWidth));
                canvas.SetBinding(HeightProperty, this, nameof(ActualHeight));

                var adornerContainer = new AdornerContainer(this) {
                    Child = canvas
                };

                var adornerLayer = AdornerLayer.GetAdornerLayer(this);
                adornerLayer.Add(adornerContainer);

                adornerCanvas = canvas;
            }

            adornerCanvas.Children.Clear();

            for (int index = 0; index < Children.Count; ++index) {
                UIElement element = Children[index];
                if (element == null)
                    continue;

                //element.RenderTransform = new TranslateTransform(-10000.0, -10000.0);
                var size = element.RenderSize;

                var rectangle = new Rectangle {
                    Width = size.Width,
                    Height = size.Height,
                    IsHitTestVisible = false,
                    Fill = new VisualBrush(element)
                };

                Rect bounds = LayoutInformation.GetLayoutSlot((FrameworkElement)element); //((FrameworkElement)element).BoundsRelativeTo(this);
                Canvas.SetLeft(rectangle, bounds.Left);
                Canvas.SetTop(rectangle, bounds.Top);
                adornerCanvas.Children.Add(rectangle);

                continue;

                //element.RenderTransform = new TranslateTransform(-10000.0, -10000.0);
                //canvas.RenderTransform = new TranslateTransform(10000.0, 10000.0);

                var animation = new DoubleAnimation {
                    Duration = TimeSpan.FromMilliseconds(500),
                    From = 0,
                    To = -Children[index - 1].RenderSize.Width
                };
                animation.EasingFunction = new CubicEase {
                    EasingMode = EasingMode.EaseInOut
                };

                Storyboard.SetTarget(animation, rectangle);
                Storyboard.SetTargetProperty(
                    animation,
                    new PropertyPath("(Canvas.Left)"));

                var storyboard = new Storyboard();
                storyboard.AutoReverse = true;
                storyboard.RepeatBehavior = RepeatBehavior.Forever;
                storyboard.Children.Add(animation);
                //storyboard.Begin();
            }

            this.Opacity = 0.25;
        }

        public bool IsAnimating { get; set; }

        public TimeSpan Duration
        {
            get { return (TimeSpan)GetValue(DurationProperty); }
            set { SetValue(DurationProperty, value); }
        }

        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register(
                nameof(Duration),
                typeof(TimeSpan),
                typeof(ReorderStackPanel),
                new UIPropertyMetadata(TimeSpan.FromMilliseconds(500)));

        protected override Size MeasureOverride(Size constraint)
        {
            return base.MeasureOverride(constraint);
        }

        protected override UIElementCollection CreateUIElementCollection(FrameworkElement logicalParent)
        {
            return base.CreateUIElementCollection(logicalParent);
        }

        private double ComputePhysicalFromLogicalOffset(
            double logicalOffset, bool horizontal)
        {
            double offset = 0.0;
            UIElementCollection children = Children;
            for (int index = 0; index < logicalOffset; ++index)
                offset -= horizontal ? children[index].DesiredSize.Width : children[index].DesiredSize.Height;
            return offset;
        }
    }
}
