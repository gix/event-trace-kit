namespace InstrManifestCompiler.Extensions
{
    using System.Runtime.Serialization;

    internal static class SerializationInfoExtensions
    {
        public static T GetValue<T>(this SerializationInfo serializationInfo, string name)
        {
            return (T)serializationInfo.GetValue(name, typeof(T));
        }
    }
}
