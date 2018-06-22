namespace EventTraceKit.VsExtension.Views.PresetManager
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;
    using Windows;

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

        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            if (viewModel.IsDialogStateDirty)
                viewModel.ApplyChanges();
            Close();
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnApplyButtonClick(object sender, RoutedEventArgs e)
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

        private void OnLayoutListKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && Keyboard.Modifiers == ModifierKeys.None)
                viewModel.Remove(layoutList.SelectedItems.Cast<PresetManagerColumnViewModel>().ToArray());
        }
    }
}
