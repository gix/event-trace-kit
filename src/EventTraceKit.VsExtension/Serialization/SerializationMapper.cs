namespace EventTraceKit.VsExtension.Serialization
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using EventTraceKit.VsExtension.Extensions;

    public class SerializationMapper<TSerializedBaseType>
    {
        public bool TrySerialize<TTarget>(object source, out TTarget target)
            where TTarget : class, TSerializedBaseType
        {
            if (TrySerialize(source, out var t)) {
                target = t as TTarget;
                if (target != null)
                    return true;
            }

            target = null;
            return false;
        }

        public bool TrySerialize(object source, out TSerializedBaseType target)
        {
            if (TrySerialize(source?.GetType(), source, out var element)
                && element is TSerializedBaseType) {
                target = (TSerializedBaseType)element;
                return true;
            }

            target = default;
            return false;
        }

        public bool TryDeserialize<T>(TSerializedBaseType element, out T result)
        {
            if (!TryDeserialize(element, typeof(T), out object target) || !(target is T)) {
                result = default;
                return false;
            }

            result = (T)target;
            return true;
        }

        public bool TryPopulateObject(object source, object target)
        {
            if (source == null)
                return false;

            foreach (var entry in GetSerializedProperties(target.GetType(), source.GetType())) {
                PropertyInfo targetProperty = entry.Item1;
                PropertyInfo sourceProperty = entry.Item2;

                Type sourcePropertyType = sourceProperty.PropertyType;
                Type targetPropertyType = targetProperty.PropertyType;

                var callback = targetProperty.GetCustomAttribute<DeserializationCallbackAttribute>()?.Callback;

                object sourceValue = sourceProperty.GetValue(source);
                if (sourceValue == null)
                    continue;

                object targetValue;
                if (TryGetSerializedType(targetPropertyType, out Type serializedType) && serializedType == sourcePropertyType) {
                    if (!TryDeserialize(sourceValue, targetPropertyType, out targetValue))
                        continue;

                    callback?.OnDeserialized(targetValue);
                    targetProperty.SetValue(target, targetValue);
                    continue;
                }

                if (TryConvertBackCollection(
                        target, targetPropertyType, sourcePropertyType,
                        sourceValue, targetProperty, callback))
                    continue;

                if (TryConvertValueForDeserialization(
                    sourcePropertyType, targetPropertyType, sourceValue, out targetValue)) {
                    callback?.OnDeserialized(targetValue);
                    targetProperty.SetValue(target, targetValue);
                }
            }

            return true;
        }

        private bool TryDeserialize2(object source, Type sourceType, Type targetType, out object target)
        {
            if (TryDeserialize(source, targetType, out target))
                return true;
            if (CanReuse(sourceType, targetType)) {
                target = source;
                return true;
            }

            return false;
        }

        private bool TryDeserialize(object source, Type targetType, out object result)
        {
            result = null;
            if (source == null)
                return false;

            if (!TryGetSerializedType(targetType, out Type serializedType) ||
                serializedType != source.GetType())
                return false;

            if (!TryGetDeserializedType(targetType, source.GetType(), out Type deserializedType))
                return false;

            if (!ActivatorUtils.TryCreateInstance(targetType, out result))
            //if (!ActivatorUtils.TryCreateInstance(deserializedType, out result))
                return false;

            return TryPopulateObject(source, result);
        }

        private bool TryConvertValueForDeserialization(
            Type sourceType, Type targetType, object sourceValue, out object targetValue)
        {
            if (sourceType == null || targetType == null || sourceValue == null) {
                targetValue = null;
                return false;
            }

            if (targetType.IsAssignableFrom(sourceType)) {
                targetValue = sourceValue;
                return true;
            }

            if (sourceType == typeof(string) && targetType == typeof(Guid)) {
                string str = sourceValue as string;
                if (string.IsNullOrEmpty(str) || !Guid.TryParse(str, out Guid guid)) {
                    targetValue = null;
                    return false;
                }

                targetValue = guid;
                return true;
            }

            if (sourceType == typeof(string) && targetType.IsEnum) {
                string str = sourceValue as string;
                if (string.IsNullOrEmpty(str)) {
                    targetValue = GetDefaultValue(targetType);
                    return true;
                }

                return EnumUtils.TryParse(targetType, str, out targetValue);
            }

            targetValue = null;
            return false;
        }

        private bool TrySerialize(
            Type sourceType, Type targetType, object sourceObj, out object targetObj)
        {
            targetObj = null;
            if (targetType == null)
                return false;

            return TrySerialize(sourceType, sourceObj, out targetObj) &&
                   targetType.IsInstanceOfType(targetObj);
        }

        private bool TrySerialize(
            Type sourceType, object sourceObj, out object targetObj)
        {
            targetObj = null;

            if (sourceType == null || !sourceType.IsInstanceOfType(sourceObj))
                return false;

            Type actualSourceType = sourceObj.GetType();
            if (!TryGetSerializedType(actualSourceType, out Type serializedType) ||
                !typeof(TSerializedBaseType).IsAssignableFrom(serializedType))
                return false;

            if (!ActivatorUtils.TryCreateInstance(serializedType, out targetObj))
                return false;

            foreach (var entry in GetSerializedProperties(actualSourceType, serializedType)) {
                PropertyInfo sourceProperty = entry.Item1;
                PropertyInfo targetProperty = entry.Item2;
                ConvertProperty(sourceObj, targetObj, sourceProperty, targetProperty);
            }

            return true;
        }

        private void ConvertProperty(
            object source, object target, PropertyInfo sourceProperty, PropertyInfo targetProperty)
        {
            object sourceValue = sourceProperty.GetValue(source);
            if (sourceValue == null || IsDefaultValue(targetProperty, sourceValue))
                return;

            Type sourcePropertyType = sourceProperty.PropertyType.IsInterface ? sourceValue.GetType() : sourceProperty.PropertyType;
            Type targetPropertyType = targetProperty.PropertyType;

            object targetValue;
            if (TryGetSerializedType(sourcePropertyType, out Type serializedPropertyType) &&
                targetPropertyType.IsAssignableFrom(serializedPropertyType)) {
                if (!TrySerialize(sourcePropertyType, targetPropertyType, sourceValue, out object element))
                    throw new InvalidOperationException($"Failed to convert '{sourceValue}' from '{sourcePropertyType}' to '{targetPropertyType}'.");

                targetValue = element;
                targetProperty.SetValue(target, targetValue);
                return;
            }

            if (TryConvertCollection(target, targetProperty, sourcePropertyType, targetPropertyType, sourceValue))
                return;

            if (TryConvertValueForSerialization(sourcePropertyType, targetPropertyType, sourceValue, out targetValue))
                targetProperty.SetValue(target, targetValue);
        }

        private bool TryConvertCollection(
            object target, PropertyInfo targetProperty, Type sourcePropertyType,
            Type targetPropertyType, object sourceValue)
        {
            if (!CanConvertCollection(sourcePropertyType, targetPropertyType,
                                      out Type sourceItemType, out Type targetItemType))
                return false;

            if (!(sourceValue is IList sourceList))
                return true;

            IList targetList;
            if (targetProperty.CanWrite) {
                if (!TryCreateCollection(sourceList, targetPropertyType, out targetList))
                    throw new InvalidOperationException(
                        $"Failed to create collection of type '{targetPropertyType}'.");
                targetProperty.SetValue(target, targetList);
            } else {
                targetList = targetProperty.GetValue(target) as IList;
                if (targetList == null)
                    throw new InvalidOperationException(
                        $"Read-only target property '{targetProperty}' returned null.");
                if (targetPropertyType.IsArray && targetList.Count != sourceList.Count)
                    throw new InvalidOperationException(
                        $"Target array '{targetProperty}' has different length.");
            }

            ConvertCollection(
                targetPropertyType, sourceList, sourceItemType, targetList, targetItemType);

            return true;
        }

        private bool TryConvertBackCollection(
            object target, Type targetPropertyType, Type sourcePropertyType,
            object sourceValue, PropertyInfo targetProperty,
            IDeserializationCallback callback)
        {
            if (!CanConvertCollection(targetPropertyType, sourcePropertyType,
                out Type targetItemType, out Type sourceItemType))
                return false;

            if (!(sourceValue is IList sourceList))
                return true;

            IList targetList;
            if (targetProperty.CanWrite) {
                if (!TryCreateCollection(sourceList, targetPropertyType, out targetList))
                    throw new InvalidOperationException(
                        $"Failed to create collection of type '{targetPropertyType}'.");

                targetProperty.SetValue(target, targetList);
            } else {
                targetList = targetProperty.GetValue(target) as IList;
                if (targetList == null)
                    throw new InvalidOperationException(
                        $"Read-only target property '{targetProperty}' returned null.");
                if (targetPropertyType.IsArray && targetList.Count != sourceList.Count)
                    throw new InvalidOperationException(
                        $"Target array '{targetProperty}' has different length.");
            }

            if (targetPropertyType.IsArray) {
                for (int i = 0; i < sourceList.Count; ++i) {
                    if (TryDeserialize2(sourceList[i], sourceItemType, targetItemType, out object value))
                        targetList[i] = value;
                }
            } else {
                foreach (var item in sourceList) {
                    if (TryDeserialize2(item, sourceItemType, targetItemType, out object value))
                        targetList.Add(value);
                }
            }

            callback?.OnDeserialized(targetList);

            return true;
        }

        private bool TryCreateCollection(
            IList sourceList, Type collectionType, out IList collection)
        {
            var args = collectionType.IsArray ? new object[] { sourceList.Count } : null;

            if (ActivatorUtils.TryCreateInstance(collectionType, out IList list, args)) {
                collection = list;
                return true;
            }

            collection = null;
            return false;
        }

        private void ConvertCollection(
            Type targetPropertyType, IList sourceList, Type sourceItemType,
            IList targetList, Type targetItemType)
        {
            if (targetPropertyType.IsArray) {
                for (int i = 0; i < sourceList.Count; ++i) {
                    if (TrySerialize(sourceItemType, targetItemType, sourceList[i], out object targetItem))
                        targetList[i] = targetItem;
                }
            } else {
                foreach (object item in sourceList) {
                    if (TrySerialize(sourceItemType, targetItemType, item, out object targetItem))
                        targetList.Add(targetItem);
                    else if (sourceItemType.IsValueType && sourceItemType == targetItemType)
                        targetList.Add(item);
                }
            }
        }

        private bool CanConvertCollection(
            Type sourceType, Type targetType, out Type sourceItemType, out Type targetItemType)
        {
            targetItemType = null;
            if (!TypeHelper.TryGetGenericListItemType(sourceType, out sourceItemType) ||
                !TypeHelper.TryGetGenericListItemType(targetType, out targetItemType))
                return false;

            bool isSerializedAsCustomType =
                TryGetSerializedType(sourceItemType, out Type serializedItemType) &&
                serializedItemType == targetItemType &&
                typeof(TSerializedBaseType).IsAssignableFrom(serializedItemType);
            if (isSerializedAsCustomType)
                return true;

            return CanReuse(sourceItemType, targetItemType);
        }

        private static bool CanReuse(Type sourceType, Type targetType)
        {
            return sourceType == targetType &&
                   (sourceType.IsValueType || IsImmutableType(sourceType));
        }

        private static bool IsImmutableType(Type type)
        {
            return type == typeof(string);
        }

        private object GetDefaultValue(Type type)
        {
            if (type.IsNullableType())
                return null;
            if (type.IsValueType)
                return Activator.CreateInstance(type);
            return null;
        }

        private bool TryConvertValueForSerialization(
            Type sourceType, Type targetType, object sourceValue, out object targetValue)
        {
            if (sourceType == null || targetType == null || sourceValue == null) {
                targetValue = null;
                return false;
            }

            Type c = sourceType.IsNullableType() ? sourceType.GetGenericArguments().Single() : sourceType;
            if (targetType.IsAssignableFrom(c)) {
                targetValue = sourceValue;
                return true;
            }

            if (targetType == typeof(string)) {
                if (sourceValue.Equals(GetDefaultValue(sourceType)))
                    targetValue = null;
                else
                    targetValue = sourceValue.ToString();

                return true;
            }

            targetValue = null;
            return false;
        }

        private IEnumerable<Tuple<PropertyInfo, PropertyInfo>> GetSerializedProperties(
            Type sourceType, Type targetType)
        {
            foreach (PropertyInfo source in sourceType.GetProperties()) {
                PropertyInfo target;
                if (TryGetSerializedPropertyName(source, out string serializedName)) {
                    target = targetType.GetProperty(serializedName);
                    if (target == null)
                        throw new InvalidOperationException(string.Format(
                            CultureInfo.InvariantCulture,
                            "Property '{0}.{1}' references missing serialized property '{2}.{3}'.",
                            sourceType, source.Name, targetType, serializedName));
                } else {
                    target = targetType.GetProperty(source.Name);
                    if (target == null)
                        continue;
                }

                yield return Tuple.Create(source, target);
            }
        }

        private bool TryGetSerializedPropertyName(
            PropertyInfo info, out string serializedName)
        {
            var attribute = info.GetCustomAttribute<SerializeAttribute>(true);
            if (attribute == null) {
                serializedName = null;
                return false;
            }

            serializedName = attribute.SerializedName;
            return !string.IsNullOrEmpty(serializedName);
        }

        private readonly Dictionary<Type, Type> typeToShapeMap =
            new Dictionary<Type, Type>();
        private readonly HashSet<Assembly> typeToShapeMapScannedAssemblies =
            new HashSet<Assembly>();

        private void UpdateShapeMap(Assembly assembly)
        {
            if (!typeToShapeMapScannedAssemblies.Add(assembly))
                return;

            foreach (var type in assembly.GetTypes()) {
                var attribute = type.GetCustomAttributes<SerializedShapeAttribute>(true)
                    .TryGetSingleOrDefault();
                if (attribute != null)
                    typeToShapeMap.Add(type, attribute.Shape);
            }

            typeToShapeMapScannedAssemblies.Add(assembly);
        }

        private bool TryGetDeserializedType(Type targetType, Type sourceType, out Type deserializedType)
        {
            if (!TryGetSerializedType(targetType, out deserializedType) ||
                deserializedType != sourceType)
                return false;
            return true;

            if (sourceType == targetType && !targetType.IsAbstract) {
                deserializedType = targetType;
                return true;
            }

            if (TryGetSerializedType(targetType, out deserializedType) &&
                deserializedType == sourceType)
                return true;

            if (!targetType.IsAbstract)
                return false;

            UpdateShapeMap(targetType.Assembly);
            return typeToShapeMap.TryGetValue(targetType, out deserializedType);
        }

        private static bool TryGetShape2(Type type, out Type serializedType)
        {
            serializedType = type?.GetCustomAttribute<SerializedShapeAttribute>(true)?.Shape;
            return serializedType != null;
        }

        private bool TryGetSerializedType(Type sourceType, out Type serializedType)
        {
            if (sourceType == null) {
                serializedType = null;
                return false;
            }

            var attribute = sourceType.GetCustomAttributes<SerializedShapeAttribute>(true)
                .TryGetSingleOrDefault();
            if (attribute != null) {
                serializedType = attribute.Shape;
                return serializedType != null;
            }

            serializedType = null;
            return false;
        }

        private bool IsDefaultValue(PropertyInfo property, object value)
        {
            var attribute = property.GetCustomAttribute<DefaultValueAttribute>(true);
            object defaultValue;
            if (attribute != null)
                defaultValue = attribute.Value;
            else
                defaultValue = GetDefaultValue(value.GetType());

            return value == defaultValue || (value != null && value.Equals(defaultValue));
        }
    }
}
