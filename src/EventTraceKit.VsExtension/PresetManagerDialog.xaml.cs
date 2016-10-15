namespace EventTraceKit.VsExtension
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;
    using Controls;
    using Windows;
    using Microsoft.VisualStudio.PlatformUI;

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

        public static readonly DependencyProperty HeaderBrushProperty =
            DependencyProperty.Register(
                nameof(HeaderBrush),
                typeof(Brush),
                typeof(PresetManagerDialog),
                new PropertyMetadata());

        public Brush HeaderBrush
        {
            get { return (Brush)GetValue(HeaderBrushProperty); }
            set { SetValue(HeaderBrushProperty, value); }
        }

        private void SaveButtonClickHandler(object sender, RoutedEventArgs e) { }

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

        public static void ShowPresetManagerDialog(AsyncDataViewModel adv)
        {
            CreateDialog(adv).ShowModal();
        }

        public static PresetManagerDialog CreateDialog(AsyncDataViewModel adv)
        {
            if (adv == null)
                throw new ArgumentNullException(nameof(adv));

            var viewModel = new PresetManagerViewModel(adv);
            return new PresetManagerDialog(viewModel);
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

    [TemplatePart(Name = "PART_PopupContainer", Type = typeof(Control))]
    [TemplatePart(Name = "PART_Popup", Type = typeof(Popup))]
    public class DropDownBox : HeaderedContentControl
    {
        // Fields
        private DelegateCommand closeDropDownCommand;
        public static readonly DependencyProperty IsDropDownOpenProperty;
        private DelegateCommand openDropDownCommand;
        public const string PART_Popup = "PART_Popup";
        public const string PART_PopupContainer = "PART_PopupContainer";
        public static readonly DependencyProperty PopupContainerPartProperty;
        private static readonly DependencyPropertyKey PopupContainerPartPropertyKey;
        private DelegateCommand toggleIsDropDownOpenCommand;

        static DropDownBox()
        {
            IsDropDownOpenProperty = DependencyProperty.Register("IsDropDownOpen", typeof(bool), typeof(DropDownBox), new PropertyMetadata(Boxed.False, (d, e) => ((DropDownBox)d).IsDropDownOpenPropertyChanged(e)));
            PopupContainerPartPropertyKey = DependencyProperty.RegisterReadOnly("PopupContainerPart", typeof(Control), typeof(DropDownBox), new PropertyMetadata(null));
            PopupContainerPartProperty = PopupContainerPartPropertyKey.DependencyProperty;
            FrameworkElement.DefaultStyleKeyProperty.OverrideMetadata(typeof(DropDownBox), new FrameworkPropertyMetadata(typeof(DropDownBox)));
        }
        public bool IsDropDownOpen
        {
            get
            {
                return (bool)base.GetValue(IsDropDownOpenProperty);
            }
            set
            {
                base.SetValue(IsDropDownOpenProperty, value);
            }
        }
        public ICommand OpenDropDownCommand
        {
            get
            {
                base.VerifyAccess();
                if (this.openDropDownCommand == null) {
                    this.openDropDownCommand = new DelegateCommand(delegate (object _) {
                        this.OpenDropDown();
                    });
                }
                return this.openDropDownCommand;
            }
        }
        public DropDownBox()
        {
            base.Loaded += new RoutedEventHandler(this.OnLoaded);
            base.Unloaded += new RoutedEventHandler(this.OnUnloaded);
        }



        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            this.LoadPartsFromTemplate(null);
        }
        public void OpenDropDown()
        {
            this.IsDropDownOpen = true;
        }
        public void CloseDropDown()
        {
            this.IsDropDownOpen = false;
        }

        private bool TryGetPopupPart(out Popup p)
        {
            p = null;
            Control popupContainerPart = this.PopupContainerPart;
            if (popupContainerPart == null) {
                return false;
            }
            ControlTemplate template = popupContainerPart.Template;
            if (template == null) {
                return false;
            }
            p = template.FindName("PART_Popup", popupContainerPart) as Popup;
            if (p == null) {
                return false;
            }
            return true;
        }

        public void ToggleIsDropDownOpen()
        {
            this.IsDropDownOpen = !this.IsDropDownOpen;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Popup popup;
            this.LoadPartsFromTemplate(base.Template);
            if (this.TryGetPopupPart(out popup)) {
                popup.Opened += delegate (object s2, EventArgs e2) {
                    UIElement searchVisual = ((Popup)s2).Child;
                    if (searchVisual != null) {
                        searchVisual.FindVisualChild<FrameworkElement>(fx => fx.Focusable && fx.Focus());
                    }
                };
                popup.Closed += delegate (object s2, EventArgs e2) {
                    if (this.PopupContainerPart != null) {
                        this.PopupContainerPart.Focus();
                    }
                };
                popup.Child.LostKeyboardFocus += delegate (object s2, KeyboardFocusChangedEventArgs e2) {
                    UIElement element = (UIElement)s2;
                    if (!element.IsKeyboardFocusWithin && popup.IsOpen) {
                        element.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    }
                };
            }
        }

        private void LoadPartsFromTemplate(ControlTemplate template)
        {
            if (template == null) {
                this.PopupContainerPart = null;
            } else {
                this.PopupContainerPart = base.Template.FindName("PART_PopupContainer", this) as Control;
            }
        }



        public override void OnApplyTemplate()
        {
            this.LoadPartsFromTemplate(base.Template);
            base.OnApplyTemplate();
        }


        public ICommand ToggleIsDropDownOpenCommand
        {
            get
            {
                base.VerifyAccess();
                if (this.toggleIsDropDownOpenCommand == null) {
                    this.toggleIsDropDownOpenCommand = new DelegateCommand(delegate (object _) {
                        this.ToggleIsDropDownOpen();
                    });
                }
                return this.toggleIsDropDownOpenCommand;
            }
        }


        public Control PopupContainerPart
        {
            get
            {
                return (Control)base.GetValue(PopupContainerPartProperty);
            }
            private set
            {
                base.SetValue(PopupContainerPartPropertyKey, value);
            }
        }


        public ICommand CloseDropDownCommand
        {
            get
            {
                base.VerifyAccess();
                if (this.closeDropDownCommand == null) {
                    this.closeDropDownCommand = new DelegateCommand(delegate (object _) {
                        this.CloseDropDown();
                    });
                }
                return this.closeDropDownCommand;
            }
        }


        private void IsDropDownOpenPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.VerifyAccess();
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
            var item = dragSource as ListBoxItem;
            if (item != null) {
                var listBox = item.GetParentListBox();
                dragData = listBox.GetOrderedSelectedItemsArray();
            }
        }

        private void OnAvailableListDragStart()
        {
            var columns = dragData as ColumnViewModelPreset[];
            if (columns != null) {
                var data = new DataObject(typeof(ColumnViewModelPreset[]), columns);
                DragDrop.DoDragDrop(dragSource, data, DragDropEffects.Copy);
            }
        }

        private void OnLayoutListDragStart()
        {
            var columns = dragData as PresetManagerColumnViewModel[];
            if (columns != null) {
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
            PresetManagerColumnViewModel[] toRemove;
            if (data.TryGetArray(out toRemove))
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
            var relativeTo = sender as FrameworkElement;
            if (relativeTo == null)
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
            var relativeTo = sender as ListBoxItem;
            if (relativeTo == null)
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
            var relativeTo = sender as ListBoxItem;
            if (relativeTo == null)
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
            ColumnViewModelPreset[] toAdd;
            if (data.TryGetArray(out toAdd))
                return true;

            PresetManagerColumnViewModel[] toMove;
            if (data.TryGetArray(out toMove))
                return viewModel.CanMove(toMove, target, moveAfter);

            return false;
        }

        private void DoDrop(IDataObject data, PresetManagerColumnViewModel target, bool moveAfter)
        {
            layoutListDragPreview.Hide();

            PresetManagerColumnViewModel[] toMove;
            ColumnViewModelPreset[] toAdd;
            if (data.TryGetArray(out toMove))
                viewModel.Move(toMove, target, moveAfter);
            else if (data.TryGetArray(out toAdd))
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
