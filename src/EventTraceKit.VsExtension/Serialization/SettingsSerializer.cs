namespace EventTraceKit.VsExtension.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using AutoMapper;
    using AutoMapper.Configuration.Internal;
    using AutoMapper.Execution;
    using AutoMapper.Internal;
    using AutoMapper.Mappers.Internal;
    using EventTraceKit.VsExtension.Collections;
    using Settings.Persistence;

    public class SettingsSerializer : ShapingXamlSerializer<SettingsElement>
    {
        public SettingsSerializer()
            : base(CreateSerializer(), new SerializationMapper<SettingsElement>(), CreateMapper())
        {
        }

        private static SafeXamlSerializer CreateSerializer()
        {
            var serializer = new SafeXamlSerializer(typeof(SettingsElement).Assembly);

            foreach (var type in typeof(SettingsElement).Assembly.GetTypes()) {
                if (typeof(SettingsElement).IsAssignableFrom(type))
                    serializer.AddKnownType(type);
            }

            return serializer;
        }

        private sealed class SerializationProfile : Profile
        {
            public SerializationProfile()
            {
                var assembly = typeof(SettingsElement).Assembly;

                foreach (var targetType in assembly.GetTypes()) {
                    var sourceType = GetShape(targetType);
                    if (sourceType == null)
                        continue;

                    CreateMapHierarchy(sourceType, targetType);
                    CreateMapHierarchy(targetType, sourceType);
                }
            }

            private void CreateMapHierarchy(Type sourceType, Type targetType)
            {
                var m = CreateMap(sourceType, targetType);

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
            }

            private static Type GetShape(Type type)
            {
                var shape = type.GetCustomAttribute<SerializedShapeAttribute>()?.Shape;
                if (typeof(SettingsElement).IsAssignableFrom(shape))
                    return shape;
                return null;
            }
        }

        private static IMapper CreateMapper()
        {
            var cfg = new MapperConfiguration(c => {
                c.Mappers.Insert(0, new CommaSeparatedValuesToCollectionMapper());
                c.Mappers.Insert(0, new CollectionToCommaSeparatedValuesMapper());
                c.AddProfile(new SerializationProfile());
            });
            return cfg.CreateMapper();
        }
    }

    public class CommaSeparatedValuesToCollectionMapper : IObjectMapper
    {
        private static readonly MethodInfo MapMethodInfo;

        static CommaSeparatedValuesToCollectionMapper()
        {
            MapMethodInfo = typeof(CommaSeparatedValuesToCollectionMapper).GetMethod(
                nameof(Map), BindingFlags.Static | BindingFlags.NonPublic);
        }

        public bool IsMatch(TypePair context)
        {
            return context.SourceType == typeof(string) &&
                   context.DestinationType != typeof(string) &&
                   PrimitiveHelper.IsCollectionType(context.DestinationType);
        }

        public Expression MapExpression(
            IConfigurationProvider configurationProvider, ProfileMap profileMap,
            PropertyMap propertyMap, Expression sourceExpression,
            Expression destExpression, Expression contextExpression)
        {
            var destElementType = ElementTypeHelper.GetElementType(destExpression.Type);
            var destCollectionType = destExpression.Type;
            var typeArguments = new[] { destElementType };
            var arguments = new[] {
                sourceExpression,
                Expression.Condition(
                    Expression.Equal(ExpressionFactory.ToObject(destExpression), Expression.Constant(null)),
                    DelegateFactory.GenerateConstructorExpression(destCollectionType),
                    destExpression),
                contextExpression,
                Expression.Constant(profileMap)
            };
            return Expression.Call(null, MapMethodInfo.MakeGenericMethod(typeArguments), arguments);
        }

        private static ICollection<TDestination> Map<TDestination>(
            string source, ICollection<TDestination> destination,
            ResolutionContext context, ProfileMap profileMap)
        {
            var values = source.Split(',').Select(x => context.Mapper.Map<TDestination>(x));
            destination.AddRange(values);
            return destination;
        }
    }

    public class CollectionToCommaSeparatedValuesMapper : IObjectMapper
    {
        private static readonly MethodInfo MapMethodInfo;

        static CollectionToCommaSeparatedValuesMapper()
        {
            MapMethodInfo = typeof(CollectionToCommaSeparatedValuesMapper).GetMethod(
                nameof(Map), BindingFlags.Static | BindingFlags.NonPublic);
        }

        public bool IsMatch(TypePair context)
        {
            return context.SourceType != typeof(string) &&
                   PrimitiveHelper.IsCollectionType(context.SourceType) &&
                   context.DestinationType == typeof(string);
        }

        public Expression MapExpression(
            IConfigurationProvider configurationProvider, ProfileMap profileMap,
            PropertyMap propertyMap, Expression sourceExpression,
            Expression destExpression, Expression contextExpression)
        {
            var sourceElementType = ElementTypeHelper.GetElementType(sourceExpression.Type);
            var typeArguments = new[] { sourceElementType };
            var arguments = new[] {
                sourceExpression,
                destExpression,
                contextExpression,
                Expression.Constant(profileMap)
            };
            return Expression.Call(null, MapMethodInfo.MakeGenericMethod(typeArguments), arguments);
        }

        private static string Map<TSource>(
            ICollection<TSource> source, string destination,
            ResolutionContext context, ProfileMap profileMap)
        {
            if (source == null || source.Count == 0)
                return null;
            return string.Join(",", source);
        }
    }
}
