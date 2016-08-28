namespace EventTraceKit.VsExtension
{
    using System;
    using System.Threading.Tasks;
    using System.Windows.Input;

    public class AsyncDelegateCommand : ICommand
    {
        private readonly Func<object, Task> execute;
        private readonly Predicate<object> canExecute;
        private bool isExecuting;

        public AsyncDelegateCommand(Func<object, Task> execute)
            : this(execute, null)
        {
        }

        public AsyncDelegateCommand(Func<Task> execute)
            : this(async obj => await execute(), null)
        {
        }

        public AsyncDelegateCommand(
            Func<object, Task> execute, Predicate<object> canExecute)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        private bool IsExecuting
        {
            get { return isExecuting; }
            set
            {
                if (isExecuting != value) {
                    isExecuting = value;
                    RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        ///   Occurs when changes occur that affect whether or not the command
        ///   should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        ///   Defines the method that determines whether the command can execute
        ///   in its current state.
        /// </summary>
        /// <param name="parameter">
        ///   Data used by the command. If the command does not require data to
        ///   be passed, this object can be set to <see langword="null"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if this command can be executed; otherwise,
        ///   <see langword="false"/>.
        /// </returns>
        public bool CanExecute(object parameter)
        {
            return !IsExecuting && (canExecute == null || canExecute(parameter));
        }

        /// <summary>
        ///   Defines the method to be called when the command is invoked.
        /// </summary>
        /// <param name="parameter">
        ///   Data used by the command. If the command does not require data to
        ///   be passed, this object can be set to <see langword="null"/>.
        /// </param>
        public async void Execute(object parameter)
        {
            if (!CanExecute(parameter))
                return;

            IsExecuting = true;
            try {
                await execute(parameter);
            } finally {
                IsExecuting = false;
            }
        }

        public void RaiseCanExecuteChanged()
        {
            OnCanExecuteChanged(EventArgs.Empty);
        }

        protected virtual void OnCanExecuteChanged(EventArgs e)
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
