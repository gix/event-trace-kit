namespace EventTraceKit.VsExtension.Serialization
{
    using System;

    public class ActivatorUtils
    {
        public static bool TryCreateInstance(Type type, out object result, params object[] args)
        {
            try {
                result = Activator.CreateInstance(type, args);
                return result != null;
            } catch {
                result = null;
                return false;
            }
        }

        public static bool TryCreateInstance<TBase>(
            Type type, out TBase result, params object[] args)
        {
            if (TryCreateInstance(type, out var instance, args) && instance is TBase) {
                result = (TBase)instance;
                return true;
            }

            result = default;
            return false;
        }
    }
}
