namespace EventTraceKit.VsExtension
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public abstract class ObservableModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(
            ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            RaisePropertyChanged(propertyName);
            return true;
        }

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            OnPropertyChanged(propertyName);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void RaisePropertyChanged(PropertyChangedEventArgs args)
        {
            OnPropertyChanged(args.PropertyName);
            PropertyChanged?.Invoke(this, args);
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
        }
    }
}
