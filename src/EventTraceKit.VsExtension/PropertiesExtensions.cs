namespace EventTraceKit.VsExtension
{
    using System;
    using EnvDTE;

    internal static class PropertiesExtensions
    {
        public static T GetValue<T>(this Properties properties, string name)
        {
            try {
                var property = properties.Item(name);
                if (property?.Value is T val) {
                    return val;
                }

                return default;
            } catch (Exception) {
                return default;
            }
        }

        public static bool TryGetProperty<T>(this Properties properties, string name, out T value)
        {
            try {
                var property = properties.Item(name);
                if (property?.Value is T val) {
                    value = val;
                    return true;
                }

                value = default;
                return false;
            } catch (Exception) {
                value = default;
                return false;
            }
        }
    }
}
