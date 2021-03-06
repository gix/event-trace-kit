namespace EventTraceKit.VsExtension.Views.PresetManager
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Media;
    using EventTraceKit.VsExtension.Controls;

    public class ListBoxDragPreview
    {
        private readonly ListBox listBox;
        private InsertionAdorner insertionAdorner;
        private bool dragEntered;

        public ListBoxDragPreview(ListBox listBox)
        {
            this.listBox = listBox;
            listBox.PreviewDragEnter += OnPreviewDragEnter;
            listBox.PreviewDragOver += OnPreviewDragOver;
            listBox.PreviewDragLeave += OnPreviewDragLeave;
        }

        private AdornerLayer AdornerLayer => AdornerLayer.GetAdornerLayer(listBox);

        public void Hide()
        {
            RemoveAdorner();
        }

        public void UpdateAdorner(ListBoxItem dropTarget, Point positionInDropTarget)
        {
            if (insertionAdorner == null)
                return;

            bool insertAfter;
            if (insertionAdorner.IsHorizontal)
                insertAfter = positionInDropTarget.Y > dropTarget.ActualHeight / 2;
            else
                insertAfter = positionInDropTarget.X > dropTarget.ActualWidth / 2;

            UpdateAdorner(dropTarget, insertAfter);
        }

        private void OnPreviewDragEnter(object sender, DragEventArgs e)
        {
            dragEntered = true;
            ShowAdorner();
        }

        private void OnPreviewDragOver(object sender, DragEventArgs e)
        {
            UpdateAdorner();
        }

        private void OnPreviewDragLeave(object sender, DragEventArgs e)
        {
            dragEntered = false;
            HideAdornerAsync();
        }

        private void ShowAdorner()
        {
            if (insertionAdorner == null) {
                insertionAdorner = new InsertionAdorner(
                    listBox,
                    true,
                    new Pen(listBox.Foreground ?? Brushes.Black, 2),
                    AdornerLayer);
            }
        }

        private void HideAdornerAsync()
        {
            listBox.Dispatcher.InvokeAsync(() => {
                if (!dragEntered)
                    RemoveAdorner();
            });
        }

        private void RemoveAdorner()
        {
            if (insertionAdorner != null) {
                insertionAdorner.Detach();
                insertionAdorner = null;
            }
        }

        private void UpdateAdorner()
        {
            if (listBox.Items.Count == 0)
                return;

            var lastItem = listBox.Items[listBox.Items.Count - 1];
            var container = (UIElement)listBox.ItemContainerGenerator.ContainerFromItem(lastItem);
            UpdateAdorner(container, true);
        }

        private void UpdateAdorner(UIElement dropTarget, bool insertAfter)
        {
            if (insertionAdorner == null)
                return;

            insertionAdorner.DropTarget = dropTarget;
            insertionAdorner.InsertAfter = insertAfter;
        }
    }
}
