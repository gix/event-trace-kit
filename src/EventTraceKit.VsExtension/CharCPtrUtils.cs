namespace EventTraceKit.VsExtension
{
    using System;

    internal static class CharCPtrUtils
    {
        public static unsafe int GetLength(char* ptr, int maxLength)
        {
            if (ptr == null)
                throw new ArgumentNullException(nameof(ptr));
            if (maxLength < 0)
                throw new ArgumentOutOfRangeException(nameof(maxLength), "must be non-negative");

            int length = 0;
            while (length < maxLength) {
                if (ptr[0] == '\0')
                    return length;
                ++ptr;
                ++length;
            }

            return length;
        }

        public static unsafe string GetString(char* ptr, int maxLength)
        {
            int length = GetLength(ptr, maxLength);
            return new string(ptr, 0, length);
        }
    }
}
