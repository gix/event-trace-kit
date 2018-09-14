namespace EventTraceKit.VsExtension.Windows
{
    using System;
    using System.Collections;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;

    public static class Bind
    {
        #region public object LoadingContent { get; set; }

        /// <summary>
        ///   Identifies the <c>LoadingContent</c> attached dependency property.
        /// </summary>
        public static readonly DependencyProperty LoadingContentProperty =
            DependencyProperty.RegisterAttached(
                "LoadingContent",
                typeof(object),
                typeof(Bind),
                new PropertyMetadata("Loading…"));

        /// <summary>
        ///   Gets the content displayed when an items source is loading.
        /// </summary>
        public static object GetLoadingContent(DependencyObject d)
        {
            return d.GetValue(LoadingContentProperty);
        }

        /// <summary>
        ///   Sets the content displayed when an items source is loading.
        /// </summary>
        public static void SetLoadingContent(DependencyObject d, object value)
        {
            d.SetValue(LoadingContentProperty, value);
        }

        #endregion

        public static readonly DependencyProperty ItemsSourceAsyncProperty =
            DependencyProperty.RegisterAttached(
                "ItemsSourceAsync",
                typeof(Task<IEnumerable>),
                typeof(Bind),
                new FrameworkPropertyMetadata(OnItemsSourceAsyncChanged));

        public static Task<IEnumerable> GetItemsSourceAsync(ComboBox d)
        {
            return (Task<IEnumerable>)d.GetValue(ItemsSourceAsyncProperty);
        }

        public static void SetItemsSourceAsync(ComboBox d, Task<IEnumerable> value)
        {
            d.SetValue(ItemsSourceAsyncProperty, value);
        }

        private static void OnItemsSourceAsyncChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ComboBox comboBox) {
                if (e.OldValue != null) {
                    comboBox.DropDownOpened -= Async_ComboBoxOnDropDownOpened;
                    comboBox.DropDownClosed -= Async_ComboBoxOnDropDownClosed;
                }

                if (e.NewValue != null) {
                    comboBox.DropDownClosed += Async_ComboBoxOnDropDownClosed;
                    comboBox.DropDownOpened += Async_ComboBoxOnDropDownOpened;
                }
            }
        }

        public static readonly DependencyProperty ItemsSourceProviderProperty =
            DependencyProperty.RegisterAttached(
                "ItemsSourceProvider",
                typeof(Func<IEnumerable>),
                typeof(Bind),
                new FrameworkPropertyMetadata(OnItemsSourceProviderChanged));

        public static Func<IEnumerable> GetItemsSourceProvider(ComboBox d)
        {
            return (Func<IEnumerable>)d.GetValue(ItemsSourceProviderProperty);
        }

        public static void SetItemsSourceProvider(ComboBox d, Func<IEnumerable> value)
        {
            d.SetValue(ItemsSourceProviderProperty, value);
        }

        private static void OnItemsSourceProviderChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ComboBox comboBox) {
                if (e.OldValue != null) {
                    comboBox.DropDownOpened -= Provider_ComboBoxOnDropDownOpened;
                    comboBox.DropDownClosed -= Provider_ComboBoxOnDropDownClosed;
                }

                if (e.NewValue != null) {
                    comboBox.DropDownClosed += Provider_ComboBoxOnDropDownClosed;
                    comboBox.DropDownOpened += Provider_ComboBoxOnDropDownOpened;
                }
            }
        }

        public static readonly DependencyProperty ItemsSourceProviderAsyncProperty =
            DependencyProperty.RegisterAttached(
                "ItemsSourceProviderAsync",
                typeof(Func<Task<IEnumerable>>),
                typeof(Bind),
                new FrameworkPropertyMetadata(OnItemsSourceProviderAsyncChanged));

        public static Func<Task<IEnumerable>> GetItemsSourceProviderAsync(ComboBox d)
        {
            return (Func<Task<IEnumerable>>)d.GetValue(ItemsSourceProviderAsyncProperty);
        }

        public static void SetItemsSourceProviderAsync(ComboBox d, Func<Task<IEnumerable>> value)
        {
            d.SetValue(ItemsSourceProviderAsyncProperty, value);
        }

        private static void OnItemsSourceProviderAsyncChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ComboBox comboBox) {
                if (e.OldValue != null) {
                    comboBox.DropDownOpened -= AsyncProvider_ComboBoxOnDropDownOpened;
                    comboBox.DropDownClosed -= AsyncProvider_ComboBoxOnDropDownClosed;
                }

                if (e.NewValue != null) {
                    comboBox.DropDownClosed += AsyncProvider_ComboBoxOnDropDownClosed;
                    comboBox.DropDownOpened += AsyncProvider_ComboBoxOnDropDownOpened;
                }
            }
        }

        public static readonly DependencyProperty DefaultItemProperty =
            DependencyProperty.RegisterAttached(
                "DefaultItem",
                typeof(object),
                typeof(Bind),
                new FrameworkPropertyMetadata(OnSelectedItemChanged));

        public static object GetDefaultItem(ComboBox d)
        {
            return d.GetValue(DefaultItemProperty);
        }

        public static void SetDefaultItem(ComboBox d, object value)
        {
            d.SetValue(DefaultItemProperty, value);
        }

        private static void OnSelectedItemChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ComboBox comboBox) {
                EnsureSelectedItemAvailable(comboBox, e.NewValue);
            }
        }

        private static async void Async_ComboBoxOnDropDownOpened(object sender, EventArgs e)
        {
            var source = (ComboBox)sender;

            var itemsSourceTask = GetItemsSourceAsync(source);
            if (itemsSourceTask == null) {
                ClearItemsSource(source);
                return;
            }

            await SetItemsSourceWithLoading(source, itemsSourceTask);
        }

        private static void Async_ComboBoxOnDropDownClosed(object sender, EventArgs e)
        {
        }

        private static void Provider_ComboBoxOnDropDownOpened(object sender, EventArgs e)
        {
            var source = (ComboBox)sender;

            var itemsSourceProvider = GetItemsSourceProvider(source);
            if (itemsSourceProvider == null) {
                ClearItemsSource(source);
                return;
            }

            SetItemsSource(source, itemsSourceProvider(), source.SelectedItem);
        }

        private static void Provider_ComboBoxOnDropDownClosed(object sender, EventArgs e)
        {
            ClearItemsSource((ComboBox)sender);
        }

        private static async void AsyncProvider_ComboBoxOnDropDownOpened(object sender, EventArgs e)
        {
            var source = (ComboBox)sender;

            var itemsSourceTaskProvider = GetItemsSourceProviderAsync(source);
            if (itemsSourceTaskProvider == null) {
                ClearItemsSource(source);
                return;
            }

            await SetItemsSourceWithLoading(source, itemsSourceTaskProvider());
        }

        private static void AsyncProvider_ComboBoxOnDropDownClosed(object sender, EventArgs e)
        {
            ClearItemsSource((ComboBox)sender);
        }

        private static void ClearItemsSource(ComboBox comboBox)
        {
            void Clear()
            {
                var selectedItem = comboBox.SelectedItem;

                comboBox.ItemsSource = null;
                comboBox.Items.Clear();

                if (selectedItem != null) {
                    if (comboBox.Items.Count == 0)
                        comboBox.Items.Add(selectedItem);
                    else
                        comboBox.Items[0] = selectedItem;
                }

                comboBox.SelectedItem = selectedItem;
            }

            if (comboBox.IsEditable) {
                using (new SuspendBindingScope(comboBox, ComboBox.TextProperty))
                    Clear();
            } else {
                Clear();
            }
        }

        private static void EnsureSelectedItemAvailable(
            ComboBox comboBox, object newSelectedValue)
        {
            if (comboBox.ItemsSource != null)
                return;

            using (new SuspendBindingScope(comboBox, ComboBox.TextProperty)) {
                if (comboBox.Items.Count == 0)
                    comboBox.Items.Add(newSelectedValue);
                else
                    comboBox.Items[0] = newSelectedValue;
            }
        }

        private static async Task SetItemsSourceWithLoading(
            ComboBox comboBox, Task<IEnumerable> itemsSourceTask)
        {
            var selectedItem = comboBox.SelectedItem;

            if (itemsSourceTask.IsCompleted) {
                SetItemsSource(comboBox, await itemsSourceTask, selectedItem);
                return;
            }

            var cts = CreateDropDownClosedCts(comboBox);
            IEnumerable itemsSource;
            using (new ComboBoxLoadingScope(comboBox, GetLoadingContent(comboBox)))
                itemsSource = await itemsSourceTask;
            if (!cts.IsCancellationRequested)
                SetItemsSource(comboBox, itemsSource, selectedItem);
        }

        private static void SetItemsSource(
            ComboBox comboBox, IEnumerable itemsSource, object selectedItem)
        {
            if (comboBox.ItemsSource == null && comboBox.Items.Count != 0)
                comboBox.Items.Clear();

            if (comboBox.IsEditable) {
                using (new SuspendBindingScope(comboBox, ComboBox.TextProperty)) {
                    comboBox.ItemsSource = itemsSource;
                    comboBox.SelectedItem = selectedItem;
                }
            } else {
                comboBox.ItemsSource = itemsSource;
                comboBox.SelectedItem = selectedItem;
            }
        }

        private static CancellationTokenSource CreateDropDownClosedCts(ComboBox comboBox)
        {
            var cts = new CancellationTokenSource();
            void OnDropDownClosed(object sender, EventArgs args)
            {
                cts.Cancel();
                comboBox.DropDownClosed -= OnDropDownClosed;
            }
            comboBox.DropDownClosed += OnDropDownClosed;
            return cts;
        }
    }
}
