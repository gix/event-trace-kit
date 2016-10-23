namespace EventTraceKit.VsExtension.Extensions
{
    using System;

    public static class ServiceProviderExtensions
    {
        public static T GetService<T>(this IServiceProvider provider)
            where T : class
        {
            return provider.GetService(typeof(T)) as T;
        }

        public static T GetService<TService, T>(this IServiceProvider provider)
            where T : class
        {
            return provider.GetService(typeof(TService)) as T;
        }
    }
}
