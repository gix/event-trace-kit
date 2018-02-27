namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Windows.Controls;
    using System.Windows.Input;

    public class MenuItemCommand : MenuItem, ICommand
    {
        public MenuItemCommand()
        {
            Command = this;
        }

        protected virtual bool CanExecuteCore(object parameter)
        {
            return true;
        }

        protected virtual void ExecuteCore(object parameter)
        {
        }

        event EventHandler ICommand.CanExecuteChanged
        {
            add {}
            remove {}
        }

        bool ICommand.CanExecute(object parameter)
        {
            return CanExecuteCore(parameter);
        }

        void ICommand.Execute(object parameter)
        {
            ExecuteCore(parameter);
        }
    }
}
