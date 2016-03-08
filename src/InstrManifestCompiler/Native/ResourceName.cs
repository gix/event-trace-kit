namespace InstrManifestCompiler.Native
{
    using System;
    using System.Globalization;
    using System.Runtime.InteropServices;

    internal sealed class ResourceName
    {
        private readonly string name;
        private readonly short id;

        public static ResourceName FromPtr(IntPtr ptr)
        {
            if (UnsafeNativeMethods.IS_INTRESOURCE(ptr))
                return new ResourceName((short)ptr.ToInt64());
            return new ResourceName(Marshal.PtrToStringUni(ptr));
        }

        public ResourceName(short id)
        {
            this.id = id;
        }

        public ResourceName(string name)
        {
            this.name = name;
        }

        public short Id
        {
            get { return id; }
        }

        public string Name
        {
            get { return name; }
        }

        public override string ToString()
        {
            if (Name != null)
                return Name;
            return Id.ToString(CultureInfo.InvariantCulture);
        }
    }
}
