namespace EventTraceKit.VsExtension.Serialization
{
    using System;

    [AttributeUsage(AttributeTargets.Class)]
    public class DeserializationCallbackAttribute : Attribute
    {
        public DeserializationCallbackAttribute(Type callbackType)
        {
            if (callbackType == null || !typeof(IDeserializationCallback).IsAssignableFrom(callbackType))
                throw new ArgumentException(
                    $"Callback type '{callbackType}' does not implement IDeserializationCallback.", nameof(callbackType));

            Callback = (IDeserializationCallback)Activator.CreateInstance(callbackType);
        }

        public IDeserializationCallback Callback { get; }
    }

    public interface IDeserializationCallback
    {
        void OnDeserialized(object obj);
    }
}
