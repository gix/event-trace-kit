namespace EventTraceKit.VsExtension.Serialization
{
    using System.IO;

    public static class XamlSerializerExtensions
    {
        public static MemoryStream SaveToStream(
            this IXamlSerializer serializer, object element)
        {
            var stream = new MemoryStream();
            serializer.Save(element, stream);
            stream.Position = 0;
            return stream;
        }

        public static string SaveToString(
            this IXamlSerializer serializer, object element)
        {
            using (var stream = serializer.SaveToStream(element))
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }

        public static T LoadFromString<T>(
            this IXamlSerializer serializer, string xml)
        {
            using (var stream = new MemoryStream()) {
                using (var writer = new StreamWriter(stream))
                    writer.Write(xml);

                stream.Position = 0;
                return serializer.Load<T>(stream);
            }
        }
    }
}
