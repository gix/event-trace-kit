namespace EventTraceKit.VsExtension.Serialization
{
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection;
    using AutoMapper;
    using AutoMapper.Configuration.Internal;
    using AutoMapper.Mappers.Internal;

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
