namespace EventTraceKit.VsExtension.Windows
{
    using System.Windows;

    public static class DataObjectExtensions
    {
        public static bool TryGetArray<T>(this IDataObject data, out T[] payload)
        {
            if (data.GetDataPresent(typeof(T))) {
                payload = new[] { (T)data.GetData(typeof(T)) };
                return true;
            }

            if (data.GetDataPresent(typeof(T[]))) {
                payload = (T[])data.GetData(typeof(T[]));
                return true;
            }

            payload = null;
            return false;
        }
    }
}
