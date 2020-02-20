namespace EventTraceKit.VsExtension.Native
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    public struct UnmanagedString
        : IEquatable<UnmanagedString>
        , IComparable<UnmanagedString>
        , IComparable<string>
    {
        private readonly unsafe char* str;

        public unsafe UnmanagedString(char* str)
        {
            this.str = str;
        }

        public static readonly UnmanagedString Empty;

        public unsafe bool IsEmpty
        {
            get
            {
                if (HasValue)
                    return str[0] == '\0';
                return true;
            }
        }

        public unsafe bool HasValue => str != null;

        public override unsafe string ToString()
        {
            return str != null ? new string(str) : string.Empty;
        }

        public static implicit operator string(UnmanagedString value)
        {
            return value.ToString();
        }

        public static unsafe explicit operator char*(UnmanagedString value)
        {
            return value.str;
        }

        public bool Equals(UnmanagedString other)
        {
            throw new NotImplementedException();
        }

        public int CompareTo(UnmanagedString other)
        {
            throw new NotImplementedException();
        }

        public int CompareTo(string other)
        {
            throw new NotImplementedException();
        }
    }
}
