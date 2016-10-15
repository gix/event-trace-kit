namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Windows;
    using System.Windows.Input;

    public class DragEventSource
    {
        private readonly MouseButtonEventHandler mouseDownHandler;
        private readonly MouseEventHandler mouseMoveHandler;
        private readonly MouseButtonEventHandler mouseUpHandler;
        private readonly MouseEventHandler lostMouseCaptureHandler;

        private readonly Vector minimumDragDistance = new Vector(
            SystemParameters.MinimumHorizontalDragDistance,
            SystemParameters.MinimumVerticalDragDistance);

        private DragState state;
        private Point startPosition;

        public DragEventSource()
        {
            mouseDownHandler = OnMouseDown;
            mouseMoveHandler = OnMouseMove;
            mouseUpHandler = OnMouseUp;
            lostMouseCaptureHandler = OnLostMouseCapture;
        }

        public bool IsActive => state != DragState.None;
        public bool IsDragging => state == DragState.Dragging;
        public bool IsPreparing => state == DragState.Preparing;

        public event Action<object> DragPrepare;
        public event Action DragStart;
        public event Action<bool> DragFinish;

        public void Attach(UIElement element)
        {
            element.AddHandler(UIElement.MouseLeftButtonDownEvent, mouseDownHandler, true);
            element.AddHandler(UIElement.MouseMoveEvent, mouseMoveHandler, true);
            element.AddHandler(UIElement.MouseLeftButtonUpEvent, mouseUpHandler, true);
            element.AddHandler(UIElement.LostMouseCaptureEvent, lostMouseCaptureHandler, true);
        }

        public void Detach(UIElement element)
        {
            element.RemoveHandler(UIElement.MouseLeftButtonDownEvent, mouseDownHandler);
            element.RemoveHandler(UIElement.MouseMoveEvent, mouseMoveHandler);
            element.RemoveHandler(UIElement.MouseLeftButtonUpEvent, mouseUpHandler);
            element.RemoveHandler(UIElement.LostMouseCaptureEvent, lostMouseCaptureHandler);
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var element = sender as UIElement;
            if (element == null)
                return;

            // Because our mouse move event is attached to the draggable object
            // and not the surrounding elements (there might not even be any if
            // its at a window edge!) we need to capture the mouse to continue
            // receiving mouse move events. Otherwise attempting to drag small
            // elements or at the edges of an element will fail.
            //   Unconditionally capturing the mouse here might interfere with
            // any contained elements that themselves capture the mouse (e.g.,
            // it prevents toggling a checkbox in a draggable list view column).
            // Ignoring handled mouse down events is not possible either because
            // list view item flags them as handled for selection purposes.
            if (element.IsMouseCaptureWithin || element.CaptureMouse()) {
                Point currentPosition = e.GetPosition(element);
                PrepareDrag(currentPosition, sender);
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            var element = sender as UIElement;
            if (element == null || e.LeftButton != MouseButtonState.Pressed)
                return;

            if (IsPreparing) {
                Point currentPosition = e.GetPosition(element);
                if (ShouldStartDrag(currentPosition))
                    StartDrag();
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            var element = sender as UIElement;
            if (element == null)
                return;

            if (IsActive) {
                if (element.IsMouseCaptured)
                    element.ReleaseMouseCapture();
                FinishDrag(canceled: false);
            }
        }

        private void OnLostMouseCapture(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && IsDragging)
                FinishDrag(canceled: true);
        }

        private void PrepareDrag(Point position, object source)
        {
            startPosition = position;
            state = DragState.Preparing;
            DragPrepare?.Invoke(source);
        }

        private void StartDrag()
        {
            state = DragState.Dragging;
            DragStart?.Invoke();
        }

        private void FinishDrag(bool canceled)
        {
            state = DragState.None;
            DragFinish?.Invoke(canceled);
        }

        private bool ShouldStartDrag(Point currentPosition)
        {
            var delta = VectorUtils.Abs(currentPosition - startPosition);
            return delta.X.GreaterThan(minimumDragDistance.X) ||
                   delta.Y.GreaterThan(minimumDragDistance.Y);
        }

        private enum DragState
        {
            None,
            Preparing,
            Dragging,
        }
    }
}
