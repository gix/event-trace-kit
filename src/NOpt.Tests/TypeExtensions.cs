namespace NOpt.Tests
{
    using System;

    internal static class TypeExtensions
    {
        public static bool ImplementsInterface(this Type type, Type interfaceType)
        {
            return interfaceType.IsInterface && interfaceType.IsAssignableFrom(type);
        }

        public static bool ImplementsInterface<TInterface>(this Type type)
        {
            return type.ImplementsInterface(typeof(TInterface));
        }
    }
}
