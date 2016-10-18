namespace EventTraceKit.VsExtension
{
    public static class ArrayUtils
    {
        private static class EmptyHelper<T>
        {
            public static readonly T[] Instance;

            static EmptyHelper()
            {
                Instance = new T[0];
            }
        }

        public static T[] Empty<T>()
        {
            return EmptyHelper<T>.Instance;
        }
    }
}