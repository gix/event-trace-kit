namespace EventTraceKit.VsExtension.Views.PresetManager
{
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using EventTraceKit.VsExtension.Controls;
    using EventTraceKit.VsExtension.Windows;

    public class PresetColumnDragBehavior
    {
        private readonly DragEventSource availableListDragEventSource = new DragEventSource();
        private readonly DragEventSource layoutListDragEventSource = new DragEventSource();
        private readonly ListBoxDragPreview layoutListDragPreview;

        private readonly PresetManagerViewModel viewModel;
        private readonly ItemsControl availableList;
        private readonly ListBox layoutList;
        private DependencyObject dragSource;
        private object dragData;

        public PresetColumnDragBehavior(
            UIElement associatedObject,
            PresetManagerViewModel viewModel,
            ItemsControl availableList,
            ListBox layoutList)
        {
            AssociatedObject = associatedObject;
            this.viewModel = viewModel;
            this.availableList = availableList;
            this.layoutList = layoutList;

            availableListDragEventSource.DragPrepare += OnDragPrepare;
            availableListDragEventSource.DragStart += OnAvailableListDragStart;

            layoutListDragEventSource.DragPrepare += OnDragPrepare;
            layoutListDragEventSource.DragStart += OnLayoutListDragStart;
            layoutListDragPreview = new ListBoxDragPreview(layoutList);
            layoutList.DragEnter += OnLayoutListDragEnter;
            layoutList.DragOver += OnLayoutListDragOver;

            availableList.Drop += OnAvailableListDrop;
            layoutList.Drop += OnLayoutListDrop;
        }

        public UIElement AssociatedObject { get; }

        private void OnDragPrepare(object source)
        {
            dragSource = (DependencyObject)source;
            if (dragSource is ListBoxItem item) {
                var listBox = item.GetParentListBox();
                dragData = listBox.GetOrderedSelectedItemsArray();
            }
        }

        private void OnAvailableListDragStart()
        {
            if (dragData is ColumnViewModelPreset[] columns) {
                var data = new DataObject(typeof(ColumnViewModelPreset[]), columns);
                DragDrop.DoDragDrop(dragSource, data, DragDropEffects.Copy);
            }
        }

        private void OnLayoutListDragStart()
        {
            if (dragData is PresetManagerColumnViewModel[] columns) {
                var data = new DataObject(typeof(PresetManagerColumnViewModel[]), columns);
                DragDrop.DoDragDrop(dragSource, data, DragDropEffects.Move);
            }
        }

        private void OnAvailableListDrop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            OnAvailableListDrop(e.Data);
        }

        private void OnAvailableListDrop(IDataObject data)
        {
            if (data.TryGetArray(out PresetManagerColumnViewModel[] toRemove))
                viewModel.Remove(toRemove);
        }

        private void OnLayoutListDrop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            OnLayoutListDrop(e.Data);
        }

        private void OnLayoutListDrop(IDataObject data)
        {
            var target = viewModel.PresetColumns.LastOrDefault();
            DoDrop(data, target, true);
        }

        public void OnLayoutListItemDrop(object sender, DragEventArgs e)
        {
            if (!(sender is FrameworkElement relativeTo))
                return;

            e.Handled = true;
            var target = relativeTo.DataContext as PresetManagerColumnViewModel;
            var moveAfter = e.GetPosition(relativeTo).Y >= relativeTo.ActualHeight / 2.0;
            DoDrop(e.Data, target, moveAfter);
        }

        public void OnLayoutListDragEnter(object sender, DragEventArgs e)
        {
            e.Handled = true;

            var target = viewModel.PresetColumns.LastOrDefault();
            if (!CanDrop(e.Data, target, true)) {
                e.Effects = DragDropEffects.None;
                layoutListDragPreview.Hide();
            }
        }

        public void OnLayoutListDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;

            var target = viewModel.PresetColumns.LastOrDefault();
            if (!CanDrop(e.Data, target, true)) {
                e.Effects = DragDropEffects.None;
                layoutListDragPreview.Hide();
            }
        }

        public void OnLayoutListItemDragEnter(object sender, DragEventArgs e)
        {
            if (!(sender is ListBoxItem relativeTo))
                return;

            e.Handled = true;

            var target = relativeTo.DataContext as PresetManagerColumnViewModel;
            var moveAfter = e.GetPosition(relativeTo).Y >= relativeTo.ActualHeight / 2.0;
            if (!CanDrop(e.Data, target, moveAfter)) {
                e.Effects = DragDropEffects.None;
                layoutListDragPreview.Hide();
            } else {
                Point position = e.GetPosition(relativeTo);
                layoutListDragPreview.UpdateAdorner(relativeTo, position);
            }

            ItemsControl control = layoutList;
            TryScrollIntoView(control, e.GetPosition(control));
        }

        public void OnLayoutListItemDragOver(object sender, DragEventArgs e)
        {
            if (!(sender is ListBoxItem relativeTo))
                return;

            e.Handled = true;

            var target = relativeTo.DataContext as PresetManagerColumnViewModel;
            var moveAfter = e.GetPosition(relativeTo).Y >= relativeTo.ActualHeight / 2.0;
            if (!CanDrop(e.Data, target, moveAfter)) {
                e.Effects = DragDropEffects.None;
                layoutListDragPreview.Hide();
            } else {
                Point position = e.GetPosition(relativeTo);
                layoutListDragPreview.UpdateAdorner(relativeTo, position);
            }

            ItemsControl control = layoutList;
            TryScrollIntoView(control, e.GetPosition(control));
        }

        private void TryScrollIntoView(ItemsControl control, Point point)
        {
            var viewer = control.FindVisualChild<ScrollViewer>();
            if (viewer == null)
                return;

            const double threshold = 40.0;

            double wheelScrollLines = SystemParameters.WheelScrollLines;
            double y = point.Y;
            double height = control.ActualHeight;
            if (y < threshold) {
                var deltaY = wheelScrollLines + (threshold - y);
                viewer.ScrollToVerticalOffset(viewer.VerticalOffset - deltaY);
            } else if (y > height - threshold) {
                var deltaY = wheelScrollLines + (y - (height - threshold));
                viewer.ScrollToVerticalOffset(viewer.VerticalOffset + deltaY);
            }
        }

        private bool CanDrop(IDataObject data, PresetManagerColumnViewModel target, bool moveAfter)
        {
            if (data.TryGetArray(out ColumnViewModelPreset[] toAdd))
                return true;

            if (data.TryGetArray(out PresetManagerColumnViewModel[] toMove))
                return viewModel.CanMove(toMove, target, moveAfter);

            return false;
        }

        private void DoDrop(IDataObject data, PresetManagerColumnViewModel target, bool moveAfter)
        {
            layoutListDragPreview.Hide();

            if (data.TryGetArray(out PresetManagerColumnViewModel[] toMove))
                viewModel.Move(toMove, target, moveAfter);
            else if (data.TryGetArray(out ColumnViewModelPreset[] toAdd))
                viewModel.Add(toAdd, target, moveAfter);
        }

        public void AddAvailableListDraggable(UIElement element)
        {
            RemoveAvailableListDraggable(element);
            availableListDragEventSource.Attach(element);
        }

        public void RemoveAvailableListDraggable(UIElement element)
        {
            availableListDragEventSource.Detach(element);
        }

        public void AddLayoutListDraggable(UIElement element)
        {
            RemoveLayoutListDraggable(element);
            layoutListDragEventSource.Attach(element);
        }

        public void RemoveLayoutListDraggable(UIElement element)
        {
            layoutListDragEventSource.Detach(element);
        }
    }
}
