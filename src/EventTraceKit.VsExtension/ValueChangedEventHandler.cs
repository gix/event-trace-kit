namespace EventTraceKit.VsExtension
{
    using System.Windows;

    public delegate void ValueChangedEventHandler<T>(object sender, ValueChangedEventArgs<T> e);

    public static class ValueChangedEventHandlerExtensions
    {
        public static void Raise<T>(
            this ValueChangedEventHandler<T> handler, object sender, T oldValue, T newValue)
        {
            if (handler != null) {
                var args = ValueChangedEventArgs<T>.Get(oldValue, newValue);
                handler(sender, args);
                args.Return();
            }
        }

        public static void Raise<T>(
            this ValueChangedEventHandler<T> handler, object sender,
            DependencyPropertyChangedEventArgs e)
        {
            if (handler != null) {
                var args = ValueChangedEventArgs<T>.Get((T)e.OldValue, (T)e.NewValue);
                handler(sender, args);
                args.Return();
            }
        }
    }
}
