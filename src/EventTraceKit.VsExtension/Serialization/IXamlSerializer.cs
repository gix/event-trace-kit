namespace EventTraceKit.VsExtension.Serialization
{
    using System.IO;

    public interface IXamlSerializer
    {
        void Save(object element, Stream stream);
        T Load<T>(Stream stream);
    }
}
