namespace EventTraceKit.VsExtension
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Media;
    using Controls;
    using Windows;
    using Extensions;

    public partial class PresetManagerDialog
    {
        private readonly PresetColumnDragBehavior dragBehavior;
        private readonly PresetManagerViewModel viewModel;

        private PresetManagerDialog()
        {
            InitializeComponent();
        }

        private PresetManagerDialog(PresetManagerViewModel viewModel)
            : this()
        {
            this.viewModel = viewModel;
            DataContext = viewModel;
            dragBehavior = new PresetColumnDragBehavior(this, viewModel, availableList, layoutList);
        }

        public static IValueConverter MultiplierConverter { get; } =
             new DelegateValueConverter<double, double, double>((x, param) => x * param, null);

        public static IValueConverter WidthToStringConverter { get; } =
            new DelegateValueConverter<int, string>(
                x => x.ToString(CultureInfo.CurrentCulture),
                x => {
                    int width;
                    if (!int.TryParse(x, NumberStyles.Integer, CultureInfo.CurrentCulture, out width))
                        return 100;
                    return width;
                });

        public static IValueConverter SeparatorToColorBrushConverter { get; } =
            new DelegateValueConverter<PresetManagerColumnViewModel, Brush>(x => {
                switch (x.ColumnType) {
                    case PresetManagerColumnType.LeftFreezableAreaSeparator:
                        return Brushes.Gray;
                    case PresetManagerColumnType.RightFreezableAreaSeparator:
                        return Brushes.DarkGray;
                    default:
                        return Brushes.Black;
                }
            });

        private void CloseButtonClickHandler(object sender, RoutedEventArgs e)
        {
            if (viewModel.IsDialogStateDirty)
                viewModel.ApplyChanges();
            Close();
        }

        private void CancelButtonClickHandler(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ApplyButtonClickHandler(object sender, RoutedEventArgs e)
        {
            viewModel.ApplyChanges();
        }

        public static PresetManagerDialog CreateDialog(AsyncDataViewModel adv)
        {
            if (adv == null)
                throw new ArgumentNullException(nameof(adv));

            var viewModel = new PresetManagerViewModel(adv);
            return new PresetManagerDialog(viewModel);
        }

        public static void ShowModalDialog(AsyncDataViewModel adv)
        {
            CreateDialog(adv).ShowModal();
        }

        private void OnAvailableListItemLoaded(object sender, RoutedEventArgs e)
        {
            var element = (UIElement)sender;
            dragBehavior.AddAvailableListDraggable(element);
        }

        private void OnAvailableListItemUnloaded(object sender, RoutedEventArgs e)
        {
            var element = (UIElement)sender;
            dragBehavior.RemoveAvailableListDraggable(element);
        }

        private void OnLayoutListItemLoaded(object sender, RoutedEventArgs e)
        {
            var element = (UIElement)sender;
            dragBehavior.AddLayoutListDraggable(element);
        }

        private void OnLayoutListItemUnloaded(object sender, RoutedEventArgs e)
        {
            var element = (UIElement)sender;
            dragBehavior.RemoveLayoutListDraggable(element);
        }

        private void OnLayoutListItemDragEnter(object sender, DragEventArgs e)
        {
            dragBehavior.OnLayoutListItemDragEnter(sender, e);
        }

        private void OnLayoutListItemDragOver(object sender, DragEventArgs e)
        {
            dragBehavior.OnLayoutListItemDragOver(sender, e);
        }

        private void OnLayoutListItemDrop(object sender, DragEventArgs e)
        {
            dragBehavior.OnLayoutListItemDrop(sender, e);
        }
    }

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
                layoutListDragPreview.HideOnly();
            }
        }

        public void OnLayoutListDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;

            var target = viewModel.PresetColumns.LastOrDefault();
            if (!CanDrop(e.Data, target, true)) {
                e.Effects = DragDropEffects.None;
                layoutListDragPreview.HideOnly();
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
                layoutListDragPreview.HideOnly();
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
                layoutListDragPreview.HideOnly();
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
