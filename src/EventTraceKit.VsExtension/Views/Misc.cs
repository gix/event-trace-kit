namespace EventTraceKit.VsExtension.Views
{
    using System;
    using System.Collections;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Documents;
    using System.Windows.Input;
    using Microsoft.Expression.Interactivity.Layout;
    using Microsoft.VisualStudio.Threading;
    using Task = System.Threading.Tasks.Task;

    public class AsyncComboBoxItem : ComboBoxItem
    {
        static AsyncComboBoxItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(AsyncComboBoxItem),
                new FrameworkPropertyMetadata(typeof(ComboBoxItem)));
        }

        public AsyncComboBoxItem()
        {
            DefaultStyleKey = typeof(ComboBoxItem);
        }

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            // Noop
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            // Noop
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            // Noop
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            // Noop
        }
    }

    public class AsyncComboBox : ComboBox
    {
        private Popup dropDownPopup;
        private AdornerContainer loadingAdorner;

        static AsyncComboBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(AsyncComboBox),
                new FrameworkPropertyMetadata(typeof(ComboBox)));
        }

        public AsyncComboBox()
        {
            DefaultStyleKey = typeof(ComboBox);
        }

        private static readonly DependencyPropertyKey IsLoadingPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsLoading),
                typeof(bool),
                typeof(AsyncComboBox),
                new PropertyMetadata(
                    false, (d, e) => ((AsyncComboBox)d).OnIsLoadingChanged((bool)e.NewValue)));

        public static readonly DependencyProperty IsLoadingProperty =
            IsLoadingPropertyKey.DependencyProperty;

        public static readonly DependencyProperty LoadingTemplateProperty =
            DependencyProperty.Register(
                nameof(LoadingTemplate),
                typeof(DataTemplate),
                typeof(AsyncComboBox),
                new PropertyMetadata(
                    null, (d, e) => ((AsyncComboBox)d).OnLoadingTemplateChanged((DataTemplate)e.NewValue)));

        public static readonly DependencyProperty ItemsSourceProviderProperty =
            DependencyProperty.Register(
                nameof(ItemsSourceProvider),
                typeof(Func<Task<IEnumerable>>),
                typeof(AsyncComboBox),
                new PropertyMetadata(
                    null, (d, e) => ((AsyncComboBox)d).OnItemsSourceProviderChanged()));

        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            private set => SetValue(IsLoadingPropertyKey, value);
        }

        public DataTemplate LoadingTemplate
        {
            get => (DataTemplate)GetValue(LoadingTemplateProperty);
            set => SetValue(LoadingTemplateProperty, value);
        }

        public Func<Task<IEnumerable>> ItemsSourceProvider
        {
            get => (Func<Task<IEnumerable>>)GetValue(ItemsSourceProviderProperty);
            set => SetValue(ItemsSourceProviderProperty, value);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            dropDownPopup = GetTemplateChild("PART_Popup") as Popup;
        }

        protected override void OnDropDownOpened(EventArgs e)
        {
            base.OnDropDownOpened(e);
            RefreshAsync().Forget();
        }

        protected override void OnDropDownClosed(EventArgs e)
        {
            base.OnDropDownClosed(e);
            ItemsSource = null;
        }

        private void OnIsLoadingChanged(bool newValue)
        {
            if (dropDownPopup == null)
                return;

            var layer = AdornerLayer.GetAdornerLayer(dropDownPopup);
            if (layer == null)
                return;

            if (loadingAdorner != null)
                layer.Remove(loadingAdorner);

            if (newValue) {
                loadingAdorner = CreateLoadingAdorner();
                layer.Add(loadingAdorner);
            }
        }

        private void OnLoadingTemplateChanged(DataTemplate newValue)
        {
            if (loadingAdorner?.Child is ContentControl control)
                control.ContentTemplate = newValue;
        }

        private async void OnItemsSourceProviderChanged()
        {
            await RefreshAsync();
        }

        private AdornerContainer CreateLoadingAdorner()
        {
            var content = new ContentControl {
                ContentTemplate = LoadingTemplate
            };
            return new AdornerContainer(content);
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new AsyncComboBoxItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is AsyncComboBoxItem;
        }

        private async Task RefreshAsync()
        {
            IsLoading = true;
            try {
                if (ItemsSourceProvider != null)
                    ItemsSource = await ItemsSourceProvider();
                else
                    ItemsSource = null;
            } catch (Exception) {
                ItemsSource = null;
            } finally {
                IsLoading = false;
            }
        }
    }
}
