namespace EventTraceKit.VsExtension
{
    using System.ComponentModel;

    public class DefaultValueExAttribute : DefaultValueAttribute
    {
        public DefaultValueExAttribute(ulong value)
            : base(null)
        {
            SetValue(value);
        }
    }
}
