namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Windows;
    using System.Windows.Documents;
    using System.Windows.Media;

    public sealed class InsertionAdorner : Adorner
    {
        private readonly AdornerLayer adornerLayer;

        public static readonly DependencyProperty PenProperty =
            DependencyProperty.Register(
                nameof(Pen),
                typeof(Pen),
                typeof(InsertionAdorner),
                new FrameworkPropertyMetadata(
                    null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty DropTargetProperty =
            DependencyProperty.Register(
                nameof(DropTarget), typeof(UIElement), typeof(InsertionAdorner),
                new FrameworkPropertyMetadata(
                    null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty InsertAfterProperty =
            DependencyProperty.Register(
                nameof(InsertAfter), typeof(bool), typeof(InsertionAdorner),
                new FrameworkPropertyMetadata(
                    Boxed.True, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsHorizontalProperty =
            DependencyProperty.Register(
                nameof(IsHorizontal), typeof(bool), typeof(InsertionAdorner),
                new FrameworkPropertyMetadata(
                    Boxed.True, FrameworkPropertyMetadataOptions.AffectsRender));

        public InsertionAdorner(
            UIElement adornedElement, bool isHorizontal, Pen pen, AdornerLayer adornerLayer)
            : base(adornedElement)
        {
            if (adornedElement == null)
                throw new ArgumentNullException(nameof(adornedElement));
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (adornerLayer == null)
                throw new ArgumentNullException(nameof(adornerLayer));

            this.adornerLayer = adornerLayer;
            IsHorizontal = isHorizontal;
            Pen = pen;

            SnapsToDevicePixels = true;
            IsHitTestVisible = false;

            adornerLayer.Add(this);
        }

        public Pen Pen
        {
            get { return (Pen)GetValue(PenProperty); }
            set { SetValue(PenProperty, value); }
        }

        public UIElement DropTarget
        {
            get { return (UIElement)GetValue(DropTargetProperty); }
            set { SetValue(DropTargetProperty, value); }
        }

        public bool InsertAfter
        {
            get { return (bool)GetValue(InsertAfterProperty); }
            set { SetValue(InsertAfterProperty, Boxed.Bool(value)); }
        }

        public bool IsHorizontal
        {
            get { return (bool)GetValue(IsHorizontalProperty); }
            set { SetValue(IsHorizontalProperty, Boxed.Bool(value)); }
        }

        public void Detach()
        {
            adornerLayer.Remove(this);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (DropTarget == null)
                return;

            double width = DropTarget.RenderSize.Width;
            double height = DropTarget.RenderSize.Height;

            var lineBegin = new Point();
            var lineEnd = new Point();
            if (IsHorizontal) {
                lineEnd.X = width;
                if (InsertAfter)
                    lineBegin.Y = lineEnd.Y = height;
            } else {
                lineEnd.Y = height;
                if (InsertAfter)
                    lineBegin.X = lineEnd.X = width;
            }

            var transform = DropTarget.TransformToAncestor(AdornedElement);
            lineBegin = transform.Transform(lineBegin);
            lineEnd = transform.Transform(lineEnd);

            drawingContext.DrawLine(Pen, lineBegin, lineEnd);
        }
    }
}
