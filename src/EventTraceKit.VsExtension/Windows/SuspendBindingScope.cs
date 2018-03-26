namespace EventTraceKit.VsExtension.Windows
{
    using System;
    using System.Windows;
    using System.Windows.Data;

    public class SuspendBindingScope : IDisposable
    {
        private readonly DependencyObject obj;
        private readonly DependencyProperty property;

        private readonly Binding selectedItemBinding;

        public SuspendBindingScope(DependencyObject obj, DependencyProperty property)
        {
            this.obj = obj;
            this.property = property;

            SuspendBinding(property, out selectedItemBinding);
        }

        public void Dispose()
        {
            RestoreBinding(property, selectedItemBinding);
        }

        private void SuspendBinding(DependencyProperty dp, out Binding oldBinding)
        {
            oldBinding = BindingOperations.GetBinding(obj, dp);
            if (oldBinding != null) {
                // Used bindings cannot be modified thus we clone it first and
                // save the old binding.
                var explicitBinding = oldBinding.Clone();
                explicitBinding.UpdateSourceTrigger = UpdateSourceTrigger.Explicit;
                BindingOperations.SetBinding(obj, dp, explicitBinding);
            }
        }

        private void RestoreBinding(DependencyProperty dp, Binding binding)
        {
            if (binding != null)
                BindingOperations.SetBinding(obj, dp, binding);
        }
    }
}
