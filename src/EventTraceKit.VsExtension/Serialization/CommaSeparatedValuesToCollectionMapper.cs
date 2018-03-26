namespace EventTraceKit.VsExtension.Serialization
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using AutoMapper;
    using AutoMapper.Configuration.Internal;
    using AutoMapper.Execution;
    using AutoMapper.Internal;
    using AutoMapper.Mappers.Internal;
    using EventTraceKit.VsExtension.Extensions;

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
}
