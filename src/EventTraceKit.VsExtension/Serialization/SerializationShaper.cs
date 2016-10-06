namespace EventTraceKit.VsExtension.Serialization
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;

    public class SerializationShaper<TSerializedBaseType>
    {
        public bool TrySerialize(object source, out TSerializedBaseType target)
        {
            object element;
            if (TrySerialize(source?.GetType(), source, out element)
                && element is TSerializedBaseType) {
                target = (TSerializedBaseType)element;
                return true;
            }

            target = default(TSerializedBaseType);
            return false;
        }


        public bool TryDeserialize<T>(TSerializedBaseType element, out T result)
        {
            object target;
            if (!TryDeserialize(element, typeof(T), out target) || !(target is T)) {
                result = default(T);
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

                object sourceValue = sourceProperty.GetValue(source);
                if (sourceValue == null)
                    continue;

                Type serializedType;
                object targetValue;
                if (TryGetSerializedType(targetPropertyType, out serializedType) && serializedType == sourcePropertyType) {
                    if (!TryDeserialize(sourceValue, targetPropertyType, out targetValue))
                        continue;

                    targetProperty.SetValue(target, targetValue);
                    continue;
                }

                if (TryConvertBackCollection(target, targetPropertyType, sourcePropertyType, sourceValue, targetProperty))
                    continue;

                if (TryConvertValueForDeserialization(
                    sourcePropertyType, targetPropertyType, sourceValue, out targetValue))
                    targetProperty.SetValue(target, targetValue);
            }

            return true;
        }

        private bool TryDeserialize(object source, Type targetType, out object result)
        {
            Type serializedType;
            result = null;
            if (!TryGetSerializedType(targetType, out serializedType) || source == null ||
                serializedType != source.GetType())
                return false;

            if (!ActivatorUtils.TryCreateInstance(targetType, out result))
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
                Guid guid;
                if (string.IsNullOrEmpty(str) || !Guid.TryParse(str, out guid)) {
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

                return TryParseEnum(targetType, str, out targetValue);
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

            Type serializedType;
            Type actualSourceType = sourceObj.GetType();
            if (!TryGetSerializedType(actualSourceType, out serializedType) ||
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
            if (sourceValue == null || IsDefaultValue(sourceProperty, sourceValue))
                return;

            Type sourcePropertyType = sourceProperty.PropertyType.IsInterface ? sourceValue.GetType() : sourceProperty.PropertyType;
            Type targetPropertyType = targetProperty.PropertyType;

            Type serializedPropertyType;
            object targetValue;
            if (TryGetSerializedType(sourcePropertyType, out serializedPropertyType) &&
                targetPropertyType.IsAssignableFrom(serializedPropertyType)) {
                object element;
                if (!TrySerialize(sourcePropertyType, targetPropertyType, sourceValue, out element))
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
            Type sourceItemType;
            Type targetItemType;
            if (!CanConvertCollection(sourcePropertyType, targetPropertyType,
                                      out sourceItemType, out targetItemType))
                return false;

            IList sourceList = sourceValue as IList;
            if (sourceList == null)
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
            object sourceValue, PropertyInfo targetProperty)
        {
            Type sourceItemType;
            Type targetItemType;
            if (!CanConvertCollection(targetPropertyType, sourcePropertyType,
                out targetItemType, out sourceItemType))
                return false;

            IList sourceList = sourceValue as IList;
            if (sourceList == null)
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
                    object value;
                    if (TryDeserialize(sourceList[i], targetItemType, out value))
                        targetList[i] = value;
                }
            } else {
                foreach (var item in sourceList) {
                    object value;
                    if (TryDeserialize(item, targetItemType, out value))
                        targetList.Add(value);
                    else if (sourceItemType.IsValueType && sourceItemType == targetItemType)
                        targetList.Add(item);
                }
            }

            return true;
        }

        private bool TryCreateCollection(
            IList sourceList, Type collectionType, out IList collection)
        {
            var args = collectionType.IsArray ? new object[] { sourceList.Count } : null;

            IList list;
            if (ActivatorUtils.TryCreateInstance(collectionType, out list, args)) {
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
                    object targetItem;
                    if (TrySerialize(sourceItemType, targetItemType, sourceList[i], out targetItem))
                        targetList[i] = targetItem;
                }
            } else {
                foreach (object item in sourceList) {
                    object targetItem;
                    if (TrySerialize(sourceItemType, targetItemType, item, out targetItem))
                        targetList.Add(targetItem);
                    else if (sourceItemType.IsValueType && sourceItemType == targetItemType)
                        targetList.Add(item);
                }
            }
        }

        private bool CanConvertCollection(
            Type sourceType, Type targetType, out Type sourceItemType, out Type targetItemType)
        {
            Type serializedItemType;
            targetItemType = null;
            if (!TypeHelper.TryGetGenericListItemType(sourceType, out sourceItemType) ||
                !TypeHelper.TryGetGenericListItemType(targetType, out targetItemType))
                return false;

            bool isSerializedAsCustomType =
                TryGetSerializedType(sourceItemType, out serializedItemType) &&
                serializedItemType == targetItemType &&
                typeof(TSerializedBaseType).IsAssignableFrom(serializedItemType);
            if (isSerializedAsCustomType)
                return true;

            return sourceItemType.IsValueType && sourceItemType == targetItemType;
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
                string serializedName;
                PropertyInfo target;
                if (TryGetSerializedPropertyName(source, out serializedName)) {
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
            var attribute = info?.TryGetAttribute<SerializeAttribute>(true);
            if (attribute == null) {
                serializedName = null;
                return false;
            }

            serializedName = attribute.SerializedName;
            return !string.IsNullOrEmpty(serializedName);
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
            var attribute = property.TryGetAttribute<DefaultValueAttribute>(true);
            object defaultValue;
            if (attribute != null)
                defaultValue = attribute.Value;
            else
                defaultValue = GetDefaultValue(value.GetType());

            return value == defaultValue || (value != null && value.Equals(defaultValue));
        }

        private bool TryParseEnum(Type type, string str, out object result)
        {
            var parameters = new object[] { str, false, null };
            var method = typeof(Enum).GetMethod(nameof(Enum.TryParse))
                .MakeGenericMethod(type);

            if ((bool)method.Invoke(null, parameters)) {
                result = parameters[2];
                return true;
            }

            result = null;
            return false;
        }
    }
}