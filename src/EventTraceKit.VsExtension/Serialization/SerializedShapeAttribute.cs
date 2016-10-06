namespace EventTraceKit.VsExtension.Serialization
{
    using System;

    [AttributeUsage(AttributeTargets.Class)]
    public class SerializedShapeAttribute : Attribute
    {
        public SerializedShapeAttribute(Type shape)
        {
            Shape = shape;
        }

        public Type Shape { get; }
    }
}
