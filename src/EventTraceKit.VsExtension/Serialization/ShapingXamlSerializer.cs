namespace EventTraceKit.VsExtension.Serialization
{
    using System;
    using System.IO;

    public class ShapingXamlSerializer<TSerializedBaseType> : IXamlSerializer
    {
        private readonly SafeXamlSerializer serializer;
        private readonly SerializationShaper<TSerializedBaseType> shaper;

        public ShapingXamlSerializer()
            : this(new SafeXamlSerializer(), new SerializationShaper<TSerializedBaseType>())
        {
        }

        public ShapingXamlSerializer(
            SafeXamlSerializer serializer, SerializationShaper<TSerializedBaseType> shaper)
        {
            this.serializer = serializer;
            this.shaper = shaper;
        }

        public void Save(object element, Stream stream)
        {
            serializer.Save(ConvertToSerializedShape(element), stream);
        }

        public T Load<T>(Stream stream)
        {
            return ConvertFromSerializedShape<T>(
                serializer.Load<TSerializedBaseType>(stream));
        }

        private TSerializedBaseType ConvertToSerializedShape(object element)
        {
            TSerializedBaseType serialized;
            if (!shaper.TrySerialize(element, out serialized))
                throw new InvalidOperationException("Unable to convert object to serialized shape.");

            return serialized;
        }

        private T ConvertFromSerializedShape<T>(TSerializedBaseType serialized)
        {
            T element;
            if (!shaper.TryDeserialize(serialized, out element))
                throw new InvalidOperationException("Unable to convert object from serialized shape.");

            return element;
        }
    }
}
