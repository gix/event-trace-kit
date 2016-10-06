namespace EventTraceKit.VsExtension.Windows
{
    using System.Windows;

    public interface IDependencyObjectCustomSerializerAccess
    {
        object GetValue(DependencyProperty dp);
        bool ShouldSerializeProperty(DependencyProperty dp);
    }

    public abstract class FreezableCustomSerializerAccessBase
        : Freezable, IDependencyObjectCustomSerializerAccess
    {
        object IDependencyObjectCustomSerializerAccess.GetValue(DependencyProperty dp)
        {
            return GetValue(dp);
        }

        bool IDependencyObjectCustomSerializerAccess.ShouldSerializeProperty(DependencyProperty dp)
        {
            return ShouldSerializeProperty(dp);
        }
    }
}
