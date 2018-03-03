namespace EventTraceKit.VsExtension.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using AutoMapper;

    public class ShapingXamlSerializer<TSerializedBaseType> : IXamlSerializer
        where TSerializedBaseType : class
    {
        private readonly SafeXamlSerializer serializer;
        private readonly SerializationMapper<TSerializedBaseType> shaper;
        private readonly IMapper mapper;

        public ShapingXamlSerializer()
            : this(new SafeXamlSerializer(), new SerializationMapper<TSerializedBaseType>())
        {
        }

        public ShapingXamlSerializer(
            SafeXamlSerializer serializer, SerializationMapper<TSerializedBaseType> shaper,
            IMapper mapper = null)
        {
            this.serializer = serializer;
            this.shaper = shaper;
            this.mapper = mapper;
        }

        public void Save(object element, Stream stream)
        {
            serializer.Save(ConvertToSerializedShape(element), stream);
        }

        public void Save(IEnumerable<object> elements, Stream stream)
        {
            serializer.Save(elements.Select(ConvertToSerializedShape), stream);
        }

        public T Load<T>(Stream stream)
        {
            return ConvertFromSerializedShape<T>(
                serializer.Load<TSerializedBaseType>(stream));
        }

        public IReadOnlyList<T> LoadMultiple<T>(Stream stream)
        {
            return serializer.LoadMultiple<TSerializedBaseType>(stream)
                .Select(ConvertFromSerializedShape<T>).ToArray();
        }

        private TSerializedBaseType ConvertToSerializedShape(object element)
        {
            if (element is TSerializedBaseType serializedType)
                return serializedType;
            if (element == null)
                return null;

            var sourceType = element.GetType();
            var targetType = sourceType.GetCustomAttribute<SerializedShapeAttribute>()?.Shape;
            return (TSerializedBaseType)mapper.Map(element, sourceType, targetType);

            if (!shaper.TrySerialize(element, out var serialized))
                throw new InvalidOperationException("Unable to convert object to serialized shape.");

            return serialized;
        }

        private T ConvertFromSerializedShape<T>(TSerializedBaseType serialized)
        {
            return mapper.Map<T>(serialized);

            if (shaper.TryDeserialize(serialized, out T element))
                return element;

            if (typeof(TSerializedBaseType).IsAssignableFrom(typeof(T)))
                return (T)(object)serialized;

            throw new InvalidOperationException("Unable to convert object from serialized shape.");
        }
    }
}
