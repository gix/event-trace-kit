namespace EventTraceKit.VsExtension.Serialization
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using Extensions;

    public class SerializableElementHelperOld<TSerializedBaseType>
    {
        public static bool TryConvertForSerialization<TSource, TTarget>(
            TSource source, out TTarget target)
            where TTarget : TSerializedBaseType
        {
            if (TryConvertForSerialization(typeof(TSource), typeof(TTarget), source, out var element)
                && element is TTarget) {
                target = (TTarget)element;
                return true;
            }

            target = default;
            return false;
        }

        public static bool TryConvertForSerialization(
            object source, out TSerializedBaseType target)
        {
            if (TryConvertForSerialization(source?.GetType(), source, out var element)
                && element is TSerializedBaseType) {
                target = (TSerializedBaseType)element;
                return true;
            }

            target = default;
            return false;
        }

        private static bool TryConvertForSerialization(
            Type sourceType, Type targetType, object sourceObj, out object targetObj)
        {
            targetObj = null;
            if (targetType == null)
                return false;

            return TryConvertForSerialization(sourceType, sourceObj, out targetObj) &&
                   targetType.IsInstanceOfType(targetObj);
        }

        private static bool TryConvertForSerialization(
            Type sourceType, object sourceObj, out object targetObj)
        {
            targetObj = null;

            if (sourceType == null ||
                !sourceType.IsInstanceOfType(sourceObj))
                return false;

            Type actualSourceType = sourceObj.GetType();
            if (!TryGetSerializedType(actualSourceType, out var serializedType) ||
                !typeof(TSerializedBaseType).IsAssignableFrom(serializedType))
                return false;

            if (!ActivatorUtils.TryCreateInstance(serializedType, out targetObj))
                return false;

            foreach (var entry in GetSerializedProperties(actualSourceType, serializedType)) {
                PropertyInfo sourceProperty = entry.Item1;
                PropertyInfo targetProperty = entry.Item2;
                object specifiedDefaultValue = entry.Item3;
                object sourceValue = sourceProperty.GetValue(sourceObj);
                if (sourceValue == null || sourceValue == GetDefaultValue(sourceValue.GetType(), specifiedDefaultValue))
                    continue;

                Type sourcePropertyType = sourceProperty.PropertyType.IsInterface ? sourceValue.GetType() : sourceProperty.PropertyType;
                Type targetPropertyType = targetProperty.PropertyType;

                object targetValue;
                if (TryGetSerializedType(sourcePropertyType, out var serializedPropertyType) &&
                    targetPropertyType.IsAssignableFrom(serializedPropertyType)) {
                    if (!TryConvertForSerialization(sourcePropertyType, targetPropertyType, sourceValue, out var element))
                        continue;
                    targetValue = element;
                } else {
                    if (CanConvertCollection(sourcePropertyType, targetPropertyType, out var sourceItemType, out var targetItemType)) {
                        if (!(sourceValue is IList sourceList))
                            continue;

                        IList targetList = SerializeCollection(
                            targetPropertyType, sourceList, sourceItemType, targetItemType);
                        if (targetList == null)
                            continue;

                        if (targetList.Count == 0)
                            targetList = null;

                        targetValue = targetList;
                    } else if (!TryConvertValueForSerialization(sourcePropertyType, targetPropertyType, sourceValue, specifiedDefaultValue, out targetValue)) {
                        continue;
                    }
                }

                if (targetPropertyType == typeof(string) &&
                    string.IsNullOrEmpty(targetValue as string))
                    targetValue = null;

                targetProperty.SetValue(targetObj, targetValue);

                if (targetPropertyType.IsValueType) {
                    PropertyInfo property = serializedType.GetProperty(targetProperty.Name + "Specified");
                    if (property != null) {
                        bool flag = (targetValue != null) && ((sourceValue != GetDefaultValue(sourcePropertyType, specifiedDefaultValue)) || sourcePropertyType.IsNullableType());
                        property.SetValue(targetObj, flag);
                    }
                }
            }

            return true;
        }

        private static IList SerializeCollection(
            Type targetPropertyType, IList sourceList, Type sourceItemType,
            Type targetItemType)
        {
            IList targetList;
            if (targetPropertyType.IsArray) {
                if (!ActivatorUtils.TryCreateInstance(targetPropertyType, out targetList, sourceList.Count))
                    return null;

                for (int i = 0; i < sourceList.Count; ++i) {
                    if (TryConvertForSerialization(sourceItemType, targetItemType, sourceList[i], out var targetItem))
                        targetList[i] = targetItem;
                }
            } else {
                if (!ActivatorUtils.TryCreateInstance(targetPropertyType, out targetList))
                    return null;

                foreach (object item in sourceList) {
                    if (TryConvertForSerialization(sourceItemType, targetItemType, item, out var targetItem))
                        targetList.Add(targetItem);
                    else if (sourceItemType.IsValueType && sourceItemType == targetItemType)
                        targetList.Add(item);
                }
            }

            return targetList;
        }

        private static bool CanConvertCollection(
            Type sourceType, Type targetType, out Type sourceItemType, out Type targetItemType)
        {
            targetItemType = null;
            if (!TypeHelper.TryGetGenericListItemType(sourceType, out sourceItemType) ||
                !TypeHelper.TryGetGenericListItemType(targetType, out targetItemType))
                return false;

            bool isSerializedAsCustomType =
                TryGetSerializedType(sourceItemType, out var serializedItemType) &&
                serializedItemType == targetItemType &&
                typeof(TSerializedBaseType).IsAssignableFrom(serializedItemType);
            if (isSerializedAsCustomType)
                return true;

            return sourceItemType.IsValueType && sourceItemType == targetItemType;
        }

        private static object GetDefaultValue(Type type, object specifiedDefaultValue)
        {
            if (type.IsNullableType())
                return null;
            if (specifiedDefaultValue != null)
                return specifiedDefaultValue;
            if (type.IsValueType)
                return Activator.CreateInstance(type);
            return null;
        }

        private static object GetDefaultValue(Type type)
        {
            if (type.IsNullableType())
                return null;
            if (type.IsValueType)
                return Activator.CreateInstance(type);
            return null;
        }

        private static bool TryConvertValueForSerialization(
            Type sourceType, Type targetType, object sourceValue, object defaultValue, out object targetValue)
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
                if (sourceValue.Equals(GetDefaultValue(sourceType, defaultValue)))
                    targetValue = null;
                else
                    targetValue = sourceValue.ToString();

                return true;
            }

            targetValue = null;
            return false;
        }


        private static IEnumerable<Tuple<PropertyInfo, PropertyInfo, object>> GetSerializedProperties(
            Type sourceType, Type targetType)
        {
            foreach (PropertyInfo source in sourceType.GetProperties()) {
                if (!TryGetSerializedPropertyName(source, out var serializedName, out var defaultValue))
                    continue;

                PropertyInfo target = targetType.GetProperty(serializedName);
                if (target == null)
                    throw new InvalidOperationException(string.Format(
                        CultureInfo.InvariantCulture, "Property '{0}.{1}' references missing serialized property '{2}.{3}'.",
                        sourceType, source.Name, targetType, serializedName));

                yield return Tuple.Create(source, target, defaultValue);
            }
        }

        private static bool TryGetSerializedPropertyName(
            PropertyInfo info, out string serializedName, out object defaultValue)
        {
            var attribute = info.GetCustomAttribute<SerializeAttribute>(true);
            if (attribute == null) {
                serializedName = null;
                defaultValue = null;
                return false;
            }

            serializedName = attribute.SerializedName;
            defaultValue = null;
            return !string.IsNullOrEmpty(serializedName);
        }

        private static bool TryGetSerializedType(Type sourceType, out Type serializedType)
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

        public static bool TryDeserialize<T>(object element, out T result)
        {
            if (!TryDeserialize(element, typeof(T), out var target) || !(target is T)) {
                result = default;
                return false;
            }

            result = (T)target;
            return true;
        }

        private static bool TryDeserialize(object source, Type targetType, out object result)
        {
            result = null;
            if (!TryGetSerializedType(targetType, out var serializedType) || source == null ||
                serializedType != source.GetType())
                return false;

            if (!ActivatorUtils.TryCreateInstance(targetType, out result))
                return false;

            return TryPopulateObject(source, result);
        }

        public static bool TryPopulateObject(object source, object target)
        {
            if (source == null)
                return false;

            foreach (var entry in GetSerializedProperties(target.GetType(), source.GetType())) {
                PropertyInfo targetProperty = entry.Item1;
                PropertyInfo sourceProperty = entry.Item2;

                //CustomDeserializerAttribute customAttribute = targetProperty.GetCustomAttribute<CustomDeserializerAttribute>();
                //if (customAttribute != null) {
                //    ICustomPropertyDeserializer deserializer = customAttribute.Deserializer;
                //    if (deserializer != null)
                //        deserializer.DeserializeProperty(source, target);
                //    continue;
                //}

                Type sourcePropertyType = sourceProperty.PropertyType;
                Type targetPropertyType = targetProperty.PropertyType;
                object defaultValue = GetDefaultValue(targetPropertyType, entry.Item3);

                //if (!Equals(ObjectUtils.GetDefaultValue(targetPropertyType), defaultValue) && MissingSpecifiedValue(source, sourceProperty)) {
                //    targetProperty.SetValue(target, defaultValue);
                //    continue;
                //}

                object sourceValue = sourceProperty.GetValue(source);
                if (sourceValue == null)
                    continue;

                //if (targetPropertyType.IsNullableType() && MissingSpecifiedValue(source, sourceProperty)) {
                //    targetProperty.SetValue(target, null);
                //    continue;
                //}

                object targetValue;
                bool skipWrite = false;
                if (TryGetSerializedType(targetPropertyType, out var type5) && type5 == sourcePropertyType) {
                    if (!TryDeserialize(sourceValue, targetPropertyType, out targetValue))
                        continue;
                } else {
                    if (CanConvertCollection(targetPropertyType, sourcePropertyType, out var targetItemType, out var sourceItemType)) {
                        if (!(sourceValue is IList list))
                            continue;

                        IList targetList;
                        if (targetPropertyType.IsArray) {
                            if (!targetProperty.CanWrite) {
                                targetList = targetProperty.GetValue(target) as IList;
                                if (targetList == null || targetList.Count != list.Count)
                                    continue;
                                skipWrite = true;
                            } else if (!ActivatorUtils.TryCreateInstance(targetPropertyType, out targetList, list.Count))
                                continue;

                            for (int i = 0; i < list.Count; ++i) {
                                if (TryDeserialize(list[i], targetItemType, out var value))
                                    targetList[i] = value;
                            }
                        } else {
                            if (!targetProperty.CanWrite) {
                                targetList = targetProperty.GetValue(target) as IList;
                                if (targetList == null)
                                    continue;
                                skipWrite = true;
                            } else if (!ActivatorUtils.TryCreateInstance(targetPropertyType, out targetList))
                                continue;

                            foreach (var item in list) {
                                if (TryDeserialize(item, targetItemType, out var value))
                                    targetList.Add(value);
                                else if (sourceItemType.IsValueType && sourceItemType == targetItemType)
                                    targetList.Add(item);
                            }
                        }

                        targetValue = targetList;
                    } else if (!TryConvertValueForDeserialization(
                        sourcePropertyType, targetPropertyType, sourceValue, out targetValue)) {
                        continue;
                    }
                }

                if (!skipWrite)
                    targetProperty.SetValue(target, targetValue);
            }

            return true;
        }

        private static bool TryConvertValueForDeserialization(
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
                if (string.IsNullOrEmpty(str) || !Guid.TryParse(str, out var guid)) {
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

        private static bool TryParseEnum(Type type, string str, out object result)
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
