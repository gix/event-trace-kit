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
        private readonly IMapper mapper;

        public ShapingXamlSerializer(SafeXamlSerializer serializer, IMapper mapper)
        {
            this.serializer = serializer;
            this.mapper = mapper;
        }

        public void Save(object element, Stream stream)
        {
            serializer.Save(ConvertToSerializedShape<TSerializedBaseType, object>(element), stream);
        }

        public void Save<T>(IEnumerable<T> elements, Stream stream)
        {
            serializer.Save(elements.Select(ConvertToSerializedShape<TSerializedBaseType, T>), stream);
        }

        public T Load<T>(Stream stream)
        {
            return ConvertFromSerializedShape<T>(
                serializer.Load<TSerializedBaseType>(stream));
        }

        public IReadOnlyList<T> LoadMultiple<T>(Stream stream)
        {
            return serializer.LoadMultiple<TSerializedBaseType>(stream)
                .Select(ConvertFromSerializedShape<T>).ToList();
        }

        public TSerialized ConvertToSerializedShape<TSerialized, T>(T element)
            where TSerialized : class, TSerializedBaseType
        {
            if (element is TSerialized serializedType)
                return serializedType;
            if (element == null)
                return null;

            var sourceType = element.GetType();
            var targetType = sourceType.GetCustomAttribute<SerializedShapeAttribute>()?.Shape;
            if (targetType == null)
                throw new InvalidOperationException("Source type '{sourceType}' has no serialized shape");

            return (TSerialized)mapper.Map(element, sourceType, targetType);
        }

        public T ConvertFromSerializedShape<T>(TSerializedBaseType serialized)
        {
            return mapper.Map<T>(serialized);
        }
    }
}
