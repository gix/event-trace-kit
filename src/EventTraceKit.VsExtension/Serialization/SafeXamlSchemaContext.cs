namespace EventTraceKit.VsExtension.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Xaml;

    public sealed class SafeXamlSchemaContext : XamlSchemaContext
    {
        private readonly Dictionary<string, Dictionary<string, XamlType>> knownTypes =
            new Dictionary<string, Dictionary<string, XamlType>>();

        public SafeXamlSchemaContext(IEnumerable<Assembly> safeAssemblies)
            : base(safeAssemblies)
        {
            AddKnownType(typeof(sbyte));
            AddKnownType(typeof(short));
            AddKnownType(typeof(int));
            AddKnownType(typeof(long));
            AddKnownType(typeof(byte));
            AddKnownType(typeof(ushort));
            AddKnownType(typeof(uint));
            AddKnownType(typeof(ulong));
            AddKnownType(typeof(decimal));
            AddKnownType(typeof(float));
            AddKnownType(typeof(double));
            AddKnownType(typeof(string));
            AddKnownType(typeof(object));
            AddKnownType(typeof(bool));
            AddKnownType(typeof(char));
            AddKnownType(typeof(Guid));
            AddKnownType(typeof(TimeSpan));
            AddKnownType(typeof(DateTime));
            AddKnownType(typeof(DateTimeOffset));
        }

        public void AddKnownType(Type type)
        {
            string assemblyName = type.Assembly.GetName().Name;
            string ns = GetClrNamespace(type.Namespace, assemblyName);
            AddKnownType(ns, type.Name, type);
        }

        private static string GetClrNamespace(string namespaceName, string assemblyName)
        {
            return $"clr-namespace:{namespaceName};assembly={assemblyName}";
        }

        private void AddKnownType(string ns, string name, Type type)
        {
            if (type.IsGenericType)
                throw new NotSupportedException("Known types cannot be generic types.");

            Dictionary<string, XamlType> typeMap;
            if (!knownTypes.TryGetValue(ns, out typeMap))
                typeMap = knownTypes[ns] = new Dictionary<string, XamlType>();

            typeMap[name] = new XamlType(type, this);
        }

        protected override XamlType GetXamlType(
            string xamlNamespace, string name, params XamlType[] typeArguments)
        {
            var xamlType = base.GetXamlType(xamlNamespace, name, typeArguments);
            var hasTypeArgs = typeArguments != null && typeArguments.Length != 0;
            if (xamlType != null || hasTypeArgs)
                return xamlType;

            return TryGetKnownXamlType(xamlNamespace, name);
        }

        private XamlType TryGetKnownXamlType(string xamlNamespace, string name)
        {
            Dictionary<string, XamlType> typeMap;
            XamlType xamlType;
            if (knownTypes.TryGetValue(xamlNamespace, out typeMap) &&
                typeMap.TryGetValue(name, out xamlType))
                return xamlType;

            return null;
        }
    }
}
