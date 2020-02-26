namespace EventTraceKit.EventTracing.Internal.Native
{
    using System;
    using System.Globalization;
    using System.Runtime.InteropServices;

    internal sealed class ResourceName
    {
        public ResourceName(short id)
        {
            Id = id;
        }

        public ResourceName(string name)
        {
            Name = name;
        }

        public static ResourceName FromPtr(IntPtr ptr)
        {
            if (UnsafeNativeMethods.IS_INTRESOURCE(ptr))
                return new ResourceName((short)ptr.ToInt64());
            return new ResourceName(Marshal.PtrToStringUni(ptr));
        }

        public short Id { get; }
        public string Name { get; }

        public static implicit operator ResourceName(short id)
        {
            return new ResourceName(id);
        }

        public static implicit operator ResourceName(string name)
        {
            return new ResourceName(name);
        }

        public override string ToString()
        {
            if (Name != null)
                return Name;
            return Id.ToString(CultureInfo.InvariantCulture);
        }
    }
}
