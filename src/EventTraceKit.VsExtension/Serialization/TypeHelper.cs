namespace EventTraceKit.VsExtension.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class TypeHelper
    {
        public static bool TryGetGenericListItemType(Type type, out Type itemType)
        {
            if (type.IsArray) {
                itemType = type.GetElementType();
                return true;
            }

            Type listType = null;
            if (type.IsGenericType) {
                var typeDefinition = type.GetGenericTypeDefinition();
                if (typeDefinition == typeof(IList<>))
                    listType = type;
            }

            if (listType == null)
                listType = type.FindInterfaces(FilterListInterfaces, null).TryGetSingleOrDefault();

            if (listType != null) {
                itemType = listType.GetGenericArguments().Single();
                return true;
            }

            itemType = null;
            return false;
        }

        private static bool FilterListInterfaces(Type type, object criteria)
        {
            if (type == typeof(List<>))
                return true;

            if (type.IsGenericType) {
                var typeDefinition = type.GetGenericTypeDefinition();
                if (typeDefinition == typeof(IList<>))
                    return true;
            }

            return false;
        }

        public static bool IsNullableType(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
    }
}
