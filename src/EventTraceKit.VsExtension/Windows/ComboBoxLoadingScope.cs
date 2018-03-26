namespace EventTraceKit.VsExtension.Windows
{
    using System;
    using System.Collections;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;

    internal sealed class ComboBoxLoadingScope : IDisposable
    {
        private readonly ComboBox comboBox;

        private readonly IEnumerable oldItemsSource;

        private readonly Binding selectedItemBinding;
        private readonly Binding selectedIndexBinding;
        private readonly Binding selectedValueBinding;
        private readonly Binding selectedValuePathBinding;
        private readonly object selectedValuePath;
        private readonly Binding textBinding;

        private readonly bool usesDisplayMemberPath;
        private readonly Binding displayMemberPathBinding;
        private readonly object displayMemberPath;
        private readonly Binding itemTemplateBinding;
        private readonly object itemTemplate;
        private readonly Binding itemTemplateSelectorBinding;
        private readonly object itemTemplateSelector;
        private readonly Binding textPathBinding;
        private readonly object textPath;

        public ComboBoxLoadingScope(ComboBox comboBox, object loadingItem)
        {
            this.comboBox = comboBox;
            oldItemsSource = comboBox.ItemsSource;

            // Suspend all selection-relevant bindings to ensure that the
            // data context is not touched while the loading item is shown.
            // To suspend we use an explicit UpdateSourceTrigger on cloned
            // Bindings (because they cannot be changed once used). Also
            // simply clearing the bindings does not work because
            // dependencies between these properties results in data context
            // updates when removing.
            SuspendBinding(Selector.SelectedItemProperty, out selectedItemBinding);
            SuspendBinding(Selector.SelectedIndexProperty, out selectedIndexBinding);
            SuspendBinding(Selector.SelectedValueProperty, out selectedValueBinding);
            SuspendBinding(Selector.SelectedValuePathProperty, out selectedValuePathBinding);
            SuspendBinding(ComboBox.TextProperty, out textBinding);

            selectedValuePath = comboBox.GetValue(Selector.SelectedValuePathProperty);
            comboBox.ClearValue(Selector.SelectedValuePathProperty);

            // Clear out any item templates to avoid binding errors when adding
            // the temporary items.
            if (!string.IsNullOrEmpty(comboBox.DisplayMemberPath)) {
                usesDisplayMemberPath = true;
                SuspendBinding(
                    ItemsControl.DisplayMemberPathProperty,
                    out displayMemberPathBinding,
                    out displayMemberPath);
            } else {
                SuspendBinding(
                    ItemsControl.ItemTemplateSelectorProperty,
                    out itemTemplateSelectorBinding,
                    out itemTemplateSelector);
                SuspendBinding(
                    ItemsControl.ItemTemplateProperty,
                    out itemTemplateBinding,
                    out itemTemplate);
            }
            SuspendBinding(TextSearch.TextPathProperty, out textPathBinding, out textPath);

            var selectedItem = comboBox.IsEditable ? comboBox.Text : comboBox.SelectedItem;
            SetLoading(comboBox, true, selectedItem, loadingItem);
        }

        public void Dispose()
        {
            SetLoading(comboBox, false);
            comboBox.ItemsSource = oldItemsSource;

            if (usesDisplayMemberPath) {
                RestoreBinding(
                    ItemsControl.DisplayMemberPathProperty, displayMemberPathBinding,
                    displayMemberPath);
            } else {
                RestoreBinding(
                    ItemsControl.ItemTemplateSelectorProperty, itemTemplateSelectorBinding,
                    itemTemplateSelector);
                RestoreBinding(
                    ItemsControl.ItemTemplateProperty, itemTemplateBinding, itemTemplate);
            }

            RestoreBinding(TextSearch.TextPathProperty, textPathBinding, textPath);

            if (selectedValuePath != DependencyProperty.UnsetValue)
                comboBox.SetValue(Selector.SelectedValuePathProperty, selectedValuePath);

            RestoreBinding(Selector.SelectedValueProperty, selectedValueBinding);
            RestoreBinding(Selector.SelectedValuePathProperty, selectedValuePathBinding);
            RestoreBinding(Selector.SelectedIndexProperty, selectedIndexBinding);
            RestoreBinding(Selector.SelectedItemProperty, selectedItemBinding);
            RestoreBinding(ComboBox.TextProperty, textBinding);
        }

        private void SuspendBinding(DependencyProperty dp, out Binding oldBinding, out object oldValue)
        {
            oldBinding = BindingOperations.GetBinding(comboBox, dp);
            if (oldBinding != null) {
                oldValue = null;
                var explicitBinding = oldBinding.Clone();
                explicitBinding.UpdateSourceTrigger = UpdateSourceTrigger.Explicit;
                BindingOperations.SetBinding(comboBox, dp, explicitBinding);
            } else {
                oldValue = comboBox.ReadLocalValue(dp);
                if (oldValue != DependencyProperty.UnsetValue)
                    comboBox.ClearValue(dp);
            }
        }

        private void SuspendBinding(DependencyProperty dp, out Binding oldBinding)
        {
            oldBinding = BindingOperations.GetBinding(comboBox, dp);
            if (oldBinding != null) {
                var explicitBinding = oldBinding.Clone();
                explicitBinding.UpdateSourceTrigger = UpdateSourceTrigger.Explicit;
                BindingOperations.SetBinding(comboBox, dp, explicitBinding);
            }
        }

        private void RestoreBinding(DependencyProperty dp, Binding binding, object value)
        {
            if (binding != null)
                BindingOperations.SetBinding(comboBox, dp, binding);
            else if (value != DependencyProperty.UnsetValue)
                comboBox.SetValue(dp, value);
        }

        private void RestoreBinding(DependencyProperty dp, Binding binding)
        {
            if (binding != null)
                BindingOperations.SetBinding(comboBox, dp, binding);
        }

        private static void SetLoading(
            ComboBox comboBox, bool isLoading, object selectedItem = null,
            object loadingItem = null)
        {
            if (isLoading) {
                comboBox.ItemsSource = null;
                comboBox.Items.Clear();

                // The SelectedItem should be preserved even for non-editable
                // ComboBoxes. Since it is impossible to have a SelectedItem
                // not contained in the ItemsSource we add a fake item.
                // Collapsed hides the item from the dropdown list but allows
                // its content to be shown as SelectionBoxItem.
                comboBox.Items.Add(new ComboBoxItem {
                    IsEnabled = false,
                    Content = selectedItem,
                    Visibility = Visibility.Collapsed
                });

                // The item displaying the loading message. Disabled so it
                // cannot be selected.
                comboBox.Items.Add(new ComboBoxItem {
                    IsEnabled = false,
                    Content = loadingItem
                });

                comboBox.SelectedIndex = 0;
            } else {
                comboBox.Items.Clear();
                comboBox.ItemsSource = null;
            }
        }
    }
}
