namespace EventTraceKit.VsExtension.Serialization
{
    using System;
    using System.Reflection;

    public static class TypeExtensions
    {
        public static T TryGetAttribute<T>(this MemberInfo member, bool inherit)
            where T : Attribute
        {
            var attributes = member.GetCustomAttributes(typeof(T), inherit);
            if (attributes.Length != 0)
                return (T)attributes[0];
            return default(T);
        }
    }
}
