namespace EventTraceKit.VsExtension.Formatting
{
    using System;

    public interface IFormatProviderSource
    {
        IFormatProvider GetFormatProvider(Type dataType);
        string GetFormat(IFormatProvider formatProvider, string format);
    }
}
