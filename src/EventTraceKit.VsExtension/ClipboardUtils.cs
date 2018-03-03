namespace EventTraceKit.VsExtension
{
    using System.Runtime.InteropServices;
    using System.Windows;

    public static class ClipboardUtils
    {
        public static bool TryGet<T>(out T value, out string text)
            where T : class
        {
            value = default;
            text = default;

            var dataObj = Clipboard.GetDataObject();
            if (dataObj == null)
                return false;

            if (dataObj.GetDataPresent(typeof(T)))
                value = (T)dataObj.GetData(typeof(T));
            if (dataObj.GetDataPresent(DataFormats.UnicodeText, true))
                text = (string)dataObj.GetData(DataFormats.UnicodeText, true);

            return value != null || text != null;
        }

        public static bool Set(object value, string text)
        {
            var data = new DataObject();
            data.SetData(value);
            data.SetText(text, TextDataFormat.UnicodeText);
            Clipboard.SetDataObject(data);

            return TrySetDataObject(data);
        }

        public static bool TryGetText(out string text)
        {
            text = default;

            var dataObj = Clipboard.GetDataObject();
            if (dataObj == null || !dataObj.GetDataPresent(DataFormats.UnicodeText, true))
                return false;

            text = (string)dataObj.GetData(DataFormats.UnicodeText, true);
            return true;
        }

        public static bool SetText(string text)
        {
            var data = new DataObject();
            if (text != null)
                data.SetData(DataFormats.Text, text);

            return TrySetDataObject(data);
        }

        public static bool TrySetDataObject(object data)
        {
            try {
                Clipboard.SetDataObject(data);
                return true;
            } catch (ExternalException) {
                return false;
            }
        }
    }
}
