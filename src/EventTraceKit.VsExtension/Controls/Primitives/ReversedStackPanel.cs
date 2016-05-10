namespace EventTraceKit.VsExtension.Controls.Primitives
{
    using System.Windows.Controls;
    using System.Windows.Media;

    public class ReversedStackPanel : StackPanel
    {
        protected override Visual GetVisualChild(int index)
        {
            int last = VisualChildrenCount - 1;
            return base.GetVisualChild(last - index);
        }
    }
}
