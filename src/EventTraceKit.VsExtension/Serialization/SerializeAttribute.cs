namespace EventTraceKit.VsExtension.Serialization
{
    using System;
    using System.Runtime.CompilerServices;

    [AttributeUsage(AttributeTargets.Property)]
    public class SerializeAttribute : Attribute
    {
        public SerializeAttribute([CallerMemberName] string serializedName = "")
        {
            SerializedName = serializedName;
        }

        public string SerializedName { get; }
    }
}
