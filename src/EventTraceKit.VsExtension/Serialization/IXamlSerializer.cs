namespace EventTraceKit.VsExtension.Serialization
{
    using System.Collections.Generic;
    using System.IO;

    public interface IXamlSerializer
    {
        void Save(object element, Stream stream);
        void Save<T>(IEnumerable<T> elements, Stream stream);
        T Load<T>(Stream stream);
        IReadOnlyList<T> LoadMultiple<T>(Stream stream);
    }
}
