namespace EventTraceKit.VsExtension
{
    using System.Runtime.InteropServices;
    using System.Windows;

    public static class ClipboardUtils
    {
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
