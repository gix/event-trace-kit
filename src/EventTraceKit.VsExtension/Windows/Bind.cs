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

        private static async void Async_ComboBoxOnDropDownOpened(object sender, EventArgs e)
        {
            var source = (ComboBox)sender;

            var itemsSourceTask = GetItemsSourceAsync(source);
            if (itemsSourceTask == null) {
                source.ItemsSource = null;
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
                source.ItemsSource = null;
                return;
            }

            SetItemsSource(source, itemsSourceProvider());
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
                source.ItemsSource = null;
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
            if (!comboBox.IsEditable)
                return;

            using (new SuspendBindingScope(comboBox, ComboBox.TextProperty)) {
                comboBox.ItemsSource = null;
                comboBox.Items.Clear();
            }
        }

        private static async Task SetItemsSourceWithLoading(
            ComboBox comboBox, Task<IEnumerable> itemsSourceTask)
        {
            if (itemsSourceTask.IsCompleted) {
                SetItemsSource(comboBox, await itemsSourceTask);
                return;
            }

            var cts = CreateDropDownClosedCts(comboBox);
            IEnumerable itemsSource;
            using (new ComboBoxLoadingScope(comboBox, GetLoadingContent(comboBox)))
                itemsSource = await itemsSourceTask;
            if (!cts.IsCancellationRequested)
                SetItemsSource(comboBox, itemsSource);
        }

        private static void SetItemsSource(ComboBox comboBox, IEnumerable itemsSource)
        {
            if (comboBox.IsEditable) {
                using (new SuspendBindingScope(comboBox, ComboBox.TextProperty))
                    comboBox.ItemsSource = itemsSource;
            } else {
                comboBox.ItemsSource = itemsSource;
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
