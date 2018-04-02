namespace EventTraceKit.VsExtension.Serialization
{
    using System;
    using System.Linq;
    using System.Reflection;
    using AutoMapper;
    using Settings.Persistence;

    public class SettingsSerializer : ShapingXamlSerializer<SettingsElement>
    {
        private static readonly Lazy<IMapper> LazyMapper = new Lazy<IMapper>(() => {
            var cfg = new MapperConfiguration(c => {
                c.Mappers.Insert(0, new CommaSeparatedValuesToCollectionMapper());
                c.Mappers.Insert(0, new CollectionToCommaSeparatedValuesMapper());
                c.AddProfile(new SerializationProfile());
            });
            return cfg.CreateMapper();
        });

        public SettingsSerializer()
            : base(CreateXamlSerializer(), Mapper)
        {
        }

        public static SafeXamlSerializer CreateXamlSerializer()
        {
            var serializer = new SafeXamlSerializer(typeof(SettingsElement).Assembly);

            foreach (var type in typeof(SettingsElement).Assembly.GetTypes()) {
                if (typeof(SettingsElement).IsAssignableFrom(type))
                    serializer.AddKnownType(type);
            }

            return serializer;
        }

        public static IMapper Mapper => LazyMapper.Value;

        private sealed class SerializationProfile : Profile
        {
            public SerializationProfile()
            {
                var assembly = typeof(SettingsElement).Assembly;

                foreach (var targetType in assembly.GetTypes()) {
                    var sourceType = GetShape(targetType);
                    if (sourceType == null)
                        continue;

                    var m = CreateMapHierarchy(sourceType, targetType);
                    CreateMapHierarchy(targetType, sourceType);

                    AddCallbacks(m, targetType);
                }
            }

            private IMappingExpression CreateMapHierarchy(Type sourceType, Type targetType)
            {
                var m = CreateMap(sourceType, targetType);

                foreach (var sourceProperty in sourceType.GetProperties()) {
                    var serializedName = sourceProperty.GetCustomAttribute<SerializeAttribute>()?.SerializedName;
                    if (serializedName != null)
                        m.ForMember(serializedName, x => x.MapFrom(sourceProperty.Name));
                }

                foreach (var targetProperty in targetType.GetProperties()) {
                    var serializedName = targetProperty.GetCustomAttribute<SerializeAttribute>()?.SerializedName;
                    if (serializedName != null)
                        m.ForMember(targetProperty.Name, x => x.MapFrom(serializedName));
                }

                sourceType = sourceType.BaseType;
                targetType = targetType.BaseType;
                while (sourceType != null && targetType != null &&
                       (GetShape(targetType) == sourceType || GetShape(sourceType) == targetType)) {
                    m.IncludeBase(sourceType, targetType);
                    sourceType = sourceType.BaseType;
                    targetType = targetType.BaseType;
                }

                m.ForAllMembers(x => {
                    if (x.DestinationMember is PropertyInfo property && !property.CanWrite)
                        x.UseDestinationValue();
                });

                return m;
            }

            private IMappingExpression AddCallbacks(IMappingExpression mapping, Type type)
            {
                var callbackAttribs = type.GetCustomAttributes<DeserializationCallbackAttribute>();
                if (callbackAttribs.Any()) {
                    mapping.AfterMap((src, dst) => {
                        foreach (var attrib in callbackAttribs)
                            attrib.Callback.OnDeserialized(dst);
                    });
                }

                return mapping;
            }

            private static Type GetShape(Type type)
            {
                var shape = type.GetCustomAttribute<SerializedShapeAttribute>()?.Shape;
                if (typeof(SettingsElement).IsAssignableFrom(shape))
                    return shape;
                return null;
            }
        }
    }
}
