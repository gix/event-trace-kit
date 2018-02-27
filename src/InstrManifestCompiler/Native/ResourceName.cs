namespace InstrManifestCompiler.Native
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

        public override string ToString()
        {
            if (Name != null)
                return Name;
            return Id.ToString(CultureInfo.InvariantCulture);
        }
    }
}
